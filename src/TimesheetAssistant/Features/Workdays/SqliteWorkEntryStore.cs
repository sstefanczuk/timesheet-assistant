using System.Globalization;
using Microsoft.Data.Sqlite;
using TimesheetAssistant.Core.Features.Workdays;

namespace TimesheetAssistant.Features.Workdays;

public sealed class SqliteWorkEntryStore : IWorkEntryStore, IDisposable
{
    private const int BusyTimeoutSeconds = 5;
    private const string Columns = "entry_id, work_date, jira_id, title, description, duration_minutes, creation_order";
    private readonly string _connectionString;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    internal SqliteWorkEntryStore(string databasePath)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Pooling = false,
            DefaultTimeout = BusyTimeoutSeconds
        }.ToString();
    }

    internal void Initialize(bool isNew)
    {
        if (!isNew)
        {
            // Inspect existing files read-only before any schema/transaction writes.
            var inspection = new SqliteConnectionStringBuilder(_connectionString)
            {
                Mode = SqliteOpenMode.ReadOnly
            };
            using var existing = new SqliteConnection(inspection.ToString());
            existing.Open();
            using var version = existing.CreateCommand();
            version.CommandText = "PRAGMA user_version;";

            if (Convert.ToInt64(version.ExecuteScalar(), CultureInfo.InvariantCulture) != 1)
            {
                throw new StorageInitializationException(
                    "This database version is not supported. Your data has not been replaced. Use a compatible application version.");
            }

            using var integrity = existing.CreateCommand();
            integrity.CommandText = "PRAGMA quick_check;";

            if (!string.Equals(integrity.ExecuteScalar() as string, "ok", StringComparison.Ordinal))
            {
                throw new StorageInitializationException("The database is damaged. Your data has not been replaced.");
            }

            using var schema = existing.CreateCommand();
            schema.CommandText = $"SELECT {Columns} FROM work_entries LIMIT 0;";
            using var reader = schema.ExecuteReader();
            using var writable = OpenConnection();

            return;
        }

        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE work_entries (
                creation_order INTEGER PRIMARY KEY AUTOINCREMENT,
                entry_id TEXT NOT NULL UNIQUE,
                work_date TEXT NOT NULL CHECK(length(work_date) = 10 AND work_date GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]'),
                jira_id TEXT NOT NULL CHECK(length(trim(jira_id)) > 0 AND jira_id = trim(jira_id)),
                title TEXT NOT NULL CHECK(length(trim(title)) > 0 AND title = trim(title)),
                description TEXT NOT NULL DEFAULT '',
                duration_minutes INTEGER NOT NULL CHECK(duration_minutes BETWEEN 1 AND 1440)
            ) STRICT;
            CREATE INDEX work_entries_date_order ON work_entries(work_date, creation_order);
            PRAGMA user_version = 1;
            """;
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    public Task<IReadOnlyList<WorkEntry>> ReadAsync(DateOnly date, CancellationToken cancellationToken = default) =>
        ExecuteAsync<IReadOnlyList<WorkEntry>>(connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT {Columns} FROM work_entries WHERE work_date = $date ORDER BY creation_order;";
            command.Parameters.AddWithValue("$date", FormatDate(date));
            using var reader = command.ExecuteReader();
            var entries = new List<WorkEntry>();

            while (reader.Read())
            {
                entries.Add(ReadEntry(reader));
            }

            return entries.AsReadOnly();
        }, cancellationToken);

    public Task<WorkEntryHistoryPage> ReadHistoryAsync(DateOnly beforeExclusive, int dayLimit,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dayLimit);

        return ReadHistoryPageAsync(null, beforeExclusive, dayLimit, cancellationToken);
    }

    public Task<WorkEntryHistoryPage> ReadHistoryRangeAsync(DateOnly fromInclusive, DateOnly beforeExclusive,
        CancellationToken cancellationToken = default)
    {
        if (fromInclusive >= beforeExclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(fromInclusive));
        }

        return ReadHistoryPageAsync(fromInclusive, beforeExclusive, null, cancellationToken);
    }

    private Task<WorkEntryHistoryPage> ReadHistoryPageAsync(DateOnly? fromInclusive, DateOnly beforeExclusive,
        int? dayLimit, CancellationToken cancellationToken) =>
        ExecuteAsync(connection =>
        {
            using var transaction = connection.BeginTransaction(deferred: true);
            var dates = new List<DateOnly>();

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = fromInclusive.HasValue
                    ? "SELECT DISTINCT work_date FROM work_entries WHERE work_date >= $from AND work_date < $before ORDER BY work_date DESC;"
                    : "SELECT DISTINCT work_date FROM work_entries WHERE work_date < $before ORDER BY work_date DESC LIMIT $limit;";
                command.Parameters.AddWithValue("$before", FormatDate(beforeExclusive));

                if (fromInclusive.HasValue)
                {
                    command.Parameters.AddWithValue("$from", FormatDate(fromInclusive.Value));
                }
                else
                {
                    command.Parameters.AddWithValue("$limit", (long)dayLimit!.Value + 1);
                }

                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    dates.Add(DateOnly.ParseExact(reader.GetString(0), "yyyy-MM-dd", CultureInfo.InvariantCulture));
                }
            }

            var hasOlder = dayLimit.HasValue && dates.Count > dayLimit.Value;

            if (hasOlder)
            {
                dates.RemoveAt(dates.Count - 1);
            }

            if (fromInclusive.HasValue)
            {
                using var lookahead = connection.CreateCommand();
                lookahead.Transaction = transaction;
                lookahead.CommandText = "SELECT EXISTS(SELECT 1 FROM work_entries WHERE work_date < $from);";
                lookahead.Parameters.AddWithValue("$from", FormatDate(fromInclusive.Value));
                hasOlder = Convert.ToInt64(lookahead.ExecuteScalar(), CultureInfo.InvariantCulture) != 0;
            }

            var days = new List<WorkdaySnapshot>();

            foreach (var date in dates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = $"SELECT {Columns} FROM work_entries WHERE work_date = $date ORDER BY creation_order;";
                command.Parameters.AddWithValue("$date", FormatDate(date));
                using var reader = command.ExecuteReader();
                var entries = new List<WorkEntry>();

                while (reader.Read())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    entries.Add(ReadEntry(reader));
                }

                days.Add(new WorkdaySnapshot(date, entries.AsReadOnly()));
            }

            cancellationToken.ThrowIfCancellationRequested();
            transaction.Commit();

            return new WorkEntryHistoryPage(days, hasOlder);
        }, cancellationToken);

    public Task<WorkEntry> AddAsync(DateOnly date, WorkEntryValues values, CancellationToken cancellationToken = default) =>
        ExecuteAsync(connection =>
        {
            var id = Guid.NewGuid();
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"""
                INSERT INTO work_entries(entry_id, work_date, jira_id, title, description, duration_minutes)
                VALUES($id, $date, $jira, $title, $description, $minutes)
                RETURNING {Columns};
                """;
            Bind(command, id, date, values);
            WorkEntry entry;

            using (var reader = command.ExecuteReader())
            {
                if (!reader.Read())
                {
                    throw new WorkEntryStoreException("The entry could not be saved.");
                }

                entry = ReadEntry(reader);
            }

            transaction.Commit();

            return entry;
        }, cancellationToken);

    public Task<WorkEntry?> UpdateAsync(Guid id, DateOnly date, WorkEntryValues values,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync<WorkEntry?>(connection =>
        {
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"""
                UPDATE work_entries SET jira_id = $jira, title = $title,
                    description = $description, duration_minutes = $minutes
                WHERE entry_id = $id AND work_date = $date
                RETURNING {Columns};
                """;
            Bind(command, id, date, values);
            WorkEntry? entry;

            using (var reader = command.ExecuteReader())
            {
                entry = reader.Read() ? ReadEntry(reader) : null;

                if (reader.Read())
                {
                    throw new WorkEntryStoreException("The entry could not be updated safely.");
                }
            }

            transaction.Commit();

            return entry;
        }, cancellationToken);

    public Task<bool> DeleteAsync(Guid id, DateOnly date, CancellationToken cancellationToken = default) =>
        ExecuteAsync(connection =>
        {
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM work_entries WHERE entry_id = $id AND work_date = $date;";
            command.Parameters.AddWithValue("$id", id.ToString("D"));
            command.Parameters.AddWithValue("$date", FormatDate(date));
            var affected = command.ExecuteNonQuery();

            if (affected is not (0 or 1))
            {
                throw new WorkEntryStoreException("The entry could not be deleted safely.");
            }

            transaction.Commit();

            return affected == 1;
        }, cancellationToken);

    private async Task<T> ExecuteAsync<T>(Func<SqliteConnection, T> operation, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // Microsoft.Data.Sqlite performs synchronous I/O even through its async APIs.
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var connection = OpenConnection();

                // Once a mutation starts, return its committed result even if cancellation arrives.
                return operation(connection);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is SqliteException or IOException or UnauthorizedAccessException
            or FormatException or OverflowException or InvalidCastException)
        {
            throw new WorkEntryStoreException("The local database operation failed.", exception);
        }
        finally
        {
            _gate.Release();
        }
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);

        try
        {
            connection.Open();

            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    private static void Bind(SqliteCommand command, Guid id, DateOnly date, WorkEntryValues values)
    {
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        command.Parameters.AddWithValue("$date", FormatDate(date));
        command.Parameters.AddWithValue("$jira", values.JiraId.Trim());
        command.Parameters.AddWithValue("$title", values.Title.Trim());
        command.Parameters.AddWithValue("$description", values.Description.Trim());
        command.Parameters.AddWithValue("$minutes", values.DurationMinutes);
    }

    private static WorkEntry ReadEntry(SqliteDataReader reader)
    {
        return new WorkEntry(Guid.ParseExact(reader.GetString(0), "D"),
            DateOnly.ParseExact(reader.GetString(1), "yyyy-MM-dd", CultureInfo.InvariantCulture),
            reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetInt32(5), reader.GetInt64(6));
    }

    private static string FormatDate(DateOnly date)
    {
        return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public void Dispose()
    {
        _gate.Wait();

        try
        {
            _disposed = true;
        }
        finally
        {
            _gate.Release();
        }
    }
}
