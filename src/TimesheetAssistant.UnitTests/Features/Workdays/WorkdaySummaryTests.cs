using TimesheetAssistant.Core.Features.Workdays;

namespace TimesheetAssistant.UnitTests.Features.Workdays;

public class WorkdaySummaryTests
{
    [Fact]
    public void Creates_jira_and_mywork_rows_for_us_01()
    {
        // Arrange
        var entries = new[]
        {
            Entry("T-1", "Scrum Ceremonies", "Refinement", 60, 1),
            Entry("X-1", "Order Page", "", 372, 2)
        };

        // Act
        var summary = WorkdaySummary.Create(entries);

        // Assert
        summary.JiraRows.ShouldBe([
            new JiraSummaryRow("T-1", "Scrum Ceremonies", "Refinement", 60),
            new JiraSummaryRow("X-1", "Order Page", "", 372)
        ]);
        summary.MyWorkRows.ShouldBe([
            new MyWorkSummaryRow("T-1 Scrum Ceremonies - Refinement", 60),
            new MyWorkSummaryRow("X-1 Order Page", 420)
        ]);
        summary.JiraTotalMinutes.ShouldBe(432);
        summary.MyWorkTotalMinutes.ShouldBe(480);
    }

    [Fact]
    public void Keeps_repeated_ticket_entries_separate_and_assigns_adjustment_by_entry_duration()
    {
        // Arrange
        var entries = new[]
        {
            Entry("A", "Task A", "First", 120, 1),
            Entry("A", "Task A", "Second", 120, 2),
            Entry("B", "Task B", "", 192, 3)
        };

        // Act
        var summary = WorkdaySummary.Create(entries);

        // Assert
        summary.JiraRows.Count.ShouldBe(3);
        summary.MyWorkRows.Select(row => row.DurationMinutes).ShouldBe([120, 120, 240]);
    }

    [Fact]
    public void Gives_tie_adjustment_to_first_created_entry_even_when_input_is_unsorted()
    {
        // Arrange
        var entries = new[]
        {
            Entry("SECOND", "Second", "", 216, 2),
            Entry("FIRST", "First", "", 216, 1)
        };

        // Act
        var summary = WorkdaySummary.Create(entries);

        // Assert
        summary.MyWorkRows.ShouldBe([
            new MyWorkSummaryRow("FIRST First", 264),
            new MyWorkSummaryRow("SECOND Second", 216)
        ]);
    }

    [Theory]
    [InlineData(431)]
    [InlineData(433)]
    public void Rejects_totals_other_than_the_daily_target(int totalMinutes)
    {
        // Arrange
        var entries = new[] { Entry("T-1", "Task", "", totalMinutes, 1) };

        // Act
        var act = () => WorkdaySummary.Create(entries);

        // Assert
        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void Rejects_empty_or_mixed_date_entries()
    {
        // Arrange
        var emptyEntries = Array.Empty<WorkEntry>();
        var mixedDateEntries = new[]
        {
            Entry("T-1", "Task", "", 216, 1, new(2026, 11, 20)),
            Entry("T-2", "Task", "", 216, 2, new(2026, 11, 21))
        };

        // Act
        var emptyAct = () => WorkdaySummary.Create(emptyEntries);
        var mixedDateAct = () => WorkdaySummary.Create(mixedDateEntries);

        // Assert
        emptyAct.ShouldThrow<ArgumentException>();
        mixedDateAct.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void Does_not_mutate_input_entries()
    {
        // Arrange
        var entries = new[]
        {
            Entry("SECOND", "Second", "", 216, 2),
            Entry("FIRST", "First", "", 216, 1)
        };
        var originalOrder = entries.ToArray();

        // Act
        WorkdaySummary.Create(entries);

        // Assert
        entries.ShouldBe(originalOrder);
    }

    private static WorkEntry Entry(string jiraId, string title, string description, int durationMinutes,
        long creationOrder, DateOnly? workDate = null)
    {
        return new WorkEntry(Guid.NewGuid(), workDate ?? new DateOnly(2026, 11, 20), jiraId, title, description,
            durationMinutes, creationOrder);
    }
}
