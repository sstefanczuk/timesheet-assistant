using Microsoft.Data.Sqlite;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace TimesheetAssistant.UiTests.Features.Workdays;

[Collection("Desktop")]
public sealed class WorkdaySummaryTests
{
    [Fact]
    public async Task Us01_shows_exact_rows_totals_tabs_copy_values_and_accessible_copy_results()
    {
        var scenario = new DesktopScenario();
        await scenario.RunAsync(async () =>
        {
            var launch = await scenario.LaunchAsync();
            var workday = new WorkdayScreen(launch);
            await workday.ReadyAsync();
            await workday.DateAsync(DateText(DateOnly.FromDateTime(DateTime.Now)));
            await workday.AddAsync("T-1", "Scrum Ceremonies", "Refinement", "1h");
            await workday.TotalsAsync("1h", "Remaining: 6h 12m");
            await workday.AddAsync("X-1", "Order Page", "", "6h 12m");
            await workday.TotalsAsync("7h 12m", "Remaining: 0m");
            await workday.SummaryButtonAsync(enabled: true);
            await workday.OpenSummaryAsync();

            var summary = new WorkdaySummaryScreen(launch);
            await summary.JiraAsync();
            var jiraHeaders = summary.Dialog.Locator("#jira-panel thead th");
            await Expect(jiraHeaders).ToHaveCountAsync(5);
            await Expect(jiraHeaders.Nth(0)).ToHaveTextAsync("Jira ID");
            await Expect(jiraHeaders.Nth(1)).ToHaveTextAsync("Item title");
            await Expect(jiraHeaders.Nth(2)).ToHaveTextAsync("Work description");
            await Expect(jiraHeaders.Nth(3)).ToHaveTextAsync("Time");
            await Expect(jiraHeaders.Nth(4)).ToHaveTextAsync("Copy");
            await Expect(summary.JiraRows).ToHaveCountAsync(2);
            await Expect(summary.JiraRows.Nth(0).Locator("td").Nth(0)).ToHaveTextAsync("T-1");
            await Expect(summary.JiraRows.Nth(0).Locator("td").Nth(1)).ToHaveTextAsync("Scrum Ceremonies");
            await Expect(summary.JiraRows.Nth(0).Locator("td").Nth(2)).ToHaveTextAsync("Refinement");
            await Expect(summary.JiraRows.Nth(0).Locator("td").Nth(3)).ToHaveTextAsync("1h");
            await Expect(summary.JiraRows.Nth(1).Locator("td").Nth(0)).ToHaveTextAsync("X-1");
            await Expect(summary.JiraRows.Nth(1).Locator("td").Nth(1)).ToHaveTextAsync("Order Page");
            await Expect(summary.JiraRows.Nth(1).Locator("td").Nth(2)).ToHaveTextAsync("");
            await Expect(summary.JiraRows.Nth(1).Locator("td").Nth(3)).ToHaveTextAsync("6h 12m");
            await Expect(summary.Dialog.Locator("#jira-panel tfoot")).ToContainTextAsync("7h 12m");
            await Expect(summary.JiraRows.Nth(1).GetByRole(AriaRole.Button, new() { Name = "Copy description", Exact = true })).ToHaveCountAsync(0);

            await InstallClipboardCaptureAsync(launch.Page);
            await summary.JiraRows.Nth(0).GetByRole(AriaRole.Button, new() { Name = "Copy ID", Exact = true }).ClickAsync();
            await Expect(summary.CopyStatus).ToHaveTextAsync("Copied");
            Assert.Equal("T-1", await ReadClipboardCaptureAsync(launch.Page));
            await summary.JiraRows.Nth(0).GetByRole(AriaRole.Button, new() { Name = "Copy description", Exact = true }).ClickAsync();
            await Expect(summary.CopyStatus).ToHaveTextAsync("Copied");
            Assert.Equal("Refinement", await ReadClipboardCaptureAsync(launch.Page));

            await summary.MyWorkAsync();
            var myWorkHeaders = summary.Dialog.Locator("#mywork-panel thead th");
            await Expect(myWorkHeaders).ToHaveCountAsync(3);
            await Expect(myWorkHeaders.Nth(0)).ToHaveTextAsync("Work description");
            await Expect(myWorkHeaders.Nth(1)).ToHaveTextAsync("Time");
            await Expect(myWorkHeaders.Nth(2)).ToHaveTextAsync("Copy");
            await Expect(summary.MyWorkRows).ToHaveCountAsync(2);
            await Expect(summary.MyWorkRows.Nth(0).Locator("td").Nth(0)).ToHaveTextAsync("T-1 Scrum Ceremonies - Refinement");
            await Expect(summary.MyWorkRows.Nth(0).Locator("td").Nth(1)).ToHaveTextAsync("1h");
            await Expect(summary.MyWorkRows.Nth(1).Locator("td").Nth(0)).ToHaveTextAsync("X-1 Order Page");
            await Expect(summary.MyWorkRows.Nth(1).Locator("td").Nth(1)).ToHaveTextAsync("7h");
            await Expect(summary.Dialog.Locator("#mywork-panel tfoot")).ToContainTextAsync("8h");
            await summary.MyWorkRows.Nth(0).GetByRole(AriaRole.Button, new() { Name = "Copy description", Exact = true }).ClickAsync();
            await Expect(summary.CopyStatus).ToHaveTextAsync("Copied");
            Assert.Equal("T-1 Scrum Ceremonies - Refinement", await ReadClipboardCaptureAsync(launch.Page));

            await InstallClipboardFailureAsync(launch.Page);
            await summary.MyWorkRows.Nth(0).GetByRole(AriaRole.Button, new() { Name = "Copy description", Exact = true }).ClickAsync();
            Assert.Equal(1, await ReadClipboardFailureCallsAsync(launch.Page));
            await Expect(summary.CopyStatus).ToContainTextAsync("Could not copy the mywork description for row 1");
            await Expect(summary.MyWorkRows.Nth(0).Locator("td").Nth(0)).ToContainTextAsync("T-1 Scrum Ceremonies - Refinement");
            await summary.CloseAsync();
        });
    }

