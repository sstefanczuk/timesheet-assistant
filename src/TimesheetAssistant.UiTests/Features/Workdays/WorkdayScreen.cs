using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace TimesheetAssistant.UiTests.Features.Workdays;

internal sealed class WorkdayScreen(DesktopAppFixture application)
{
    private readonly IPage _page = application.Page;

    public ILocator Rows => _page.Locator("tbody tr");

    public ILocator Dialog => _page.GetByRole(AriaRole.Alertdialog);

    private ILocator DailyTotals => _page.GetByRole(AriaRole.Region, new() { Name = "Daily totals" });

    public ILocator Field(string name) => _page.GetByLabel(name, new() { Exact = true });

    public ILocator Button(string name) => _page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    public async Task ReadyAsync()
    {
        application.RecordStage("workday editor ready");
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Workday", Exact = true })).ToBeVisibleAsync();
        await Expect(Button("Save")).ToBeEnabledAsync();
        await Expect(DailyTotals).ToHaveAttributeAsync("aria-busy", "false", new() { Timeout = application.UiTimeoutMilliseconds });
    }

    public async Task FillAsync(string jira, string title, string description, string duration)
    {
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
        await Button("Save").ClickAsync();
        await Expect(_page.GetByRole(AriaRole.Status)).ToHaveTextAsync(status);
        await Expect(Field("Item title")).ToHaveValueAsync("");
        await Expect(Field("Jira ID")).ToBeFocusedAsync(new() { Timeout = application.UiTimeoutMilliseconds });
    }

    public async Task AddAsync(string jira, string title, string description, string duration)
    {
        await FillAsync(jira, title, description, duration);
        await SaveAsync();
    }

    public async Task DateAsync(string date)
    {
        application.RecordStage("select work date");
        await Field("Work date").FillAsync(date);
        await Field("Work date").PressAsync("Tab");
        // Browser input echo does not acknowledge the asynchronous Blazor date load.
        await Expect(DailyTotals).ToHaveAttributeAsync("data-work-date", date, new() { Timeout = application.UiTimeoutMilliseconds });
        await Expect(DailyTotals).ToHaveAttributeAsync("aria-busy", "false", new() { Timeout = application.UiTimeoutMilliseconds });
        await Expect(Field("Work date")).ToHaveValueAsync(date);
        await Expect(Button("Save")).ToBeEnabledAsync();
    }

    public async Task TotalsAsync(string total, string balance)
    {
        var totals = _page.GetByRole(AriaRole.Region, new() { Name = "Daily totals" });
        await Expect(totals).ToContainTextAsync($"Total: {total}");
        await Expect(totals).ToContainTextAsync(balance);
    }

    public async Task EntriesAsync(params string[][] entries)
    {
        application.RecordStage("assert exact saved rows and creation order");
        await Expect(Rows).ToHaveCountAsync(entries.Length);

        for (var index = 0; index < entries.Length; index++)
        {
            await Expect(Rows.Nth(index).Locator("td").Nth(0)).ToHaveTextAsync(entries[index][0]);
            await Expect(Rows.Nth(index).Locator("td").Nth(1)).ToHaveTextAsync(entries[index][1]);
            await Expect(Rows.Nth(index).Locator("td").Nth(2)).ToHaveTextAsync(entries[index][2]);
            await Expect(Rows.Nth(index).Locator("td").Nth(3)).ToHaveTextAsync(entries[index][3]);
        }
    }

    public async Task EditAsync(int index)
    {
        await Rows.Nth(index).GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Edit entry", Exact = true })).ToBeVisibleAsync();
        await Expect(Field("Jira ID")).ToBeFocusedAsync(new() { Timeout = application.UiTimeoutMilliseconds });
    }

    public async Task DeleteAsync(int index, bool confirm)
    {
        await Rows.Nth(index).GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true }).ClickAsync();
        await Expect(Dialog).ToBeVisibleAsync();
        await Dialog.GetByRole(AriaRole.Button, new() { Name = confirm ? "Delete" : "Cancel", Exact = true }).ClickAsync();
        await Expect(Dialog).ToHaveCountAsync(0);

        if (confirm)
        {
            await Expect(_page.GetByRole(AriaRole.Status)).ToHaveTextAsync("Entry deleted.");
        }
    }
}
