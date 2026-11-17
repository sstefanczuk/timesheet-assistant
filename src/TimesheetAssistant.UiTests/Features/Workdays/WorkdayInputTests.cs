using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace TimesheetAssistant.UiTests.Features.Workdays;

[Collection("Desktop")]
public sealed class WorkdayInputTests
{
    [Fact]
    public async Task Date_selection_waits_until_the_application_has_loaded_the_requested_day()
    {
        var scenario = new DesktopScenario();
        await scenario.RunAsync(async () =>
        {
            var launch = await scenario.LaunchAsync();
            var screen = new WorkdayScreen(launch);
            await screen.ReadyAsync();
            var originalDate = await screen.Field("Work date").InputValueAsync();
            var date = DateOnly.ParseExact(originalDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture).AddDays(-1)
                .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

            // Hold the browser change event so its visible value precedes application state.
            await launch.Page.EvaluateAsync("""
                () => {
                    let hold = true;
                    const date = document.getElementById('work-date');
                    const block = event => {
                        if (hold && event.target === date) {
                            event.stopImmediatePropagation();
                            window.workDateChangeHeld = true;
                        }
                    };
                    // Run before Blazor's document-level event delegation.
                    window.addEventListener('change', block, true);
                    window.releaseWorkDateChange = () => {
                        hold = false;
                        window.removeEventListener('change', block, true);
                        date.dispatchEvent(new Event('change', {bubbles: true}));
                    };
                }
                """);

            var selection = screen.DateAsync(date);

            try
            {
                await launch.Page.WaitForFunctionAsync("() => window.workDateChangeHeld === true");
                await Expect(screen.Field("Work date")).ToHaveValueAsync(date);
                await Expect(screen.Button("Save")).ToBeEnabledAsync();
                await Expect(screen.DailyTotals)
                    .ToHaveAttributeAsync("data-work-date", originalDate);

                // A bounded negative completion assertion, not a delay/retry of UI actions.
                await Assert.ThrowsAsync<TimeoutException>(() => selection.WaitAsync(TimeSpan.FromMilliseconds(300)));
            }
            finally
            {
                await launch.Page.EvaluateAsync("() => window.releaseWorkDateChange()");
                await selection.WaitAsync(TimeSpan.FromSeconds(15));
            }

            await screen.AddAsync("READY-1", "Selected day", "Acknowledged date", "1m");
            await screen.EntriesAsync(["READY-1", "Selected day", "Acknowledged date", "1m"]);
            await screen.DateAsync(originalDate);
            await screen.EntriesAsync();
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
