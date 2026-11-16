namespace TimesheetAssistant.Core.Features.Workdays;

public sealed class WorkEntryHistoryService(IWorkEntryStore store, Func<DateOnly> localToday)
{
    private const int InitialDayCount = 3;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public WorkEntryHistorySnapshot? Snapshot { get; private set; }

    public Task<WorkdayResult> LoadAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(HistoryOperation.Initial, cancellationToken);

    public Task<WorkdayResult> LoadOlderAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(HistoryOperation.Older, cancellationToken);

    public Task<WorkdayResult> RefreshAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(HistoryOperation.Refresh, cancellationToken);

    private async Task<WorkdayResult> ExecuteAsync(HistoryOperation operation, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var today = localToday();
            var previous = Snapshot;
            WorkEntryHistoryPage page;
            DateOnly? boundary;
            WorkdaySnapshot todaySnapshot;

            if (operation == HistoryOperation.Initial || previous is null || previous.LocalToday != today)
            {
                todaySnapshot = await ReadTodayAsync(today, cancellationToken);
                page = await store.ReadHistoryAsync(today, InitialDayCount, cancellationToken);
                boundary = page.Days.LastOrDefault()?.Date;
            }
            else if (operation == HistoryOperation.Older)
            {
                if (!previous.HasOlder)
                {
                    return WorkdayResult.Success();
                }

                var older = await store.ReadHistoryAsync(previous.OldestLoadedBoundary ?? today, 1, cancellationToken);
                page = new WorkEntryHistoryPage(previous.Days.Concat(older.Days)
                    .DistinctBy(day => day.Date).OrderByDescending(day => day.Date), older.HasOlder);
                boundary = older.Days.LastOrDefault()?.Date ?? previous.OldestLoadedBoundary;
                todaySnapshot = previous.Today;
            }
            else
            {
                todaySnapshot = await ReadTodayAsync(today, cancellationToken);
                boundary = previous.OldestLoadedBoundary;
                page = boundary.HasValue
                    ? await store.ReadHistoryRangeAsync(boundary.Value, today, cancellationToken)
                    : await store.ReadHistoryAsync(today, InitialDayCount, cancellationToken);

                if (page.Days.Count < InitialDayCount && page.HasOlder)
                {
                    var older = await store.ReadHistoryAsync(boundary ?? today,
                        InitialDayCount - page.Days.Count, cancellationToken);
                    page = new WorkEntryHistoryPage(page.Days.Concat(older.Days)
                        .DistinctBy(day => day.Date).OrderByDescending(day => day.Date), older.HasOlder);
                }

                var oldest = page.Days.LastOrDefault()?.Date;
                boundary = boundary.HasValue && (!oldest.HasValue || boundary.Value < oldest.Value)
                    ? boundary : oldest;
            }

            cancellationToken.ThrowIfCancellationRequested();
            Snapshot = new WorkEntryHistorySnapshot(today, todaySnapshot, page, boundary);

            return WorkdayResult.Success();
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

    private async Task<WorkdaySnapshot> ReadTodayAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var entries = await store.ReadAsync(today, cancellationToken);

        return new WorkdaySnapshot(today, Array.AsReadOnly(entries.ToArray()));
    }

    private enum HistoryOperation
    {
        Initial,
        Older,
        Refresh
    }
}
