using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace TimesheetAssistant.UiTests.Features.Workdays;

internal sealed class WorkdaySummaryScreen(DesktopAppFixture application)
{
    private readonly IPage _page = application.Page;

    public ILocator Dialog => _page.Locator("dialog.summary-dialog");
    public ILocator JiraTab => Dialog.GetByRole(AriaRole.Tab, new() { Name = "Jira", Exact = true });
    public ILocator MyWorkTab => Dialog.GetByRole(AriaRole.Tab, new() { Name = "MyWork", Exact = true });
    public ILocator JiraRows => Dialog.Locator("#jira-panel tbody tr");
    public ILocator MyWorkRows => Dialog.Locator("#mywork-panel tbody tr");
    public ILocator CopyStatus => Dialog.GetByRole(AriaRole.Status);

    public async Task JiraAsync()
    {
        await Expect(JiraTab).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Dialog.Locator("#jira-panel")).ToBeVisibleAsync();
    }

    public async Task MyWorkAsync()
    {
        await MyWorkTab.ClickAsync();
        await Expect(MyWorkTab).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Dialog.Locator("#mywork-panel")).ToBeVisibleAsync();
    }

    public async Task CloseAsync()
    {
        await Dialog.GetByRole(AriaRole.Button, new() { Name = "Close", Exact = true }).ClickAsync();
        await Expect(Dialog).ToHaveCountAsync(0);
    }
}
