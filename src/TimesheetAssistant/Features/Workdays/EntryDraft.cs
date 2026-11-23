namespace TimesheetAssistant.Features.Workdays;

public sealed class EntryDraft
{
    public string Date { get; set; } = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    public string JiraId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;

    public void Clear()
    {
        JiraId = string.Empty;
        Title = string.Empty;
        Description = string.Empty;
        Duration = string.Empty;
    }
}
