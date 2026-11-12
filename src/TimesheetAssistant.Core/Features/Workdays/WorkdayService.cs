namespace TimesheetAssistant.Core.Features.Workdays;

public enum WorkdayFailure { Validation, NotFound, Storage }

public sealed record WorkdayResult(bool IsSuccess, WorkdayFailure? Failure = null,
    IReadOnlyDictionary<string, string>? Errors = null);

public sealed record WorkdaySnapshot(DateOnly Date, IReadOnlyList<WorkEntry> Entries)
{
    public WorkdayTotals Totals => WorkdayTotals.Calculate(Entries);
}

public sealed class WorkdayService(IWorkEntryStore store, Func<DateOnly> localToday)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    public WorkdaySnapshot? Snapshot { get; private set; }

    public Task<WorkdayResult> LoadAsync(DateOnly date, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            if (date > localToday()) return InvalidDate();
            var entries = await store.ReadAsync(date, cancellationToken);
            Snapshot = new(date, Array.AsReadOnly(entries.ToArray()));
            return new(true);
        }, cancellationToken);

    public Task<WorkdayResult> AddAsync(DateOnly date, WorkEntryInput input, CancellationToken cancellationToken = default) =>
        SaveAsync(null, date, input, cancellationToken);

    public Task<WorkdayResult> UpdateAsync(Guid id, DateOnly date, WorkEntryInput input, CancellationToken cancellationToken = default) =>
        SaveAsync(id, date, input, cancellationToken);

    private Task<WorkdayResult> SaveAsync(Guid? id, DateOnly date, WorkEntryInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            var validation = WorkEntryValidator.Validate(date, localToday(), input);
            if (!validation.IsValid) return new(false, WorkdayFailure.Validation, validation.Errors);
            var entry = id.HasValue
                ? await store.UpdateAsync(id.Value, date, validation.Values!, cancellationToken)
                : await store.AddAsync(date, validation.Values!, cancellationToken);
            if (entry is null) return new(false, WorkdayFailure.NotFound);
            if (Snapshot?.Date == date)
                Snapshot = new(date, Array.AsReadOnly(Snapshot.Entries.Where(e => e.Id != entry.Id)
                    .Append(entry).OrderBy(e => e.CreationOrder).ToArray()));
            return new(true);
        }, cancellationToken);

    public Task<WorkdayResult> DeleteAsync(Guid id, DateOnly date, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            if (date > localToday()) return InvalidDate();
            if (!await store.DeleteAsync(id, date, cancellationToken)) return new(false, WorkdayFailure.NotFound);
            if (Snapshot?.Date == date)
                Snapshot = new(date, Array.AsReadOnly(Snapshot.Entries.Where(e => e.Id != id).ToArray()));
            return new(true);
        }, cancellationToken);

    private async Task<WorkdayResult> ExecuteAsync(Func<Task<WorkdayResult>> operation, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try { return await operation(); }
        catch (WorkEntryStoreException) { return new(false, WorkdayFailure.Storage); }
        finally { gate.Release(); }
    }

    private static WorkdayResult InvalidDate() => new(false, WorkdayFailure.Validation,
        new Dictionary<string, string> { ["WorkDate"] = "Work date cannot be in the future." }.AsReadOnly());
}
