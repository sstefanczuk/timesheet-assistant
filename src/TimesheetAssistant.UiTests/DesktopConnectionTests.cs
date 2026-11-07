namespace TimesheetAssistant.UiTests;

// Each test owns a fresh app; desktop workflows must not share instances in parallel.
[CollectionDefinition("Desktop", DisableParallelization = true)]
public sealed class DesktopCollection { }

[Collection("Desktop")]
public sealed class DesktopConnectionTests : IClassFixture<DesktopAppFixture>
{
    private readonly DesktopAppFixture application;
    public DesktopConnectionTests(DesktopAppFixture application, Xunit.Abstractions.ITestOutputHelper output)
    {
        this.application = application;
        output.WriteLine($"Desktop PID: {application.ProcessId}; WebView2 profile: {application.ProfileDirectory}");
    }

    [Fact]
    public async Task Published_application_exposes_a_real_WebView2_page()
    {
        Assert.False(application.Page.IsClosed);
        Assert.NotEmpty(application.Page.Context.Pages);
        await Microsoft.Playwright.Assertions.Expect(application.Page.GetByRole(Microsoft.Playwright.AriaRole.Heading,
            new() { Name = "Hello, world!", Exact = true })).ToBeVisibleAsync(new() { Timeout = application.UiTimeoutMilliseconds });
    }
}
