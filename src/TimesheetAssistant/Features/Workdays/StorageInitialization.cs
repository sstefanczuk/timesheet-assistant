using System.ComponentModel;
using System.Security;
using Microsoft.Data.Sqlite;

namespace TimesheetAssistant.Features.Workdays;

public sealed class StorageInitialization : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DataDirectoryLease? _lease;
    private bool _disposed;

    public SqliteWorkEntryStore? Store { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task InitializeAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);

        try
        {
            if (_disposed || Store is not null)
            {
                return;
            }

            ErrorMessage = null;
            await Task.Run(Initialize).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private void Initialize()
    {
        DataDirectoryLease? lease = null;
        SqliteWorkEntryStore? store = null;

        try
        {
            var directory = DataDirectory.Resolve();
            lease = DataDirectoryLease.Acquire(directory);
            var databasePath = Path.Combine(directory, "timesheet.db");
            var exists = Path.Exists(databasePath);

            if (exists && (File.GetAttributes(databasePath) & FileAttributes.ReparsePoint) != 0)
            {
                throw new StorageInitializationException("The database must be a regular file in the data directory.");
            }

            if (!exists)
            {
                // Existing empty or unversioned files are never treated as new stores.
                using var newFile = new FileStream(databasePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            }

            store = new SqliteWorkEntryStore(databasePath);
            store.Initialize(isNew: !exists);
            Store = store;
            _lease = lease;
            store = null;
            lease = null;
        }
        catch (StorageInitializationException exception)
        {
            ErrorMessage = exception.Message;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or SecurityException or ArgumentException or NotSupportedException or Win32Exception
            or SqliteException or DllNotFoundException or TypeInitializationException or BadImageFormatException)
        {
            // Provider messages can contain paths or record values; never expose or log them.
            ErrorMessage = "Local storage could not be opened. Check the data directory, file permissions and database, then select Retry. Existing data has not been replaced.";
        }
        finally
        {
            store?.Dispose();
            lease?.Dispose();
        }
    }

    public void Dispose()
    {
        _gate.Wait();

        try
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Store?.Dispose();
            _lease?.Dispose();
        }
        finally
        {
            _gate.Release();
        }
    }
}

internal sealed class StorageInitializationException(string message, Exception? innerException = null)
    : Exception(message, innerException);
