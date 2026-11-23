using TimesheetAssistant.Core.Features.Workdays;

namespace TimesheetAssistant.UnitTests.Features.Workdays;

public class WorkdayDateSuggestionTests
{
    private static readonly DateOnly _today = new(2026, 11, 16);
    private static readonly WorkEntryValues _values = new("T-1", "Title", "Description", 60);

    [Fact]
    public async Task Suggestion_uses_the_earliest_incomplete_weekday_after_source()
    {
        // Arrange
        var store = new FakeWorkEntryStore();
        var source = new DateOnly(2026, 11, 9);
        var tuesday = source.AddDays(1);
        var wednesday = source.AddDays(2);
        await AddMinutesAsync(store, tuesday, WorkdayTotals.TargetMinutes);
        await AddMinutesAsync(store, wednesday, 90);
        var service = new WorkdayService(store, () => _today);

        // Act
        var result = await service.SuggestNextIncompleteWeekdayAsync(source);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.SuggestedDate.ShouldBe(wednesday);
    }

    [Fact]
    public async Task Suggestion_skips_complete_weekdays_and_weekends_until_first_incomplete_weekday()
    {
        // Arrange
        var store = new FakeWorkEntryStore();
        var thursday = new DateOnly(2026, 11, 12);
        var friday = new DateOnly(2026, 11, 13);
        await AddMinutesAsync(store, thursday, WorkdayTotals.TargetMinutes);
        await AddMinutesAsync(store, friday, WorkdayTotals.TargetMinutes + 30);
        await AddMinutesAsync(store, friday.AddDays(1), 10);
        await AddMinutesAsync(store, _today, 120);
        var service = new WorkdayService(store, () => _today);

        // Act
        var result = await service.SuggestNextIncompleteWeekdayAsync(new DateOnly(2026, 11, 11));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.SuggestedDate.ShouldBe(_today);
    }

    [Fact]
    public async Task Suggestion_includes_empty_weekdays_as_incomplete()
    {
        // Arrange
        var store = new FakeWorkEntryStore();
        var service = new WorkdayService(store, () => _today);
        var source = new DateOnly(2026, 11, 13);

        // Act
        var result = await service.SuggestNextIncompleteWeekdayAsync(source);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.SuggestedDate.ShouldBe(_today);
    }

    [Fact]
    public async Task Suggestion_falls_back_to_today_when_every_remaining_weekday_is_complete()
    {
        // Arrange
        var store = new FakeWorkEntryStore();
        var source = new DateOnly(2026, 11, 12);
        await AddMinutesAsync(store, new DateOnly(2026, 11, 13), WorkdayTotals.TargetMinutes);
        await AddMinutesAsync(store, _today, WorkdayTotals.TargetMinutes);
        var service = new WorkdayService(store, () => _today);

        // Act
        var result = await service.SuggestNextIncompleteWeekdayAsync(source);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.SuggestedDate.ShouldBe(_today);
    }

    [Fact]
    public async Task Suggestion_returns_today_without_storage_reads_for_source_today()
    {
        // Arrange
        var store = new FakeWorkEntryStore { Fail = true };
        var service = new WorkdayService(store, () => _today);

        // Act
        var result = await service.SuggestNextIncompleteWeekdayAsync(_today);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.SuggestedDate.ShouldBe(_today);
        store.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task Suggestion_reports_storage_failure_without_a_date()
    {
        // Arrange
        var store = new FakeWorkEntryStore { Fail = true };
        var service = new WorkdayService(store, () => _today);
        var source = new DateOnly(2026, 11, 13);

        // Act
        var result = await service.SuggestNextIncompleteWeekdayAsync(source);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.SuggestedDate.ShouldBeNull();
        result.FailureReason.ShouldBe(WorkdayFailure.Storage);
    }

    private static async Task AddMinutesAsync(FakeWorkEntryStore store, DateOnly date, int minutes)
    {
        var remaining = minutes;

        while (remaining > 0)
        {
            var duration = Math.Min(remaining, 1440);
            await store.AddAsync(date, _values with { DurationMinutes = duration });
            remaining -= duration;
        }
    }
}
