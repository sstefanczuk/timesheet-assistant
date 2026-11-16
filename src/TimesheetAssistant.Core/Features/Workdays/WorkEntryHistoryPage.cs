namespace TimesheetAssistant.Core.Features.Workdays;

public sealed class WorkEntryHistoryPage
{
    public WorkEntryHistoryPage(IEnumerable<WorkdaySnapshot> days, bool hasOlder)
    {
        Days = Array.AsReadOnly(days.Select(day => new WorkdaySnapshot(day.Date,
            Array.AsReadOnly(day.Entries.ToArray()))).ToArray());
        HasOlder = hasOlder;
    }

    public IReadOnlyList<WorkdaySnapshot> Days { get; }

    public bool HasOlder { get; }
}

public sealed record WorkEntryHistorySnapshot(DateOnly LocalToday, WorkdaySnapshot Today,
    WorkEntryHistoryPage History, DateOnly? OldestLoadedBoundary)
{
    public IReadOnlyList<WorkdaySnapshot> Days => History.Days;

    public bool HasOlder => History.HasOlder;
}
