using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace TimesheetAssistant.UiTests.Features.Workdays;

[Collection("Desktop")]
public sealed class WorkEntryDuplicationTests
{
    [Fact]
    public async Task Duplicate_works_for_initial_and_loaded_older_entries_and_persists_an_independent_same_date_copy()
    {
        var scenario = new DesktopScenario();
        await scenario.RunAsync(async () =>
        {
            var seed = await scenario.LaunchAsync();
            var seedScreen = new WorkdayScreen(seed);
            await seedScreen.ReadyAsync();
            var today = await new WorkEntryHistoryScreen(seed).LocalTodayAsync();
            var days = await HistoryTestData.SeedAsync(seedScreen, today);
            await seed.DisposeAsync();

            var launch = await scenario.LaunchAsync();
            var history = new WorkEntryHistoryScreen(launch);
            var screen = new WorkdayScreen(launch);
            await screen.ReadyAsync();
            await history.DatesAsync(days.Take(3).Select(day => day.Date).ToArray());

            var visibleSource = days[0];
            await DuplicateAsync(history, screen, visibleSource, NextWeekday(visibleSource.Date.AddDays(1)));
            await screen.Button("Cancel").ClickAsync();
            await Expect(history.Row(visibleSource.Date, visibleSource.Identities[0])).ToHaveCountAsync(1);
            await Expect(history.Day(visibleSource.Date).Locator("tr[data-entry-id]")).ToHaveCountAsync(visibleSource.Entries.Length);

            await history.OlderAsync(days.Take(4).Select(day => day.Date).ToArray());
            await history.OlderAsync(days.Take(5).Select(day => day.Date).ToArray());
            await history.OlderAsync(days.Select(day => day.Date).ToArray());

            var olderSource = days[5];
            var suggested = NextWeekday(olderSource.Date.AddDays(1));
            await DuplicateAsync(history, screen, olderSource, suggested);
            await Expect(screen.Field("Work date")).ToBeEnabledAsync();
            await Expect(screen.Field("Jira ID")).ToBeEnabledAsync();
            await Expect(screen.Field("Work date")).ToBeFocusedAsync();
            await screen.Field("Work date").FillAsync(WorkEntryHistoryScreen.DateText(olderSource.Date));
            await screen.Field("Work date").PressAsync("Tab");
            await screen.Field("Item title").FillAsync("Independent duplicate");
            await screen.Field("Duration").FillAsync("3m");
            await screen.SaveAsync("Entry duplicated.");

            var sourceRows = history.Day(olderSource.Date).Locator("tr[data-entry-id]");
            await Expect(sourceRows).ToHaveCountAsync(2);
            await Expect(sourceRows.Nth(0)).ToContainTextAsync("Saved day 5");
            await Expect(sourceRows.Nth(0)).ToContainTextAsync("2m");
            await Expect(sourceRows.Nth(1)).ToContainTextAsync("Independent duplicate");
            await Expect(sourceRows.Nth(1)).ToContainTextAsync("3m");
            var identities = await sourceRows.EvaluateAllAsync<string[]>("rows => rows.map(row => row.dataset.entryId)");
            Assert.Equal(2, identities.Distinct().Count());

            await history.Row(olderSource.Date, identities[0])
                .GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
            await screen.Field("Item title").FillAsync("Edited original");
            await screen.SaveAsync("Entry updated.");
            await Expect(sourceRows.Nth(0)).ToContainTextAsync("Edited original");
            await Expect(sourceRows.Nth(1)).ToContainTextAsync("Independent duplicate");
            await Expect(sourceRows.Nth(1)).ToContainTextAsync("3m");

            await launch.DisposeAsync();

            var restarted = await scenario.LaunchAsync();
            screen = new WorkdayScreen(restarted);
            history = new WorkEntryHistoryScreen(restarted);
            await screen.ReadyAsync();
            var visibleDaysAfterRestart = days.Take(3).Select(day => day.Date).ToArray();
            await history.DatesAsync(visibleDaysAfterRestart);
            for (var index = 3; index < days.Length; index++)
            {
                await history.OlderAsync(days.Take(index + 1).Select(day => day.Date).ToArray());
            }

            sourceRows = history.Day(olderSource.Date).Locator("tr[data-entry-id]");
            await Expect(sourceRows).ToHaveCountAsync(2);
            Assert.Equal(identities, await sourceRows.EvaluateAllAsync<string[]>("rows => rows.map(row => row.dataset.entryId)"));
            await Expect(sourceRows.Nth(0)).ToContainTextAsync("Edited original");
            await Expect(sourceRows.Nth(1)).ToContainTextAsync("Independent duplicate");
        });
    }

