using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace TimesheetAssistant.UiTests.Features.Workdays;

internal sealed class WorkdayScreen(DesktopAppFixture application)
{
    private readonly IPage _page = application.Page;
    private string _date = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd");

    public ILocator Editor => _page.GetByRole(AriaRole.Dialog);
    public ILocator Rows => _page.Locator($"tbody[data-work-date='{_date}'] tr[data-entry-id]");
    public ILocator Status => _page.Locator(".global-status");
    public ILocator Alerts => Editor.GetByRole(AriaRole.Alert);
    public ILocator Dialog => _page.GetByRole(AriaRole.Alertdialog);
    public ILocator DailyTotals => _page.Locator($"tbody[data-work-date='{_date}'] .day-summary");
    public ILocator Field(string name) => Editor.GetByLabel(name, new() { Exact = true });
    public ILocator Button(string name) => Editor.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    public async Task ReadyAsync()
    {
        application.RecordStage("entries table ready");
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Timesheet", Exact = true })).ToBeVisibleAsync();
        await Expect(_page.GetByRole(AriaRole.Region, new() { Name = "Entries", Exact = true })).ToHaveAttributeAsync("aria-busy", "false", new() { Timeout = application.UiTimeoutMilliseconds });
        await Expect(_page.GetByRole(AriaRole.Button, new() { Name = "Add entry", Exact = true })).ToBeEnabledAsync();
        await Expect(_page.GetByRole(AriaRole.Button, new() { Name = "Refresh", Exact = true })).ToBeEnabledAsync();
    }

    public async Task OpenAddAsync()
    {
        if (!await Editor.IsVisibleAsync())
        {
            await _page.GetByRole(AriaRole.Button, new() { Name = "Add entry", Exact = true }).ClickAsync();
            await Expect(Editor).ToBeVisibleAsync();
        }
    }

    public async Task FillAsync(string jira, string title, string description, string duration)
    {
        await OpenAddAsync();
        await Field("Jira ID").FillAsync(jira);
        await Field("Item title").FillAsync(title);
        await Field("Work description (optional)").FillAsync(description);
        await Field("Duration").FillAsync(duration);
        await Expect(Field("Jira ID")).ToHaveValueAsync(jira);
        await Expect(Field("Item title")).ToHaveValueAsync(title);
        await Expect(Field("Work description (optional)")).ToHaveValueAsync(description);
        await Expect(Field("Duration")).ToHaveValueAsync(duration);
    }

    public async Task SaveAsync(string status = "Entry saved.")
    {
        application.RecordStage("save entry through UI");
        _date = await Field("Work date").InputValueAsync();
        await Button("Save").ClickAsync();
        await Expect(Editor).ToHaveCountAsync(0);
        await Expect(Status).ToHaveTextAsync(status);
        await Expect(_page.GetByRole(AriaRole.Region, new() { Name = "Entries" })).ToHaveAttributeAsync("aria-busy", "false", new() { Timeout = application.UiTimeoutMilliseconds });
        await EnsureDateVisibleAsync();
    }

    public async Task AddAsync(string jira, string title, string description, string duration)
    {
        await FillAsync(jira, title, description, duration);
        await SaveAsync();
    }

    public async Task DateAsync(string date)
    {
        await OpenAddAsync();
        await Field("Work date").FillAsync(date);
        await Field("Work date").PressAsync("Tab");
        await Expect(Field("Work date")).ToHaveValueAsync(date);
        _date = date;
        await Button("Cancel").ClickAsync();
        await Expect(Editor).ToHaveCountAsync(0);
        await EnsureDateVisibleAsync();
    }

    private async Task EnsureDateVisibleAsync()
    {
        var older = _page.GetByRole(AriaRole.Button, new() { Name = "Load older day", Exact = true });
        var group = _page.Locator($"tbody[data-work-date='{_date}']");
        while (await group.CountAsync() == 0 && await older.IsVisibleAsync())
        {
            await older.ClickAsync();
            await Expect(_page.GetByRole(AriaRole.Region, new() { Name = "Entries" })).ToHaveAttributeAsync("aria-busy", "false", new() { Timeout = application.UiTimeoutMilliseconds });
        }
    }

    public async Task TotalsAsync(string total, string balance)
    {
        await EnsureDateVisibleAsync();
        await Expect(DailyTotals).ToContainTextAsync($"Total: {total}");
        await Expect(DailyTotals).ToContainTextAsync(balance);
    }

    public async Task EntriesAsync(params string[][] entries)
    {
        await EnsureDateVisibleAsync();
        await Expect(Rows).ToHaveCountAsync(entries.Length);
        for (var index = 0; index < entries.Length; index++)
        {
            for (var column = 0; column < 4; column++)
            {
                await Expect(Rows.Nth(index).Locator("td").Nth(column + 1)).ToHaveTextAsync(entries[index][column]);
            }
        }
    }

    public async Task EditAsync(int index)
    {
        await EnsureDateVisibleAsync();
        await Rows.Nth(index).GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
        await Expect(Editor.GetByRole(AriaRole.Heading, new() { Name = "Edit entry", Exact = true })).ToBeVisibleAsync();
        await Expect(Field("Jira ID")).ToBeFocusedAsync(new() { Timeout = application.UiTimeoutMilliseconds });
    }

    public async Task DeleteAsync(int index, bool confirm)
    {
        await EnsureDateVisibleAsync();
        await Rows.Nth(index).GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true }).ClickAsync();
        await Expect(Dialog).ToBeVisibleAsync();
        await Dialog.GetByRole(AriaRole.Button, new() { Name = confirm ? "Delete" : "Cancel", Exact = true }).ClickAsync();
        await Expect(Dialog).ToHaveCountAsync(0);
        if (confirm)
        {
            await Expect(Status).ToHaveTextAsync("Entry deleted.");
        }
    }
}
