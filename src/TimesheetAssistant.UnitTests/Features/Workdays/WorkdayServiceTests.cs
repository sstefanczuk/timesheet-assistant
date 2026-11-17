using TimesheetAssistant.Core.Features.Workdays;

namespace TimesheetAssistant.UnitTests.Features.Workdays;

public class WorkdayServiceTests
{
    private static readonly DateOnly _today = new(2026, 11, 12);

    private static WorkEntryInput Input(string duration = "1h") => new("T-1", "Title", null, duration);

    [Theory]
    [InlineData("add")]
    [InlineData("update")]
    [InlineData("delete")]
    [InlineData("load")]
    public async Task Future_dates_do_not_call_the_store(string operation)
    {
        // Arrange
        var store = new FakeWorkEntryStore();
        var service = new WorkdayService(store, () => _today);
        var future = _today.AddDays(1);

        // Act
        var result = await OperateAsync(service, operation, Guid.NewGuid(), future);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.FailureReason.ShouldBe(WorkdayFailure.Validation);
        store.Calls.ShouldBe(0);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("update")]
    public async Task Invalid_fields_do_not_call_the_store(string operation)
    {
        // Arrange
        var store = new FakeWorkEntryStore();
        var service = new WorkdayService(store, () => _today);
        var invalid = new WorkEntryInput(" ", " ", null, "6h60m");

        // Act
        var result = operation == "add" ? await service.AddAsync(_today, invalid)
            : await service.UpdateAsync(Guid.NewGuid(), _today, invalid);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.FailureReason.ShouldBe(WorkdayFailure.Validation);
        result.Errors!.Count.ShouldBe(3);
        store.Calls.ShouldBe(0);
    }

    [Theory]
    [InlineData(2026, 11, 12)]
    [InlineData(2026, 11, 7)]
    [InlineData(2020, 1, 1)]
    public async Task Accepts_today_past_dates_and_weekends(int year, int month, int day)
    {
        // Arrange
        var date = new DateOnly(year, month, day);
        var service = CreateService();
        await service.LoadAsync(date);

        // Act
        var result = await service.AddAsync(date, Input());

        // Assert
        result.IsSuccess.ShouldBeTrue();
        service.Snapshot!.Entries.ShouldHaveSingleItem().WorkDate.ShouldBe(date);
    }

    [Fact]
    public async Task Repeated_tickets_remain_separate_and_edits_keep_identity_and_order()
    {
        // Arrange
        var service = await DayWithTwoRepeatedTicketsAsync();
        var original = service.Snapshot!.Entries[0];
        var second = service.Snapshot.Entries[1];

        // Act
        var result = await service.UpdateAsync(original.Id, _today, new("T-1", "Changed", "Details", "2m"));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        service.Snapshot!.Entries.Count.ShouldBe(2);

        var edited = service.Snapshot.Entries[0];
        edited.Id.ShouldBe(original.Id);
        edited.CreationOrder.ShouldBe(original.CreationOrder);
        edited.WorkDate.ShouldBe(original.WorkDate);
        edited.Title.ShouldBe("Changed");
        edited.DurationMinutes.ShouldBe(2);

        service.Snapshot.Entries[1].ShouldBe(second);
        second.Id.ShouldNotBe(original.Id);
    }

    [Fact]
    public async Task Dates_have_independent_records_and_totals()
    {
        // Arrange
        var service = await DayWithTwoRepeatedTicketsAsync();
        var past = _today.AddDays(-1);
        await service.AddAsync(past, Input("2m"));
        var todaySnapshot = service.Snapshot;

        // Act
        var result = await service.LoadAsync(past);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        todaySnapshot!.Totals.TotalMinutes.ShouldBe(120);
        service.Snapshot!.Totals.TotalMinutes.ShouldBe(2);
        service.Snapshot.Entries.ShouldHaveSingleItem().WorkDate.ShouldBe(past);
    }

    [Fact]
    public async Task Edit_cannot_move_an_entry_to_another_date()
    {
        // Arrange
        var service = await DayWithTwoRepeatedTicketsAsync();
        var snapshot = service.Snapshot!;

        // Act
        var result = await service.UpdateAsync(snapshot.Entries[0].Id, _today.AddDays(-1), Input("2m"));

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.FailureReason.ShouldBe(WorkdayFailure.NotFound);
        service.Snapshot.ShouldBeSameAs(snapshot);
    }

