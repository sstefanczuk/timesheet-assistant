namespace TimesheetAssistant.Core.Features.Workdays;

public sealed record JiraSummaryRow(string JiraId, string Title, string Description, int DurationMinutes);

public sealed record MyWorkSummaryRow(string WorkDescription, int DurationMinutes);

public sealed record WorkdaySummary(
    DateOnly WorkDate,
    IReadOnlyList<JiraSummaryRow> JiraRows,
    IReadOnlyList<MyWorkSummaryRow> MyWorkRows,
    int JiraTotalMinutes,
    int MyWorkTotalMinutes)
{
    public const int MyWorkAdjustmentMinutes = 48;

    public static WorkdaySummary Create(IEnumerable<WorkEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var orderedEntries = entries.OrderBy(entry => entry.CreationOrder).ToArray();

        if (orderedEntries.Length == 0)
        {
            throw new ArgumentException("A workday summary requires at least one entry.", nameof(entries));
        }

        var workDate = orderedEntries[0].WorkDate;

        if (orderedEntries.Any(entry => entry.WorkDate != workDate))
        {
            throw new ArgumentException("All entries must belong to the same workday.", nameof(entries));
        }

        var totalMinutes = WorkdayTotals.Calculate(orderedEntries).TotalMinutes;

        if (totalMinutes != WorkdayTotals.TargetMinutes)
        {
            throw new ArgumentException("A workday summary requires exactly 432 minutes.", nameof(entries));
        }

        var longestEntry = orderedEntries
            .OrderByDescending(entry => entry.DurationMinutes)
            .ThenBy(entry => entry.CreationOrder)
            .First();

        var jiraRows = orderedEntries
            .Select(entry => new JiraSummaryRow(entry.JiraId, entry.Title, entry.Description, entry.DurationMinutes))
            .ToArray();
        var myWorkRows = orderedEntries
            .Select(entry => new MyWorkSummaryRow(
                string.IsNullOrEmpty(entry.Description)
                    ? $"{entry.JiraId} {entry.Title}"
                    : $"{entry.JiraId} {entry.Title} - {entry.Description}",
                entry.DurationMinutes + (entry.Id == longestEntry.Id ? MyWorkAdjustmentMinutes : 0)))
            .ToArray();

        return new WorkdaySummary(workDate, jiraRows, myWorkRows, (int)totalMinutes,
            (int)(totalMinutes + MyWorkAdjustmentMinutes));
    }
}
