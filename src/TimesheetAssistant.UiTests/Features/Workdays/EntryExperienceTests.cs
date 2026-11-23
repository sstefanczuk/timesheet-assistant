using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace TimesheetAssistant.UiTests.Features.Workdays;

[Collection("Desktop")]
public sealed class EntryExperienceTests
{
    [Fact]
    public async Task Validation_delete_and_older_history_preserve_entries_and_totals()
    {
        var scenario = new DesktopScenario();
        await scenario.RunAsync(async () =>
        {
            var launch = await scenario.LaunchAsync();
            var page = launch.Page;
            var add = page.GetByRole(AriaRole.Button, new() { Name = "Add entry", Exact = true });
            var dialog = page.GetByRole(AriaRole.Dialog);
            await Expect(add).ToBeEnabledAsync();
            await Expect(page.Locator(".global-status")).ToHaveTextAsync("Ready");
            await add.ClickAsync();
            await dialog.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync();
            await Expect(dialog.GetByLabel("Jira ID", new() { Exact = true })).ToHaveAttributeAsync("aria-invalid", "true");
            await Expect(dialog).ToBeVisibleAsync();

            for (var index = 0; index < 5; index++)
            {
                if (index > 0)
                {
                    await add.ClickAsync();
                }

                var date = DateOnly.FromDateTime(DateTime.Now).AddDays(-index);
                await dialog.GetByLabel("Work date", new() { Exact = true }).FillAsync(date.ToString("yyyy-MM-dd"));
                var jiraId = $"ENTRY-VERY-LONG-IDENTIFIER-{index}";
                await dialog.GetByLabel("Jira ID", new() { Exact = true }).FillAsync(jiraId);
                await dialog.GetByLabel("Item title", new() { Exact = true }).FillAsync($"Work {index}");
                await dialog.GetByLabel("Duration", new() { Exact = true }).FillAsync("1h");
                await dialog.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync();
                await Expect(dialog).ToHaveCountAsync(0);
                await Expect(page.GetByRole(AriaRole.Region, new() { Name = "Entries" })).ToHaveAttributeAsync("aria-busy", "false");
            }

            await launch.DisposeAsync();
            launch = await scenario.LaunchAsync();
            page = launch.Page;
            var rows = page.Locator("tr[data-entry-id]");
            await Expect(rows).ToHaveCountAsync(4);
            await Expect(rows.Nth(0)).ToContainTextAsync("ENTRY-VERY-LONG-IDENTIFIER-0");
            await Expect(page.Locator("tbody").First).ToContainTextAsync("Remaining: 6h 12m");
            await Expect(page.Locator("tbody").First.Locator(".status-warning")).ToHaveCountAsync(1);
            await Expect(page.Locator(".global-status")).ToHaveTextAsync("Ready");
            await Expect(page.Locator("caption")).ToHaveCountAsync(0);
            await Expect(page.Locator("thead .entry-date")).ToHaveCSSAsync("white-space", "nowrap");
            await Expect(page.Locator("thead .entry-jira")).ToHaveCSSAsync("white-space", "nowrap");
            await Expect(page.Locator("thead .entry-date")).ToHaveCSSAsync("max-width", "94.25px");
            await Expect(page.Locator("thead .entry-jira")).ToHaveCSSAsync("max-width", "91px");
            await Expect(page.Locator("tr[data-entry-id]").First.Locator(".entry-jira"))
                .ToHaveAttributeAsync("title", "ENTRY-VERY-LONG-IDENTIFIER-0");
            await Expect(page.Locator("tr[data-entry-id]").First.Locator(".entry-jira"))
                .ToHaveCSSAsync("text-overflow", "ellipsis");
            await page.GetByRole(AriaRole.Button, new() { Name = "Load older day" }).ClickAsync();
            await Expect(rows).ToHaveCountAsync(5);
            await Expect(rows.Last).ToContainTextAsync("ENTRY-VERY-LONG-IDENTIFIER-4");
            await rows.First.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
            var confirmation = page.GetByRole(AriaRole.Alertdialog);
            await confirmation.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
            await Expect(rows).ToHaveCountAsync(5);
            await rows.First.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
            await confirmation.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
            await Expect(page.Locator("tr[data-entry-id]").Filter(new() { HasText = "ENTRY-0" })).ToHaveCountAsync(0);
        });
    }

    [Fact]
    public async Task Draft_navigation_save_edit_discard_and_restart_preserve_expected_state()
    {
        var scenario = new DesktopScenario();
        await scenario.RunAsync(async () =>
        {
            var first = await scenario.LaunchAsync();
            var page = first.Page;
            var add = page.GetByRole(AriaRole.Button, new() { Name = "Add entry", Exact = true });
            var dialog = page.GetByRole(AriaRole.Dialog);
            await Expect(add).ToBeEnabledAsync();
            await add.ClickAsync();
            await Expect(dialog).ToBeVisibleAsync();
            await Expect(dialog.GetByLabel("Work date", new() { Exact = true })).ToBeFocusedAsync();
            for (var index = 0; index < 8; index++)
            {
                await page.Keyboard.PressAsync("Tab");
                Assert.True(await dialog.EvaluateAsync<bool>("dialog => dialog.contains(document.activeElement)"));
            }
            await dialog.GetByLabel("Jira ID", new() { Exact = true }).FillAsync("DRAFT-1");
            await dialog.GetByLabel("Item title", new() { Exact = true }).FillAsync("Draft work");
            await dialog.GetByLabel("Duration", new() { Exact = true }).FillAsync("1h");
            await page.Keyboard.PressAsync("Escape");
            await Expect(add).ToBeFocusedAsync();
            await page.GetByRole(AriaRole.Link, new() { Name = "Settings", Exact = true }).ClickAsync();
            await page.GetByRole(AriaRole.Link, new() { Name = "Timesheet", Exact = true }).ClickAsync();
            await add.ClickAsync();
            await Expect(dialog.GetByLabel("Jira ID", new() { Exact = true })).ToHaveValueAsync("DRAFT-1");
            await dialog.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync();
            await Expect(dialog).ToHaveCountAsync(0);
            var row = page.Locator("tr[data-entry-id]").Filter(new() { HasText = "DRAFT-1" });
            await Expect(row).ToHaveCountAsync(1);
            await row.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
            await dialog.GetByLabel("Item title", new() { Exact = true }).FillAsync("Discard this edit");
            await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
            await page.GetByRole(AriaRole.Link, new() { Name = "Settings", Exact = true }).ClickAsync();
            await page.GetByRole(AriaRole.Link, new() { Name = "Timesheet", Exact = true }).ClickAsync();
            await Expect(row).ToContainTextAsync("Draft work");
            await add.ClickAsync();
            await Expect(dialog.GetByLabel("Jira ID", new() { Exact = true })).ToHaveValueAsync("");
            await dialog.GetByLabel("Jira ID", new() { Exact = true }).FillAsync("SESSION-ONLY");
            await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
            await first.DisposeAsync();
            var restarted = await scenario.LaunchAsync();
            await Expect(restarted.Page.Locator("tr[data-entry-id]").Filter(new() { HasText = "DRAFT-1" })).ToContainTextAsync("Draft work");
            await restarted.Page.GetByRole(AriaRole.Button, new() { Name = "Add entry", Exact = true }).ClickAsync();
            await Expect(restarted.Page.GetByRole(AriaRole.Dialog).GetByLabel("Jira ID", new() { Exact = true })).ToHaveValueAsync("");
        });
    }
}
