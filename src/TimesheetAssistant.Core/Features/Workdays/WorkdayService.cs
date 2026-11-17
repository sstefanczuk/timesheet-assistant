namespace TimesheetAssistant.Core.Features.Workdays;

public enum WorkdayFailure
{
    Validation,
    NotFound,
    Storage
}

public sealed record WorkdayResult
{
    private WorkdayResult(bool isSuccess, WorkdayFailure? failureReason = null,
        IReadOnlyDictionary<string, string>? errors = null)
    {
        IsSuccess = isSuccess;
        FailureReason = failureReason;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public WorkdayFailure? FailureReason { get; }

    public IReadOnlyDictionary<string, string>? Errors { get; }

    public static WorkdayResult Success()
    {
        return new WorkdayResult(true);
    }

    public static WorkdayResult Failure(WorkdayFailure reason, IReadOnlyDictionary<string, string>? errors = null)
    {
        return new WorkdayResult(false, reason, errors);
    }
}

public sealed record WorkdaySnapshot(DateOnly Date, IReadOnlyList<WorkEntry> Entries)
{
    public WorkdayTotals Totals => WorkdayTotals.Calculate(Entries);
}

public sealed class WorkdayService(IWorkEntryStore store, Func<DateOnly> localToday)
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public WorkdaySnapshot? Snapshot { get; private set; }

    public Task<WorkdayResult> LoadAsync(DateOnly date, CancellationToken cancellationToken = default) =>
        LoadAsync(date, null, cancellationToken);

    public Task<WorkdayResult> LoadEntryAsync(DateOnly date, Guid id, CancellationToken cancellationToken = default) =>
        LoadAsync(date, id, cancellationToken);

    private Task<WorkdayResult> LoadAsync(DateOnly date, Guid? requiredEntryId, CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            if (date > localToday())
            {
                return InvalidDate();
            }

            var entries = await store.ReadAsync(date, cancellationToken);

            if (requiredEntryId.HasValue && !entries.Any(entry => entry.Id == requiredEntryId.Value))
            {
                return WorkdayResult.Failure(WorkdayFailure.NotFound);
            }

            Snapshot = new WorkdaySnapshot(date, Array.AsReadOnly(entries.ToArray()));

            return WorkdayResult.Success();
        }, cancellationToken);

    public Task<WorkdayResult> AddAsync(DateOnly date, WorkEntryInput input, CancellationToken cancellationToken = default) =>
        SaveAsync(null, date, input, cancellationToken);

    public Task<WorkdayResult> UpdateAsync(Guid id, DateOnly date, WorkEntryInput input, CancellationToken cancellationToken = default) =>
        SaveAsync(id, date, input, cancellationToken);

    private Task<WorkdayResult> SaveAsync(Guid? id, DateOnly date, WorkEntryInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            var validation = WorkEntryValidator.Validate(date, localToday(), input);

            if (!validation.IsValid)
            {
                return WorkdayResult.Failure(WorkdayFailure.Validation, validation.Errors);
            }

            var entry = id.HasValue
                ? await store.UpdateAsync(id.Value, date, validation.Values!, cancellationToken)
                : await store.AddAsync(date, validation.Values!, cancellationToken);

            if (entry is null)
            {
                return WorkdayResult.Failure(WorkdayFailure.NotFound);
            }

            if (Snapshot?.Date == date)
            {
                var entries = Snapshot.Entries
                    .Where(e => e.Id != entry.Id)
                    .Append(entry)
                    .OrderBy(e => e.CreationOrder)
                    .ToArray();

                Snapshot = new WorkdaySnapshot(date, Array.AsReadOnly(entries));
            }

            return WorkdayResult.Success();
        }, cancellationToken);

    public Task<WorkdayResult> DeleteAsync(Guid id, DateOnly date, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            if (date > localToday())
            {
                return InvalidDate();
            }

            if (!await store.DeleteAsync(id, date, cancellationToken))
            {
                return WorkdayResult.Failure(WorkdayFailure.NotFound);
            }

            if (Snapshot?.Date == date)
            {
                var entries = Snapshot.Entries.Where(e => e.Id != id).ToArray();
                Snapshot = new WorkdaySnapshot(date, Array.AsReadOnly(entries));
            }

            return WorkdayResult.Success();
        }, cancellationToken);

    private async Task<WorkdayResult> ExecuteAsync(Func<Task<WorkdayResult>> operation, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            return await operation();
        }
        catch (WorkEntryStoreException)
        {
            return WorkdayResult.Failure(WorkdayFailure.Storage);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static WorkdayResult InvalidDate()
    {
        var errors = new Dictionary<string, string>
        {
            ["WorkDate"] = "Work date cannot be in the future."
        };

        return WorkdayResult.Failure(WorkdayFailure.Validation, errors.AsReadOnly());
    }
}
