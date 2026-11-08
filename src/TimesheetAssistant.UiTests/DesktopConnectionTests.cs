using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace TimesheetAssistant.UiTests;

// This collection owns one fresh app per run; workflows must not share it in parallel.
[CollectionDefinition("Desktop", DisableParallelization = true)]
public sealed class DesktopCollection { }

[Collection("Desktop")]
public sealed class DesktopConnectionTests : IClassFixture<DesktopAppFixture>
{
    private readonly DesktopAppFixture application;
    public DesktopConnectionTests(DesktopAppFixture application, Xunit.Abstractions.ITestOutputHelper output)
    {
        this.application = application;
        output.WriteLine($"Desktop PID: {application.ProcessId}; WebView2 profile: {application.ProfileDirectory}; artifacts: {application.ArtifactDirectory}");
    }

    [Fact]
    public async Task Published_application_navigates_to_Counter_and_increments_from_zero_to_one()
    {
        try
        {
            var page = application.Page;
            var assertion = new LocatorAssertionsToBeVisibleOptions { Timeout = application.UiTimeoutMilliseconds };
            application.RecordStage("Home heading");
            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Hello, world!", Exact = true })).ToBeVisibleAsync(assertion);

            application.RecordStage("Counter navigation");
            var counterLink = page.GetByRole(AriaRole.Link, new() { Name = "Counter", Exact = true });
            if (!await counterLink.IsVisibleAsync())
                await page.GetByRole(AriaRole.Checkbox, new() { Name = "Navigation menu", Exact = true }).CheckAsync();
            await counterLink.ClickAsync();
            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Counter", Exact = true })).ToBeVisibleAsync(assertion);

            application.RecordStage("Counter starts at zero");
            var status = page.GetByRole(AriaRole.Status);
            await Expect(status).ToHaveTextAsync("Current count: 0", new() { Timeout = application.UiTimeoutMilliseconds });
            application.RecordStage("Counter click");
            await page.GetByRole(AriaRole.Button, new() { Name = "Click me", Exact = true }).ClickAsync();
            application.RecordStage("Counter reaches one");
            await Expect(status).ToHaveTextAsync("Current count: 1", new() { Timeout = application.UiTimeoutMilliseconds });
            application.RecordStage("interaction completed");
        }
        catch (Exception error)
        {
            await application.CaptureFailureAsync(error);
            throw;
        }
    }
}
