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
    private readonly string artifactDirectory = Path.Combine(FindRepositoryRoot(), "artifacts", "desktop-ui", Guid.NewGuid().ToString("N"));
    private bool tracing;
    private string stage = "configuration";

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "TimesheetAssistant.sln"))) return directory.FullName;
        throw new InvalidOperationException("Cannot locate the repository artifact directory from the test output.");
    }

    public string ArtifactDirectory => artifactDirectory;

    public void RecordStage(string value)
    {
        stage = value;
        try
        {
            Directory.CreateDirectory(artifactDirectory);
            File.AppendAllText(Path.Combine(artifactDirectory, "desktop.log"), $"{DateTimeOffset.UtcNow:O} [{stage}]\n");
        }
        catch (Exception error) { Console.Error.WriteLine($"Could not write desktop diagnostics: {error.Message}"); }
    }

    public async Task CaptureFailureAsync(Exception error)
    {
        RecordStage($"failure during {stage}: {error}");
        if (Page is not null)
        {
            try { await Page.ScreenshotAsync(new() { Path = Path.Combine(artifactDirectory, "failure.png"), Timeout = UiTimeoutMilliseconds }); }
            catch (Exception failure) { RecordStage($"Screenshot unavailable: {failure.Message}"); }
        }
        await StopTraceAsync(save: true);
    }

    private async Task StopTraceAsync(bool save)
    {
        if (!tracing) return;
        tracing = false;
        try { await Page.Context.Tracing.StopAsync(new() { Path = save ? Path.Combine(artifactDirectory, "trace.zip") : null }).WaitAsync(timeouts.Ui); }
        catch (Exception error) { RecordStage($"Trace unavailable: {error.Message}"); }
    }
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
        RecordStage($"configuration; profile={ProfileDirectory}; artifacts={artifactDirectory}");
        try { await InitializeCoreAsync(); }
        catch (Exception error)
        {
            await CaptureFailureAsync(error);
            throw;
        }
    }

    private async Task InitializeCoreAsync()
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
            RecordStage($"launch; executable={executable}; endpoint={endpoint}");
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
            RecordStage($"waiting for CDP; PID={application.Id}; endpoint={endpoint}");
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
                    RecordStage($"connecting; PID={application.Id}; listener={owner}; endpoint={endpoint}");
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
            RecordStage($"connected; PID={application.Id}; endpoint={endpoint}; page={Page.Url}");
            try
            {
                await Page.Context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true }).WaitAsync(startup.Token);
                tracing = true;
            }
            catch (Exception error) { RecordStage($"Trace start unavailable: {error.Message}"); }
            Page.SetDefaultTimeout((float)timeouts.Ui.TotalMilliseconds);
            Page.SetDefaultNavigationTimeout((float)timeouts.Ui.TotalMilliseconds);
        }
        catch (Exception error)
        {
            await CaptureFailureAsync(error);
            Exception? cleanupError = null;
            try { await DisposeAsync(); } catch (Exception failure) { cleanupError = failure; }
            throw new InvalidOperationException($"Desktop startup/connection failed at {endpoint} within {timeouts.Startup.TotalSeconds}s. Check WebView2 Evergreen Runtime and port availability. {error.Message}",
                cleanupError is null ? error : new AggregateException(error, cleanupError));
        }
    }

    public async Task DisposeAsync()
    {
        await StopTraceAsync(save: false);
        RecordStage("cleanup started");
        using var cleanup = new CancellationTokenSource(timeouts.Cleanup);
        var processesAndProfileRemoved = false;
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
            processesAndProfileRemoved = true;
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
            RecordStage(processesAndProfileRemoved ? "cleanup completed" : "cleanup incomplete; inspect test cleanup error");
        }
    }
}
