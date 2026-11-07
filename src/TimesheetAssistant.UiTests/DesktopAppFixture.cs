using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Playwright;

namespace TimesheetAssistant.UiTests;

public sealed record DesktopTimeouts
{
    public TimeSpan Startup { get; init; } = TimeSpan.FromSeconds(60);
    public TimeSpan Ui { get; init; } = TimeSpan.FromSeconds(15);
    public TimeSpan Cleanup { get; init; } = TimeSpan.FromSeconds(15);
}

public sealed class DesktopAppFixture : IAsyncLifetime
{
    private readonly DesktopTimeouts timeouts;
    private Process? application;
    private OwnedProcessJob? job;
    private IPlaywright? playwright;
    private IBrowser? browser;
    private readonly string sessionDirectory = Path.Combine(Path.GetTempPath(), "TimesheetAssistant.UiTests", Guid.NewGuid().ToString("N"));

    public DesktopAppFixture() : this(new DesktopTimeouts()) { }
    internal DesktopAppFixture(DesktopTimeouts timeouts) => this.timeouts = timeouts;
    public IPage Page { get; private set; } = null!;
    public string ProfileDirectory => Path.Combine(sessionDirectory, "webview2");
    public int ProcessId => application!.Id;
    public float UiTimeoutMilliseconds => (float)timeouts.Ui.TotalMilliseconds;

    public async Task InitializeAsync()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Desktop UI tests require Windows and WebView2 Evergreen Runtime.");
        var configuredPath = Environment.GetEnvironmentVariable("TIMESHEET_ASSISTANT_TEST_APP");
        if (string.IsNullOrWhiteSpace(configuredPath))
            throw new InvalidOperationException("Set TIMESHEET_ASSISTANT_TEST_APP to the published Release TimesheetAssistant.exe.");
        var executable = Path.GetFullPath(configuredPath);
        if (!File.Exists(executable))
            throw new FileNotFoundException("Desktop test executable does not exist: " + executable, executable);

        using var startup = new CancellationTokenSource(timeouts.Startup);
        var portReservation = new TcpListener(IPAddress.Loopback, 0);
        portReservation.Start();
        var port = ((IPEndPoint)portReservation.LocalEndpoint).Port;
        portReservation.Stop();
        var endpoint = $"http://127.0.0.1:{port}";
        try
        {
            Directory.CreateDirectory(ProfileDirectory);
            job = new OwnedProcessJob();
            var start = new ProcessStartInfo(executable)
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(executable)!
            };
            start.Environment["WEBVIEW2_USER_DATA_FOLDER"] = ProfileDirectory;
            start.Environment["WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS"] = $"--remote-debugging-port={port} --remote-debugging-address=127.0.0.1";
            application = job.StartApplication(start, timeouts.Cleanup);
            var creation = Playwright.CreateAsync();
            try { playwright = await creation.WaitAsync(startup.Token); }
            catch
            {
                _ = creation.ContinueWith(task => { if (task.IsCompletedSuccessfully) task.Result.Dispose(); }, TaskScheduler.Default);
                throw;
            }
            while (true)
            {
                startup.Token.ThrowIfCancellationRequested();
                if (application.HasExited)
                    throw new InvalidOperationException($"Desktop process {application.Id} exited before WebView2 connected (exit code {application.ExitCode}).");
                var owner = OwnedProcessJob.ListenerOwner(port);
                if (owner is not null)
                {
                    if (!job.Contains(owner.Value))
                        throw new InvalidOperationException($"CDP port collision at {endpoint}: listener PID {owner} is not owned by desktop PID {application.Id}.");
                    browser = await playwright.Chromium.ConnectOverCDPAsync(endpoint,
                        new() { Timeout = (float)timeouts.Startup.TotalMilliseconds }).WaitAsync(startup.Token);
                    break;
                }
                await Task.Delay(100, startup.Token);
            }
            while (Page is null)
            {
                startup.Token.ThrowIfCancellationRequested();
                if (application.HasExited)
                    throw new InvalidOperationException("Desktop process exited while waiting for its WebView2 page.");
                Page = browser.Contexts.SelectMany(context => context.Pages).FirstOrDefault()!;
                if (Page is null) await Task.Delay(100, startup.Token);
            }
            Page.SetDefaultTimeout((float)timeouts.Ui.TotalMilliseconds);
            Page.SetDefaultNavigationTimeout((float)timeouts.Ui.TotalMilliseconds);
        }
        catch (Exception error)
        {
            Exception? cleanupError = null;
            try { await DisposeAsync(); } catch (Exception failure) { cleanupError = failure; }
            throw new InvalidOperationException($"Desktop startup/connection failed at {endpoint} within {timeouts.Startup.TotalSeconds}s. Check WebView2 Evergreen Runtime and port availability. {error.Message}",
                cleanupError is null ? error : new AggregateException(error, cleanupError));
        }
    }

    public async Task DisposeAsync()
    {
        using var cleanup = new CancellationTokenSource(timeouts.Cleanup);
        try
        {
            // Terminate only this Windows job, including descendants after the app exits.
            if (job is not null)
            {
                job.Terminate();
                while (job.HasProcesses)
                    await Task.Delay(50, cleanup.Token);
            }
            if (application is { HasExited: false })
            {
                application.Kill(entireProcessTree: true);
                await application.WaitForExitAsync(cleanup.Token);
            }
            while (Directory.Exists(sessionDirectory))
            {
                cleanup.Token.ThrowIfCancellationRequested();
                try { Directory.Delete(sessionDirectory, recursive: true); }
                catch (IOException) { await Task.Delay(50, cleanup.Token); }
                catch (UnauthorizedAccessException) { await Task.Delay(50, cleanup.Token); }
            }
        }
        finally
        {
            job?.Dispose();
            job = null;
            application?.Dispose();
            application = null;
            if (playwright is not null)
                await Task.Run(playwright.Dispose).WaitAsync(cleanup.Token);
            playwright = null;
        }
    }
}
