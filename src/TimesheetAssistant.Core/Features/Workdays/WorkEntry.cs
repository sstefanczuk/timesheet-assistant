namespace TimesheetAssistant.Core.Features.Workdays;

public sealed record WorkEntry(Guid Id, DateOnly WorkDate, string JiraId, string Title,
    string Description, int DurationMinutes, long CreationOrder);

public sealed record WorkEntryInput(string? JiraId, string? Title, string? Description, string? Duration);

public sealed record WorkEntryValues(string JiraId, string Title, string Description, int DurationMinutes);
