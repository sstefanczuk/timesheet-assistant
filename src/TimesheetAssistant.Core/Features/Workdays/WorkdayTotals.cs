namespace TimesheetAssistant.Core.Features.Workdays;

public sealed record WorkdayTotals(long TotalMinutes)
{
    public const int TargetMinutes = 432;
    public long RemainingMinutes => Math.Max(0, TargetMinutes - TotalMinutes);
    public long ExcessMinutes => Math.Max(0, TotalMinutes - TargetMinutes);

    public static WorkdayTotals Calculate(IEnumerable<WorkEntry> entries)
    {
        long total = 0;
        foreach (var entry in entries) total = checked(total + entry.DurationMinutes);
        return new(total);
    }
}
