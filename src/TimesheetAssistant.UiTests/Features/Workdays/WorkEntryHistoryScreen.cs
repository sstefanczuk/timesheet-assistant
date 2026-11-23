using System.Globalization;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace TimesheetAssistant.UiTests.Features.Workdays;

internal sealed class WorkEntryHistoryScreen(DesktopAppFixture application)
{
    public ILocator Earlier => application.Page.GetByRole(AriaRole.Region, new() { Name = "Entries", Exact = true });

    public ILocator Today => Earlier.Locator($"tbody[data-work-date=\u0027{DateText(DateOnly.FromDateTime(DateTime.Now))}\u0027]");

    public ILocator Groups => Earlier.Locator($"tbody[data-work-date]:not([data-work-date=\u0027{DateText(DateOnly.FromDateTime(DateTime.Now))}\u0027])");

    public ILocator Button(string name) => Earlier.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    public ILocator Day(DateOnly date) => Earlier.Locator($"tbody[data-work-date='{DateText(date)}']");

    public ILocator Row(DateOnly date, string id) => Day(date).Locator($"tr[data-entry-id='{id}']");

    public ILocator DuplicateButton(DateOnly date, string id) =>
        Row(date, id).GetByRole(AriaRole.Button, new() { Name = "Duplicate entry", Exact = true });

    public async Task<DateOnly> LocalTodayAsync()
    {
        var displayed = await Earlier.GetAttributeAsync("data-local-today");

        return DateOnly.ParseExact(displayed!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public async Task IdleAsync()
    {
        await Expect(Earlier).ToHaveAttributeAsync("aria-busy", "false", new() { Timeout = application.UiTimeoutMilliseconds });
    }

    public async Task EnsureDateLoadedAsync(DateOnly date)
    {
        var older = Button("Load older day");
        var day = Day(date);

        while (await day.CountAsync() == 0 && await older.IsVisibleAsync())
        {
            await older.ClickAsync();
            await IdleAsync();
        }

        await Expect(day).ToHaveCountAsync(1);
    }

    public async Task<string> FirstEntryIdAsync(DateOnly date)
    {
        var id = await Day(date).Locator("tr[data-entry-id]").First.GetAttributeAsync("data-entry-id");
        Assert.NotNull(id);

        return id;
    }

    public async Task DatesAsync(params DateOnly[] dates)
    {
        application.RecordStage("assert exact history dates and retained boundary");
        await Expect(Groups).ToHaveCountAsync(dates.Length);

        for (var index = 0; index < dates.Length; index++)
        {
            await Expect(Groups.Nth(index)).ToHaveAttributeAsync("data-work-date", DateText(dates[index]));
            await Expect(Groups.Nth(index).Locator("time")).ToHaveTextAsync(
                dates[index].ToString("D", CultureInfo.GetCultureInfo("en-US")));
        }

        await IdleAsync();
    }

    public async Task DayAsync(HistoryTestDay expected)
    {
        var rows = Day(expected.Date).Locator("tr[data-entry-id]");
        await Expect(rows).ToHaveCountAsync(expected.Entries.Length);

        for (var index = 0; index < expected.Entries.Length; index++)
        {
            await Expect(rows.Nth(index)).ToHaveAttributeAsync("data-entry-id", expected.Identities[index]);

            for (var column = 0; column < 4; column++)
            {
                await Expect(rows.Nth(index).Locator("td").Nth(column + 1)).ToHaveTextAsync(expected.Entries[index][column]);
            }
        }

        await Expect(Day(expected.Date).Locator(".day-summary")).ToContainTextAsync($"Total: {expected.Total}");
        await Expect(Day(expected.Date).GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true })).ToHaveCountAsync(expected.Entries.Length);
    }

    public async Task OlderAsync(params DateOnly[] expectedDates)
    {
        application.RecordStage("load exactly one older complete day");
        await Button("Load older day").ClickAsync();
        await DatesAsync(expectedDates);
    }

    public async Task ExhaustedAsync()
    {
        await Expect(Button("Load older day")).ToHaveCountAsync(0);
        await Expect(Earlier.GetByText("All earlier days loaded.", new() { Exact = true })).ToBeVisibleAsync();
    }

    public async Task EditAsync(HistoryTestDay day, int entryIndex)
    {
        application.RecordStage("edit historical entry by saved date and identity");
        await Row(day.Date, day.Identities[entryIndex]).GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
    }

    public async Task EditorAcknowledgedAsync(HistoryTestDay day, int entryIndex)
    {
        var editor = new WorkdayScreen(application);
        await Expect(editor.Field("Work date")).ToHaveValueAsync(DateText(day.Date));

        await Expect(editor.Editor.GetByRole(AriaRole.Heading, new() { Name = "Edit entry", Exact = true })).ToBeVisibleAsync();
        await Expect(editor.Field("Jira ID")).ToHaveValueAsync(day.Entries[entryIndex][0]);
        await Expect(editor.Field("Item title")).ToHaveValueAsync(day.Entries[entryIndex][1]);
        await Expect(editor.Field("Work description (optional)")).ToHaveValueAsync(day.Entries[entryIndex][2]);
        await Expect(editor.Field("Duration")).ToHaveValueAsync(day.Entries[entryIndex][3]);
        await Expect(editor.Field("Jira ID")).ToBeFocusedAsync();
    }

    public static string DateText(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}

internal sealed record HistoryTestDay(DateOnly Date, string[][] Entries, string[] Identities, string Total);

internal static class HistoryTestData
{
    public static DateOnly[] SparseDates(DateOnly today)
    {
        var weekend = today.AddDays(-8);

        while (weekend.DayOfWeek != DayOfWeek.Saturday)
        {
            weekend = weekend.AddDays(-1);
        }

        return [today.AddDays(-1), today.AddDays(-4), weekend, today.AddDays(-25), today.AddYears(-2), today.AddYears(-3)];
    }

    public static string[][] Entries(int dayIndex) => dayIndex is 0 or 3
        ? [["REPEAT-1", $"First entry {dayIndex}", "First line\nSecond line", "1h"],
            ["REPEAT-1", $"Separate repeated ticket {dayIndex}", "Different work", "30m"]]
        : [["HISTORY-1", $"Saved day {dayIndex}", $"Complete description {dayIndex}", "2m"]];

    public static string Total(int dayIndex) => dayIndex is 0 or 3 ? "1h 30m" : "2m";

    public static async Task<HistoryTestDay[]> SeedAsync(WorkdayScreen editor, DateOnly today)
    {
        var dates = SparseDates(today);
        var days = new List<HistoryTestDay>();

        for (var index = 0; index < dates.Length; index++)
        {
            await editor.DateAsync(WorkEntryHistoryScreen.DateText(dates[index]));
            var entries = Entries(index);

            foreach (var entry in entries)
            {
                await editor.AddAsync(entry[0], entry[1], entry[2], entry[3]);
            }

            var ids = await editor.Rows.EvaluateAllAsync<string[]>("rows => rows.map(row => row.dataset.entryId)");
            Assert.Equal(ids.Length, ids.Distinct().Count());
            days.Add(new HistoryTestDay(dates[index], entries, ids, Total(index)));
        }

        return days.ToArray();
    }
}