    [Fact]
    public async Task Separate_ticket_entries_keep_their_rows_and_longest_entry_receives_adjustment_with_tie_to_first()
    {
        var scenario = new DesktopScenario();
        await scenario.RunAsync(async () =>
        {
            var launch = await scenario.LaunchAsync();
            var workday = new WorkdayScreen(launch);
            await workday.ReadyAsync();
            var dates = RecentWeekdays(2);

            await workday.DateAsync(DateText(dates[0]));
            await workday.AddAsync("A-1", "First separate entry", "First", "2h");
            await workday.AddAsync("A-1", "Second separate entry", "Second", "2h");
            await workday.AddAsync("B-1", "Longest individual entry", "Third", "3h 12m");
            await workday.OpenSummaryAsync();
            var summary = new WorkdaySummaryScreen(launch);
            await summary.MyWorkAsync();
            await Expect(summary.MyWorkRows).ToHaveCountAsync(3);
            await Expect(summary.MyWorkRows.Nth(0).Locator("td").Nth(0)).ToHaveTextAsync("A-1 First separate entry - First");
            await Expect(summary.MyWorkRows.Nth(0).Locator("td").Nth(1)).ToHaveTextAsync("2h");
            await Expect(summary.MyWorkRows.Nth(1).Locator("td").Nth(0)).ToHaveTextAsync("A-1 Second separate entry - Second");
            await Expect(summary.MyWorkRows.Nth(1).Locator("td").Nth(1)).ToHaveTextAsync("2h");
            await Expect(summary.MyWorkRows.Nth(2).Locator("td").Nth(0)).ToHaveTextAsync("B-1 Longest individual entry - Third");
            await Expect(summary.MyWorkRows.Nth(2).Locator("td").Nth(1)).ToHaveTextAsync("4h");
            await summary.CloseAsync();

            await workday.DateAsync(DateText(dates[1]));
            await workday.AddAsync("TIE-1", "First tied entry", "", "3h 36m");
            await workday.AddAsync("TIE-2", "Second tied entry", "", "3h 36m");
            await workday.OpenSummaryAsync();
            summary = new WorkdaySummaryScreen(launch);
            await summary.MyWorkAsync();
            await Expect(summary.MyWorkRows).ToHaveCountAsync(2);
            await Expect(summary.MyWorkRows.Nth(0).Locator("td").Nth(1)).ToHaveTextAsync("4h 24m");
            await Expect(summary.MyWorkRows.Nth(1).Locator("td").Nth(1)).ToHaveTextAsync("3h 36m");
            await summary.CloseAsync();
        });
    }

