using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace TimesheetAssistant.UiTests.Features.Workdays;

[Collection("Desktop")]
public sealed class WorkdayRecordingTests
{
    private const string WorkDate = "2026-01-12";
    private const string Weekend = "2026-01-10";

    [Fact]
    public async Task Saved_entries_preserve_exact_contents_identity_order_and_mutations_across_three_launches()
    {
        var scenario = new DesktopScenario();
        await scenario.RunAsync(async () =>
        {
            var first = await scenario.LaunchAsync();
            var screen = new WorkdayScreen(first);
            await screen.ReadyAsync();
            await screen.DateAsync(WorkDate);

            await screen.AddAsync("T-1", "Scrum Ceremonies", "Refinement", "1h");
            await screen.TotalsAsync("1h", "Remaining: 6h 12m");
            await Expect(screen.DailyTotals.Locator(".status-warning")).ToHaveCountAsync(1);
            await screen.AddAsync("X-1", "Order Page", "", "6h 12m");
            await screen.TotalsAsync("7h 12m", "Remaining: 0m");
            await Expect(screen.DailyTotals.Locator(".status-success")).ToHaveCountAsync(1);
            await screen.AddAsync("T-1", "Separate entry", "", "1m");
            await screen.TotalsAsync("7h 13m", "Excess: 1m");
            await Expect(screen.DailyTotals.Locator(".status-warning")).ToHaveCountAsync(1);
            await screen.EditAsync(2);
            await screen.Field("Duration").FillAsync("2m");
            await screen.SaveAsync("Entry updated.");
            await screen.TotalsAsync("7h 14m", "Excess: 2m");
            await screen.DeleteAsync(2, confirm: false);
            await screen.EntriesAsync(["T-1", "Scrum Ceremonies", "Refinement", "1h"],
                ["X-1", "Order Page", "", "6h 12m"], ["T-1", "Separate entry", "", "2m"]);
            await screen.DeleteAsync(2, confirm: true);
            await screen.TotalsAsync("7h 12m", "Remaining: 0m");

            await screen.AddAsync("KEEP-1", "Retained edit", "before", "1m");
            await screen.EditAsync(2);
            await screen.Field("Work description (optional)").FillAsync("after");
            await screen.Field("Duration").FillAsync("3m");
            await screen.SaveAsync("Entry updated.");
            var identities = await screen.Rows.EvaluateAllAsync<string[]>("rows => rows.map(row => row.dataset.entryId)");
            await screen.DateAsync(Weekend);
            await screen.AddAsync("W-1", "Weekend work", "Saturday", "30m");
            await screen.TotalsAsync("30m", "Remaining: 6h 42m");
            await first.DisposeAsync();

            var second = await scenario.LaunchAsync();
            Assert.NotEqual(first.ProfileDirectory, second.ProfileDirectory);
            Assert.False(Directory.Exists(first.ProfileDirectory));
            screen = new WorkdayScreen(second);
            await screen.ReadyAsync();
            await screen.DateAsync(WorkDate);
            await screen.EntriesAsync(["T-1", "Scrum Ceremonies", "Refinement", "1h"],
                ["X-1", "Order Page", "", "6h 12m"], ["KEEP-1", "Retained edit", "after", "3m"]);
            Assert.Equal(identities, await screen.Rows.EvaluateAllAsync<string[]>("rows => rows.map(row => row.dataset.entryId)"));
            await screen.TotalsAsync("7h 15m", "Excess: 3m");
            await screen.DateAsync(Weekend);
            await screen.EntriesAsync(["W-1", "Weekend work", "Saturday", "30m"]);
            await screen.TotalsAsync("30m", "Remaining: 6h 42m");
            await screen.DateAsync(WorkDate);
            await screen.EditAsync(0);
            await screen.Field("Duration").FillAsync("2h");
            await screen.SaveAsync("Entry updated.");
            await screen.DeleteAsync(1, confirm: true);
            await second.DisposeAsync();

            var third = await scenario.LaunchAsync();
            Assert.NotEqual(second.ProfileDirectory, third.ProfileDirectory);
            screen = new WorkdayScreen(third);
            await screen.ReadyAsync();
            await screen.DateAsync(WorkDate);
            await screen.EntriesAsync(["T-1", "Scrum Ceremonies", "Refinement", "2h"],
                ["KEEP-1", "Retained edit", "after", "3m"]);
            await screen.TotalsAsync("2h 3m", "Remaining: 5h 9m");
        });
    }

    [Fact]
    public async Task Validation_and_closing_add_dialog_preserve_draft_and_saved_entries()
    {
        var scenario = new DesktopScenario();
        await scenario.RunAsync(async () =>
        {
            var launch = await scenario.LaunchAsync();
            var screen = new WorkdayScreen(launch);
            await screen.ReadyAsync();
            await screen.DateAsync(WorkDate);
            await screen.AddAsync("T-1", "Saved entry", "", "1h");
            await screen.FillAsync("", "", "Draft", "6h 60m");
            await screen.Button("Save").ClickAsync();
            await Expect(screen.Field("Jira ID")).ToHaveAttributeAsync("aria-invalid", "true");
            await Expect(screen.Field("Item title")).ToHaveAttributeAsync("aria-invalid", "true");
            await Expect(screen.Field("Duration")).ToHaveAttributeAsync("aria-invalid", "true");
            await Expect(screen.Field("Duration")).ToHaveValueAsync("6h 60m");
            await screen.Button("Cancel").ClickAsync();
            await screen.EntriesAsync(["T-1", "Saved entry", "", "1h"]);
            await screen.OpenAddAsync();
            await Expect(screen.Field("Work date")).ToHaveValueAsync(WorkDate);
            await Expect(screen.Field("Work description (optional)")).ToHaveValueAsync("Draft");
            await Expect(screen.Field("Duration")).ToHaveValueAsync("6h 60m");
            await screen.Button("Cancel").ClickAsync();
        });
    }
}