    [Theory]
    [InlineData("update")]
    [InlineData("delete")]
    public async Task Missing_identity_retains_the_successful_snapshot(string operation)
    {
        // Arrange
        var service = await DayWithTwoRepeatedTicketsAsync();
        var snapshot = service.Snapshot;

        // Act
        var result = await OperateAsync(service, operation, Guid.NewGuid(), _today);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.FailureReason.ShouldBe(WorkdayFailure.NotFound);
        service.Snapshot.ShouldBeSameAs(snapshot);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("update")]
    [InlineData("delete")]
    [InlineData("load")]
    public async Task Storage_failures_retain_snapshot_and_committed_records(string operation)
    {
        // Arrange
        var store = new FakeWorkEntryStore();
        var service = await DayWithTwoRepeatedTicketsAsync(store);
        var snapshot = service.Snapshot!;
        store.Fail = true;

        // Act
        var result = await OperateAsync(service, operation, snapshot.Entries[0].Id, _today);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.FailureReason.ShouldBe(WorkdayFailure.Storage);
        service.Snapshot.ShouldBeSameAs(snapshot);

        store.Fail = false;
        (await store.ReadAsync(_today)).ShouldBe(snapshot.Entries);
    }

    [Fact]
    public async Task Delete_removes_only_the_requested_identity()
    {
        // Arrange
        var service = await DayWithTwoRepeatedTicketsAsync();
        var removed = service.Snapshot!.Entries[0];
        var retained = service.Snapshot.Entries[1];

        // Act
        var result = await service.DeleteAsync(removed.Id, _today);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        service.Snapshot!.Entries.ShouldHaveSingleItem().ShouldBe(retained);
        service.Snapshot.Totals.TotalMinutes.ShouldBe(60);
    }

    [Fact]
    public async Task Snapshot_copies_the_store_list_and_exposes_read_only_entries()
    {
        // Arrange
        var store = new FakeWorkEntryStore();
        var service = await DayWithTwoRepeatedTicketsAsync(store);
        await service.LoadAsync(_today);
        var snapshot = service.Snapshot!;

        // Act
        store.LastRead!.Clear();

        // Assert
        snapshot.Entries.Count.ShouldBe(2);
        snapshot.Entries.ShouldBeAssignableTo<IList<WorkEntry>>().IsReadOnly.ShouldBeTrue();
    }

    [Fact]
    public async Task Failed_read_of_another_date_does_not_relabel_previous_records()
    {
        // Arrange
        var store = new FakeWorkEntryStore();
        var service = await DayWithTwoRepeatedTicketsAsync(store);
        var snapshot = service.Snapshot!;
        store.Fail = true;

        // Act
        var result = await service.LoadAsync(_today.AddDays(-1));

        // Assert
        result.FailureReason.ShouldBe(WorkdayFailure.Storage);
        service.Snapshot.ShouldBeSameAs(snapshot);
        service.Snapshot!.Date.ShouldBe(_today);
    }

    [Fact]
    public async Task Loading_an_exact_entry_publishes_its_complete_destination_day()
    {
        // Arrange
        var store = new FakeWorkEntryStore();
        var service = await DayWithTwoRepeatedTicketsAsync(store);
        var date = _today.AddDays(-1);
        var first = await store.AddAsync(date, new("T-1", "First", "", 1));
        var requested = await store.AddAsync(date, new("T-1", "Exact destination", "Details", 2));

        // Act
        var result = await service.LoadEntryAsync(date, requested.Id);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        service.Snapshot!.Date.ShouldBe(date);
        service.Snapshot.Entries.ShouldBe(new[] { first, requested });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Unavailable_destination_entry_preserves_the_previous_editor_snapshot(bool storageFailure)
    {
        // Arrange
        var store = new FakeWorkEntryStore();
        var service = await DayWithTwoRepeatedTicketsAsync(store);
        var previous = service.Snapshot;
        var destination = _today.AddDays(-1);
        await store.AddAsync(destination, new("OTHER-1", "Another entry", "", 1));
        store.Fail = storageFailure;

        // Act
        var result = await service.LoadEntryAsync(destination, Guid.NewGuid());

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.FailureReason.ShouldBe(storageFailure ? WorkdayFailure.Storage : WorkdayFailure.NotFound);
        service.Snapshot.ShouldBeSameAs(previous);
    }

    private static WorkdayService CreateService(FakeWorkEntryStore? store = null) => new(store ?? new(), () => _today);

    private static async Task<WorkdayService> DayWithTwoRepeatedTicketsAsync(FakeWorkEntryStore? store = null)
    {
        var service = CreateService(store);
        await service.LoadAsync(_today);
        await service.AddAsync(_today, Input());
        await service.AddAsync(_today, Input());

        return service;
    }

    private static Task<WorkdayResult> OperateAsync(WorkdayService service, string operation, Guid id, DateOnly date) => operation switch
    {
        "add" => service.AddAsync(date, Input()),
        "update" => service.UpdateAsync(id, date, Input("2m")),
        "delete" => service.DeleteAsync(id, date),
        "load" => service.LoadAsync(date),
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };
}
