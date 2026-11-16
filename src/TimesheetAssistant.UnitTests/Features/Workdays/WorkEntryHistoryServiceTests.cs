using TimesheetAssistant.Core.Features.Workdays;

namespace TimesheetAssistant.UnitTests.Features.Workdays;

public class WorkEntryHistoryServiceTests
{
    private static readonly DateOnly _today = new(2026, 11, 16);
    private static readonly WorkEntryValues _values = new("T-1", "Title", "Full description", 60);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task Initial_window_contains_up_to_three_nonempty_dates_and_separate_today(int count)
    {
        // Arrange
        var store = await StoreWithDaysAsync(count);
        var todayEntry = await store.AddAsync(_today, _values);
        await store.AddAsync(_today.AddDays(1), _values);
        var service = new WorkEntryHistoryService(store, () => _today);

        // Act
        var result = await service.LoadAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        service.Snapshot!.Today.Entries.ShouldHaveSingleItem().ShouldBe(todayEntry);
        service.Snapshot.Days.Select(day => day.Date).ShouldBe(Dates(count).Take(3));
        service.Snapshot.HasOlder.ShouldBe(count > 3);
        service.Snapshot.OldestLoadedBoundary.ShouldBe(count == 0 ? null : Dates(count).Take(3).Last());
    }

    [Fact]
    public async Task Paging_appends_one_complete_day_and_preserves_duplicate_tickets_and_creation_order()
    {
        // Arrange
        var store = await StoreWithDaysAsync(4);
        var oldest = Dates(4).Last();
        var repeated = await store.AddAsync(oldest, _values);
        var service = new WorkEntryHistoryService(store, () => _today);
        await service.LoadAsync();
        var initial = service.Snapshot!;

        // Act
        await service.LoadOlderAsync();
        await service.LoadOlderAsync();

        // Assert
        service.Snapshot!.Days.Select(day => day.Date).ShouldBe(Dates(4));
        service.Snapshot.Days.Last().Entries.Count.ShouldBe(2);
        service.Snapshot.Days.Last().Entries.Last().ShouldBe(repeated);
        service.Snapshot.Days.Last().Totals.TotalMinutes.ShouldBe(120);
        service.Snapshot.HasOlder.ShouldBeFalse();
        store.HistoryRequests.Last().ShouldBe((initial.OldestLoadedBoundary!.Value, 1));
        store.HistoryRequests.Count.ShouldBe(2);
        initial.Days.Count.ShouldBe(3);
        service.Snapshot.Days.ShouldBeAssignableTo<IList<WorkdaySnapshot>>().IsReadOnly.ShouldBeTrue();
        service.Snapshot.Days.Last().Entries.ShouldBeAssignableTo<IList<WorkEntry>>().IsReadOnly.ShouldBeTrue();
    }

    [Theory]
    [InlineData("initial")]
    [InlineData("older")]
    [InlineData("refresh")]
    public async Task Storage_failure_preserves_state_and_retry_uses_the_same_cursor(string operation)
    {
        // Arrange
        var store = await StoreWithDaysAsync(5);
        var service = new WorkEntryHistoryService(store, () => _today);
        if (operation != "initial")
        {
            await service.LoadAsync();
        }
        var previous = service.Snapshot;
        store.Fail = true;

        // Act
        var failed = await OperateAsync(service, operation);
        var retained = service.Snapshot;
        store.Fail = false;
        var retry = await OperateAsync(service, operation);

        // Assert
        failed.FailureReason.ShouldBe(WorkdayFailure.Storage);
        retained.ShouldBeSameAs(previous);
        retry.IsSuccess.ShouldBeTrue();
        service.Snapshot!.Days.Count.ShouldBe(operation == "older" ? 4 : 3);
    }

    [Fact]
    public async Task Refresh_updates_retained_interval_without_changing_editor_or_deleted_boundary()
    {
        // Arrange
        var store = await StoreWithDaysAsync(6);
        var service = new WorkEntryHistoryService(store, () => _today);
        var editor = new WorkdayService(store, () => _today);
        await editor.LoadAsync(_today);
        var editorSnapshot = editor.Snapshot;
        await service.LoadAsync();
        await service.LoadOlderAsync();
        var boundary = service.Snapshot!.OldestLoadedBoundary!.Value;
        var removed = service.Snapshot.Days.Last().Entries.Single();
        var edited = service.Snapshot.Days.First().Entries.Single();
        await store.DeleteAsync(removed.Id, boundary);
        await store.UpdateAsync(edited.Id, edited.WorkDate, _values with { Title = "Edited", DurationMinutes = 2 });
        var added = await store.AddAsync(_today.AddDays(-2), _values);

        // Act
        await service.RefreshAsync();
        var refreshed = service.Snapshot!;
        await service.LoadOlderAsync();

        // Assert
        refreshed.OldestLoadedBoundary.ShouldBe(boundary);
        refreshed.Days.Any(day => day.Date == boundary).ShouldBeFalse();
        refreshed.Days.First().Entries.Single().Title.ShouldBe("Edited");
        refreshed.Days.SelectMany(day => day.Entries).ShouldContain(added);
        service.Snapshot!.Days.Last().Date.ShouldBe(Dates(6).ElementAt(4));
        store.HistoryRequests.Last().ShouldBe((boundary, 1));
        editor.Snapshot.ShouldBeSameAs(editorSnapshot);
    }

