using TimesheetAssistant.Core.Features.Workdays;

namespace TimesheetAssistant.UnitTests.Features.Workdays;

public class WorkdayTotalsTests
{
    [Theory]
    [InlineData(0, 432, 0)]
    [InlineData(60, 372, 0)]
    [InlineData(431, 1, 0)]
    [InlineData(432, 0, 0)]
    [InlineData(433, 0, 1)]
    [InlineData(2880, 0, 2448)]
    public void Calculates_target_remaining_and_excess(int total, long remaining, long excess)
    {
        // Arrange
        var entries = EntriesWithTotal(total);

        // Act
        var totals = WorkdayTotals.Calculate(entries);

        // Assert
        totals.TotalMinutes.ShouldBe(total);
        totals.RemainingMinutes.ShouldBe(remaining);
        totals.ExcessMinutes.ShouldBe(excess);
    }

    [Fact]
    public void Aggregate_uses_more_than_a_32_bit_integer()
    {
        // Arrange
        var entry = new WorkEntry(Guid.NewGuid(), new(2026, 11, 12), "T-1", "Title", "", 1440, 1);
        var entries = Enumerable.Repeat(entry, 1_500_000);

        // Act
        var totals = WorkdayTotals.Calculate(entries);

        // Assert
        totals.TotalMinutes.ShouldBe(2_160_000_000L);
    }

    private static IEnumerable<WorkEntry> EntriesWithTotal(int total)
    {
        while (total > 0)
        {
            var minutes = Math.Min(total, 1440);

            yield return new(Guid.NewGuid(), new(2026, 11, 12), "T-1", "Title", "", minutes, total);
            total -= minutes;
        }
    }
}
