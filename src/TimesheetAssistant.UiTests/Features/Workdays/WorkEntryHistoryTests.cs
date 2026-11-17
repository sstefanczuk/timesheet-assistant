using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace TimesheetAssistant.UiTests.Features.Workdays;

[Collection("Desktop")]
public sealed class WorkEntryHistoryTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Empty_and_short_history_keep_today_addition_usable(int earlierCount)
    {
        var scenario = new DesktopScenario();
        await scenario.RunAsync(async () =>
        {
            var launch = await scenario.LaunchAsync();
            var editor = new WorkdayScreen(launch);
            var history = new WorkEntryHistoryScreen(launch);
            await editor.ReadyAsync();
            var today = await history.LocalTodayAsync();
            var dates = HistoryTestData.SparseDates(today).Take(earlierCount).ToArray();

            foreach (var date in dates)
            {
                await editor.DateAsync(WorkEntryHistoryScreen.DateText(date));
                await editor.AddAsync("SHORT-1", "Earlier work", "Saved through UI", "1m");
            }

            if (earlierCount > 0)
            {
                await launch.DisposeAsync();
                launch = await scenario.LaunchAsync();
                editor = new WorkdayScreen(launch);
                history = new WorkEntryHistoryScreen(launch);
                await editor.ReadyAsync();
            }

            await history.DatesAsync(dates);
            await history.ExhaustedAsync();

            if (earlierCount == 0)
            {
                await Expect(history.Earlier.GetByText("No earlier entries yet.", new() { Exact = true })).ToBeVisibleAsync();
            }

            await Expect(history.Today).ToContainTextAsync("No entries for today.");
            await history.Today.GetByRole(AriaRole.Button, new() { Name = "Add entry for today", Exact = true }).ClickAsync();
            await Expect(editor.DailyTotals).ToHaveAttributeAsync("data-work-date", WorkEntryHistoryScreen.DateText(today));
            await Expect(editor.Field("Jira ID")).ToBeFocusedAsync();
            await Expect(editor.Field("Item title")).ToHaveValueAsync("");
            await editor.AddAsync("TODAY-1", "New work from scratch", "Today stays separate", "3m");
            await Expect(history.Today).ToContainTextAsync("Total: 3m");
            await history.DatesAsync(dates);
        });
    }

    [Fact]
    public async Task Sparse_history_preserves_exact_entries_paging_mutations_discard_and_restart()
    {
        var scenario = new DesktopScenario();
        await scenario.RunAsync(async () =>
        {
            var seed = await scenario.LaunchAsync();
            var editor = new WorkdayScreen(seed);
            await editor.ReadyAsync();
            var today = await new WorkEntryHistoryScreen(seed).LocalTodayAsync();
            await editor.AddAsync("TODAY-1", "Today only", "Excluded from earlier groups", "5m");
            var days = await HistoryTestData.SeedAsync(editor, today);
            await seed.DisposeAsync();

            var launch = await scenario.LaunchAsync();
            editor = new WorkdayScreen(launch);
            var history = new WorkEntryHistoryScreen(launch);
            await editor.ReadyAsync();
            Assert.NotEqual(seed.ProfileDirectory, launch.ProfileDirectory);
            await history.DatesAsync(days.Take(3).Select(day => day.Date).ToArray());
            await Expect(history.Today).ToContainTextAsync("Total: 5m");
            await Expect(history.Groups.Filter(new() { HasText = "Today only" })).ToHaveCountAsync(0);

            foreach (var day in days.Take(3))
            {
                await history.DayAsync(day);
            }

            await history.OlderAsync(days.Take(4).Select(day => day.Date).ToArray());
            await history.DayAsync(days[3]);
            await history.OlderAsync(days.Take(5).Select(day => day.Date).ToArray());
            await history.DayAsync(days[4]);
            var retainedBoundary = WorkEntryHistoryScreen.DateText(days[4].Date);

            // A context request must preserve this unrelated draft and loaded range when cancelled.
            await editor.FillAsync("DRAFT-1", "Do not discard", "Kept description", "7m");
            var originalDate = await editor.Field("Work date").InputValueAsync();
            await history.EditAsync(days[0], 1);
            await Expect(editor.Dialog).ToBeVisibleAsync();
            await editor.Dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
            await Expect(editor.Field("Work date")).ToHaveValueAsync(originalDate);
            await Expect(editor.Field("Jira ID")).ToHaveValueAsync("DRAFT-1");
            await Expect(editor.Field("Item title")).ToHaveValueAsync("Do not discard");
            await Expect(editor.Field("Work description (optional)")).ToHaveValueAsync("Kept description");
            await Expect(editor.Field("Duration")).ToHaveValueAsync("7m");
            await history.DatesAsync(days.Take(5).Select(day => day.Date).ToArray());
            await Expect(history.Earlier).ToHaveAttributeAsync("data-oldest-boundary", retainedBoundary);

            await history.EditAsync(days[0], 1);
            await editor.Dialog.GetByRole(AriaRole.Button, new() { Name = "Discard changes", Exact = true }).ClickAsync();
            await Expect(editor.Dialog).ToHaveCountAsync(0);
            await history.EditorAcknowledgedAsync(days[0], 1);
            await editor.Field("Item title").FillAsync("Committed historical correction");
            await editor.Field("Duration").FillAsync("45m");
            await editor.SaveAsync("Entry updated.");
            var changedRows = days[0].Entries.Select(row => row.ToArray()).ToArray();
            changedRows[1][1] = "Committed historical correction";
            changedRows[1][3] = "45m";
            var changed = days[0] with { Entries = changedRows, Total = "1h 45m" };
            await history.DayAsync(changed);
            await history.DatesAsync(days.Take(5).Select(day => day.Date).ToArray());

            await history.EditAsync(days[4], 0);
            await history.EditorAcknowledgedAsync(days[4], 0);
            await editor.DeleteAsync(0, confirm: true);
            var retainedDates = days.Take(4).Select(day => day.Date).ToArray();
            await history.DatesAsync(retainedDates);
            await Expect(history.Earlier).ToHaveAttributeAsync("data-oldest-boundary", retainedBoundary);
            await history.OlderAsync(retainedDates.Append(days[5].Date).ToArray());
            await history.DayAsync(days[5]);
            await history.ExhaustedAsync();
            await launch.DisposeAsync();

            var restarted = await scenario.LaunchAsync();
            editor = new WorkdayScreen(restarted);
            history = new WorkEntryHistoryScreen(restarted);
            await editor.ReadyAsync();
            await history.DatesAsync(days.Take(3).Select(day => day.Date).ToArray());
            await history.DayAsync(changed);
            await history.OlderAsync(retainedDates);
            await history.OlderAsync(retainedDates.Append(days[5].Date).ToArray());
            await history.DayAsync(days[5]);
            await Expect(history.Day(days[4].Date)).ToHaveCountAsync(0);
            await history.ExhaustedAsync();
        });
    }

    [Fact]
    public async Task Failed_older_read_keeps_complete_groups_and_cursor_until_retry_after_lock_release()
    {
        var scenario = new DesktopScenario();
        await scenario.RunAsync(async () =>
        {
            var seed = await scenario.LaunchAsync();
            var editor = new WorkdayScreen(seed);
            await editor.ReadyAsync();
            var today = await new WorkEntryHistoryScreen(seed).LocalTodayAsync();
            var days = await HistoryTestData.SeedAsync(editor, today);
            await seed.DisposeAsync();
            var launch = await scenario.LaunchAsync();
            await new WorkdayScreen(launch).ReadyAsync();
            var history = new WorkEntryHistoryScreen(launch);
            var firstFour = days.Take(4).Select(day => day.Date).ToArray();
            await history.OlderAsync(firstFour);
            var boundary = await history.Earlier.GetAttributeAsync("data-oldest-boundary");

            // FileShare.None prevents reads, unlike an ordinary SQLite reserved writer transaction.
            var blocked = new FileStream(scenario.DatabasePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            try
            {
                launch.RecordStage("test-owned database read access blocked after successful paging");
                await history.Button("Load older day").ClickAsync();
                await Expect(history.Earlier.GetByRole(AriaRole.Alert)).ToContainTextAsync("have not been refreshed",
                    new() { Timeout = launch.UiTimeoutMilliseconds });
                await history.DatesAsync(firstFour);
                await Expect(history.Earlier).ToHaveAttributeAsync("data-oldest-boundary", boundary!);
                await Expect(history.Button("Retry history")).ToBeVisibleAsync();

                foreach (var day in days.Take(4))
                {
                    await history.DayAsync(day);
                }
            }
            finally
            {
                blocked.Dispose();
                launch.RecordStage("test-owned database read access restored");
            }

            await history.Button("Retry history").ClickAsync();
            await history.DatesAsync(days.Take(5).Select(day => day.Date).ToArray());
            await history.DayAsync(days[4]);
            await Expect(history.Earlier.GetByRole(AriaRole.Alert)).ToHaveCountAsync(0);
            await Expect(history.Button("Retry history")).ToHaveCountAsync(0);
            await Expect(history.Earlier).ToHaveAttributeAsync("data-oldest-boundary", WorkEntryHistoryScreen.DateText(days[4].Date));
            await history.OlderAsync(days.Select(day => day.Date).ToArray());
            await history.ExhaustedAsync();
        });
    }
}