    [Fact]
    public async Task Refresh_refills_to_three_after_last_entry_deletions_and_extends_boundary()
    {
        // Arrange
        var store = await StoreWithDaysAsync(5);
        var service = new WorkEntryHistoryService(store, () => _today);
        await service.LoadAsync();
        foreach (var day in service.Snapshot!.Days.Take(2))
        {
            await store.DeleteAsync(day.Entries.Single().Id, day.Date);
        }

        // Act
        await service.RefreshAsync();

        // Assert
        service.Snapshot!.Days.Select(day => day.Date).ShouldBe(Dates(5).Skip(2));
        service.Snapshot.OldestLoadedBoundary.ShouldBe(Dates(5).Last());
        service.Snapshot.HasOlder.ShouldBeFalse();
    }

    [Fact]
    public async Task Cancellation_during_refill_preserves_today_groups_and_cursor_atomically()
    {
        // Arrange
        var store = await StoreWithDaysAsync(5);
        var service = new WorkEntryHistoryService(store, () => _today);
        await service.LoadAsync();
        var previous = service.Snapshot!;
        await store.DeleteAsync(previous.Days.First().Entries.Single().Id, previous.Days.First().Date);
        await store.AddAsync(_today, _values);
        using var cancellation = new CancellationTokenSource();
        var reads = 0;
        store.BeforeHistoryReadAsync = token =>
        {
            if (++reads == 2)
            {
                cancellation.Cancel();
            }

            return Task.CompletedTask;
        };

        // Act
        var operation = service.RefreshAsync(cancellation.Token);

        // Assert
        await Should.ThrowAsync<OperationCanceledException>(() => operation);
        service.Snapshot.ShouldBeSameAs(previous);
        reads.ShouldBe(2);
    }

    [Fact]
    public async Task Queued_operations_are_serialized_and_publish_only_complete_snapshots()
    {
        // Arrange
        var store = await StoreWithDaysAsync(5);
        var service = new WorkEntryHistoryService(store, () => _today);
        await service.LoadAsync();
        var previous = service.Snapshot;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        store.BeforeHistoryReadAsync = async token =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(token);
        };

        // Act
        var first = service.LoadOlderAsync();
        await entered.Task;
        var second = service.LoadOlderAsync();
        var pendingSnapshot = service.Snapshot;
        var pendingRequests = store.HistoryRequests.Count;
        release.SetResult();
        await Task.WhenAll(first, second);

        // Assert
        pendingSnapshot.ShouldBeSameAs(previous);
        pendingRequests.ShouldBe(2);
        service.Snapshot!.Days.Select(day => day.Date).ShouldBe(Dates(5));
        store.HistoryRequests.TakeLast(2).Select(request => request.BeforeExclusive)
            .ShouldBe(Dates(5).Skip(2).Take(2));
    }

    [Fact]
    public async Task Rollover_rebuilds_initial_window_and_failed_rollover_preserves_previous_state()
    {
        // Arrange
        var store = await StoreWithDaysAsync(5);
        await store.AddAsync(_today, _values);
        var current = _today;
        var service = new WorkEntryHistoryService(store, () => current);
        await service.LoadAsync();
        await service.LoadOlderAsync();
        var previous = service.Snapshot;
        current = current.AddDays(1);
        store.Fail = true;

        // Act
        var failed = await service.RefreshAsync();
        var retained = service.Snapshot;
        store.Fail = false;
        await service.LoadOlderAsync();

        // Assert
        failed.FailureReason.ShouldBe(WorkdayFailure.Storage);
        retained.ShouldBeSameAs(previous);
        service.Snapshot!.LocalToday.ShouldBe(current);
        service.Snapshot.Today.Entries.ShouldBeEmpty();
        service.Snapshot.Days.Select(day => day.Date).ShouldBe(new[] { _today }.Concat(Dates(2)));
        service.Snapshot.OldestLoadedBoundary.ShouldBe(Dates(2).Last());
    }

    private static IEnumerable<DateOnly> Dates(int count) =>
        Enumerable.Range(0, count).Select(index => _today.AddDays(-1 - index * 7));

    private static async Task<FakeWorkEntryStore> StoreWithDaysAsync(int count)
    {
        var store = new FakeWorkEntryStore();
        foreach (var date in Dates(count))
        {
            await store.AddAsync(date, _values);
        }

        return store;
    }

    private static Task<WorkdayResult> OperateAsync(WorkEntryHistoryService service, string operation) => operation switch
    {
        "initial" => service.LoadAsync(),
        "older" => service.LoadOlderAsync(),
        "refresh" => service.RefreshAsync(),
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };
}
