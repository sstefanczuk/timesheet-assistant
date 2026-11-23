using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace TimesheetAssistant.UiTests.Features.Workdays;

[Collection("Desktop")]
public sealed class WorkdayInputTests
{
    [Fact]
    public async Task Date_selection_is_retained_after_closing_and_used_for_the_saved_entry()
    {
        var scenario = new DesktopScenario();
        await scenario.RunAsync(async () =>
        {
            var launch = await scenario.LaunchAsync();
            var screen = new WorkdayScreen(launch);
            await screen.ReadyAsync();
            var date = DateOnly.FromDateTime(DateTime.Now).AddDays(-1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            await screen.DateAsync(date);
            await screen.OpenAddAsync();
            await Expect(screen.Field("Work date")).ToHaveValueAsync(date);
            await screen.AddAsync("READY-1", "Selected day", "Acknowledged date", "1m");
            await screen.EntriesAsync(["READY-1", "Selected day", "Acknowledged date", "1m"]);
        });
    }
    [Fact]
    public async Task Rapid_entry_input_retains_every_field_before_and_after_save()
    {
        var scenario = new DesktopScenario();
        await scenario.RunAsync(async () =>
        {
            var launch = await scenario.LaunchAsync();
            var screen = new WorkdayScreen(launch);
            await screen.ReadyAsync();
            await screen.DateAsync("2026-01-12");
            var expected = new List<string[]>();

            for (var index = 0; index < 12; index++)
            {
                var jira = $"FAST-{index}";
                var title = $"Title {index}";
                var description = $"Description {index}";
                await screen.FillAsync(jira, title, description, "1m");
                await Expect(screen.Field("Jira ID")).ToHaveValueAsync(jira);
                await Expect(screen.Field("Item title")).ToHaveValueAsync(title);
                await Expect(screen.Field("Work description (optional)")).ToHaveValueAsync(description);
                await Expect(screen.Field("Duration")).ToHaveValueAsync("1m");
                await screen.SaveAsync();
                expected.Add([jira, title, description, "1m"]);
                await screen.EntriesAsync(expected.ToArray());
            }

            await launch.DisposeAsync();
            var restarted = await scenario.LaunchAsync();
            screen = new WorkdayScreen(restarted);
            await screen.ReadyAsync();
            await screen.DateAsync("2026-01-12");
            await screen.EntriesAsync(expected.ToArray());
        });
    }
}
