namespace TimesheetAssistant.Core.Features.Workdays;

public sealed record WorkEntryValidation(WorkEntryValues? Values, IReadOnlyDictionary<string, string> Errors)
{
    public bool IsValid => Values is not null;
}

public static class WorkEntryValidator
{
    public static WorkEntryValidation Validate(DateOnly date, DateOnly today, WorkEntryInput input)
    {
        var errors = new Dictionary<string, string>();
        var jiraId = input.JiraId?.Trim() ?? string.Empty;
        var title = input.Title?.Trim() ?? string.Empty;

        if (date > today)
        {
            errors["WorkDate"] = "Work date cannot be in the future.";
        }

        if (jiraId.Length == 0)
        {
            errors["JiraId"] = "Enter a Jira ID.";
        }

        if (title.Length == 0)
        {
            errors["Title"] = "Enter an item title.";
        }

        if (!DurationParser.TryParse(input.Duration, out var minutes))
        {
            errors["Duration"] = "Enter 1–1440 minutes using hours and minutes, for example 6h 12m.";
        }

        var values = errors.Count == 0
            ? new WorkEntryValues(jiraId, title, input.Description?.Trim() ?? string.Empty, minutes)
            : null;

        return new WorkEntryValidation(values, errors.AsReadOnly());
    }
}