    [Fact]
    public async Task Incomplete_and_over_target_days_are_locked_and_summary_reopens_with_edited_values_after_restart()
    {
        var scenario = new DesktopScenario();
        await scenario.RunAsync(async () =>
        {
            var launch = await scenario.LaunchAsync();
            var workday = new WorkdayScreen(launch);
            await workday.ReadyAsync();
            var dates = RecentWeekdays(3);

            await workday.DateAsync(DateText(dates[0]));
            await workday.AddAsync("LOW-1", "Below target", "", "7h 11m");
            await workday.SummaryButtonAsync(enabled: false);
            await workday.SummaryButton.ClickAsync();
            await Expect(launch.Page.Locator("dialog.summary-dialog")).ToHaveCountAsync(0);

            await workday.DateAsync(DateText(dates[1]));
            await workday.AddAsync("HIGH-1", "Above target", "", "7h 13m");
            await workday.SummaryButtonAsync(enabled: false);
            await workday.SummaryButton.ClickAsync();
            await Expect(launch.Page.Locator("dialog.summary-dialog")).ToHaveCountAsync(0);

            await workday.DateAsync(DateText(dates[2]));
            await workday.AddAsync("EDIT-1", "Before edit", "Original description", "1h");
            await workday.AddAsync("EDIT-2", "Unchanged entry", "", "6h 12m");
            await workday.OpenSummaryAsync();
            var summary = new WorkdaySummaryScreen(launch);
            await summary.MyWorkAsync();
            await Expect(summary.MyWorkRows.Nth(0).Locator("td").Nth(0)).ToHaveTextAsync("EDIT-1 Before edit - Original description");
            await summary.CloseAsync();

            await workday.EditAsync(0);
            await workday.Field("Item title").FillAsync("Edited entry");
            await workday.Field("Work description (optional)").FillAsync("Updated description");
            await workday.SaveAsync("Entry updated.");
            await workday.OpenSummaryAsync();
            summary = new WorkdaySummaryScreen(launch);
            await summary.MyWorkAsync();
            await Expect(summary.MyWorkRows.Nth(0).Locator("td").Nth(0)).ToHaveTextAsync("EDIT-1 Edited entry - Updated description");
            await summary.CloseAsync();
            await launch.DisposeAsync();

            launch = await scenario.LaunchAsync();
            workday = new WorkdayScreen(launch);
            await workday.ReadyAsync();
            await workday.DateAsync(DateText(dates[2]));
            await workday.OpenSummaryAsync();
            summary = new WorkdaySummaryScreen(launch);
            await summary.MyWorkAsync();
            await Expect(summary.MyWorkRows.Nth(0).Locator("td").Nth(0)).ToHaveTextAsync("EDIT-1 Edited entry - Updated description");
            await Expect(summary.Dialog.Locator("#mywork-panel tfoot")).ToContainTextAsync("8h");
            await summary.CloseAsync();
        });
    }

    [Fact]
    public async Task Storage_read_failure_does_not_open_stale_summary_and_recovers_after_retry()
    {
        var scenario = new DesktopScenario();
        await scenario.RunAsync(async () =>
        {
            var workday = new WorkdayScreen(await scenario.LaunchAsync());
            await workday.ReadyAsync();
            var date = DateText(DateOnly.FromDateTime(DateTime.Now));
            await workday.DateAsync(date);
            await workday.AddAsync("READ-1", "Current data", "Fresh from SQLite", "7h 12m");
            await workday.Application.DisposeAsync();

            var reopened = await scenario.LaunchAsync();
            workday = new WorkdayScreen(reopened);
            await workday.ReadyAsync();
            await workday.DateAsync(date);
            using (var locked = new FileStream(scenario.DatabasePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                reopened.RecordStage("test-owned SQLite exclusive lock before summary refresh");
                await workday.SummaryButton.ClickAsync();
                await Expect(reopened.Page.GetByRole(AriaRole.Alert)).ToContainTextAsync("Could not refresh this day");
                await Expect(reopened.Page.Locator("dialog.summary-dialog")).ToHaveCountAsync(0);
            }

            await reopened.Page.GetByRole(AriaRole.Button, new() { Name = "Refresh", Exact = true }).ClickAsync();
            await workday.ReadyAsync();
            await workday.OpenSummaryAsync();
            var summary = new WorkdaySummaryScreen(reopened);
            await summary.JiraAsync();
            await Expect(summary.JiraRows.Nth(0)).ToContainTextAsync("Fresh from SQLite");
            await summary.CloseAsync();
        });
    }

    private static async Task InstallClipboardCaptureAsync(IPage page)
    {
        await page.EvaluateAsync("""() => { window.__summaryClipboardFailure = false; window.__summaryClipboardFailureCalls = 0; Object.defineProperty(navigator, 'clipboard', { configurable: true, value: { writeText: async value => { if (window.__summaryClipboardFailure) { window.__summaryClipboardFailureCalls++; throw new DOMException('Synthetic clipboard denial', 'NotAllowedError'); } window.__summaryTestClipboard = value; } } }); }""");
    }

    private static async Task InstallClipboardFailureAsync(IPage page)
    {
        await page.EvaluateAsync("""() => { window.__summaryClipboardFailure = true; }""");
    }

    private static Task<string> ReadClipboardCaptureAsync(IPage page) => page.EvaluateAsync<string>("() => window.__summaryTestClipboard");

    private static Task<int> ReadClipboardFailureCallsAsync(IPage page) => page.EvaluateAsync<int>("() => window.__summaryClipboardFailureCalls");

    private static string DateText(DateOnly date) => date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    private static DateOnly[] RecentWeekdays(int count)
    {
        var dates = new List<DateOnly>();
        for (var date = DateOnly.FromDateTime(DateTime.Now); dates.Count < count; date = date.AddDays(-1))
        {
            if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                dates.Add(date);
            }
        }

        return dates.ToArray();
    }
}