    [Fact]
    public async Task Suggestion_skips_weekends_and_days_at_or_above_the_daily_target()
    {
        var scenario = new DesktopScenario();
        await scenario.RunAsync(async () =>
        {
            var launch = await scenario.LaunchAsync();
            var screen = new WorkdayScreen(launch);
            await screen.ReadyAsync();
            var today = await new WorkEntryHistoryScreen(launch).LocalTodayAsync();
            var sourceDate = today.AddDays(-14);
            while (sourceDate.DayOfWeek != DayOfWeek.Friday)
            {
                sourceDate = sourceDate.AddDays(-1);
            }

            await AddAsync(screen, sourceDate, "DUP-THRESHOLD", "Source entry", "Source description", "1h");
            await AddAsync(screen, sourceDate.AddDays(1), "WEEKEND-1", "Saturday work", "Weekend", "1h");
            await AddAsync(screen, sourceDate.AddDays(2), "WEEKEND-2", "Sunday work", "Weekend", "1h");
            await AddAsync(screen, sourceDate.AddDays(3), "EXACT-1", "Complete Monday", "Exact target", "7h 12m");
            await AddAsync(screen, sourceDate.AddDays(4), "EXCESS-1", "Excess Tuesday", "Over target", "7h 13m");

            var history = new WorkEntryHistoryScreen(launch);
            await history.EnsureDateLoadedAsync(sourceDate);
            var sourceId = await history.FirstEntryIdAsync(sourceDate);
            await history.DuplicateButton(sourceDate, sourceId).ClickAsync();
            var expected = sourceDate.AddDays(5);
            await Expect(screen.Field("Work date")).ToHaveValueAsync(WorkEntryHistoryScreen.DateText(expected));
            await Expect(screen.Field("Jira ID")).ToHaveValueAsync("DUP-THRESHOLD");
            await Expect(screen.Field("Item title")).ToHaveValueAsync("Source entry");
            await Expect(screen.Field("Work description (optional)")).ToHaveValueAsync("Source description");
            await Expect(screen.Field("Duration")).ToHaveValueAsync("1h");
            await screen.Button("Cancel").ClickAsync();
            await Expect(launch.Page.GetByRole(AriaRole.Region, new() { Name = "Entries", Exact = true })
                .GetByRole(AriaRole.Alert)).ToHaveCountAsync(0);
        });
    }

    [Fact]
    public async Task Failed_suggestion_read_shows_error_without_opening_or_saving_a_duplicate()
    {
        var scenario = new DesktopScenario();
        await scenario.RunAsync(async () =>
        {
            var seed = await scenario.LaunchAsync();
            var seedScreen = new WorkdayScreen(seed);
            await seedScreen.ReadyAsync();
            var today = await new WorkEntryHistoryScreen(seed).LocalTodayAsync();
            var sourceDate = today.AddDays(-3);
            while (sourceDate > today || sourceDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                sourceDate = sourceDate.AddDays(-1);
            }

            await AddAsync(seedScreen, sourceDate, "LOCK-DUP", "Saved source", "Original", "1h");
            await seed.DisposeAsync();

            var launch = await scenario.LaunchAsync();
            var screen = new WorkdayScreen(launch);
            await screen.ReadyAsync();
            var history = new WorkEntryHistoryScreen(launch);
            await history.EnsureDateLoadedAsync(sourceDate);
            var sourceId = await history.FirstEntryIdAsync(sourceDate);

            using (var locked = new FileStream(scenario.DatabasePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                await history.DuplicateButton(sourceDate, sourceId).ClickAsync();
                await Expect(history.Earlier.GetByRole(AriaRole.Alert))
                    .ToContainTextAsync("Could not prepare a duplicate", new() { Timeout = launch.UiTimeoutMilliseconds });
                await Expect(screen.Editor).ToHaveCountAsync(0);
            }

            await launch.Page.GetByRole(AriaRole.Button, new() { Name = "Refresh", Exact = true }).ClickAsync();
            await history.IdleAsync();
            await Expect(history.Day(sourceDate).Locator("tr[data-entry-id]")).ToHaveCountAsync(1);
        });
    }

    private static async Task DuplicateAsync(WorkEntryHistoryScreen history, WorkdayScreen screen,
        HistoryTestDay source, DateOnly suggestedDate)
    {
        await history.DuplicateButton(source.Date, source.Identities[0]).ClickAsync();
        await Expect(screen.Editor.GetByRole(AriaRole.Heading, new() { Name = "Add entry", Exact = true })).ToBeVisibleAsync();
        await Expect(screen.Field("Work date")).ToHaveValueAsync(WorkEntryHistoryScreen.DateText(suggestedDate));
        await Expect(screen.Field("Jira ID")).ToHaveValueAsync(source.Entries[0][0]);
        await Expect(screen.Field("Item title")).ToHaveValueAsync(source.Entries[0][1]);
        await Expect(screen.Field("Work description (optional)")).ToHaveValueAsync(source.Entries[0][2]);
        await Expect(screen.Field("Duration")).ToHaveValueAsync(source.Entries[0][3]);
        await Expect(screen.Field("Work date")).ToBeFocusedAsync();
    }

    private static async Task AddAsync(WorkdayScreen screen, DateOnly date, string jiraId, string title,
        string description, string duration)
    {
        await screen.DateAsync(WorkEntryHistoryScreen.DateText(date));
        await screen.AddAsync(jiraId, title, description, duration);
    }

    private static DateOnly NextWeekday(DateOnly date)
    {
        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            date = date.AddDays(1);
        }

        return date;
    }
}
