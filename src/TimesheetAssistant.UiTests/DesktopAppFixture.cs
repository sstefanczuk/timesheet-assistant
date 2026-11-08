using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
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
    private bool failureCaptured;
    private string? lastProcessDiagnosticError;
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
        failureCaptured = true;
        RecordStage($"failure during {stage}: {error}");
        await SaveScreenshotAsync("failure.png");
        await StopTraceAsync(save: true);
    }

    private async Task SaveScreenshotAsync(string fileName)
    {
        if (Page is not null)
        {
            try { await Page.ScreenshotAsync(new() { Path = Path.Combine(artifactDirectory, fileName), Timeout = UiTimeoutMilliseconds }); }
            catch (Exception failure) { RecordStage($"Screenshot unavailable: {failure.Message}"); }
        }
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
        var startupElapsed = Stopwatch.StartNew();
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
            var runtimeLog = Path.Combine(artifactDirectory, "webview2.log");
            start.Environment["WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS"] = $"--remote-debugging-port={port} --remote-debugging-address=127.0.0.1 --enable-logging --log-file=\"{runtimeLog}\"";
            application = job.StartApplication(start, timeouts.Cleanup);
            RecordStage($"waiting for CDP; PID={application.Id}; endpoint={endpoint}");
            ObserveOwnedProcesses();
            var creation = Playwright.CreateAsync();
            try { playwright = await creation.WaitAsync(startup.Token); }
            catch
            {
                _ = creation.ContinueWith(task => { if (task.IsCompletedSuccessfully) task.Result.Dispose(); }, TaskScheduler.Default);
                throw;
            }
            using var discoveryHandler = new HttpClientHandler { UseProxy = false, AllowAutoRedirect = false };
            using var discovery = new HttpClient(discoveryHandler) { Timeout = Timeout.InfiniteTimeSpan };
            string? lastReadiness = null;
            void ReportReadiness(string reason)
            {
                if (lastReadiness == reason) return;
                lastReadiness = reason;
                RecordStage($"CDP readiness; {reason}; endpoint={endpoint}");
            }
            Uri? websocket = null;
            while (websocket is null)
            {
                ObserveOwnedProcesses();
                startup.Token.ThrowIfCancellationRequested();
                var owner = GetOwnedListener(port, endpoint);
                if (owner is not null)
                {
                    using var request = CancellationTokenSource.CreateLinkedTokenSource(startup.Token);
                    request.CancelAfter(TimeSpan.FromSeconds(2));
                    try
                    {
                        using var response = await discovery.GetAsync(endpoint + "/json/version/", request.Token);
                        if (!response.IsSuccessStatusCode)
                            throw new InvalidOperationException($"Owned CDP discovery returned HTTP {(int)response.StatusCode}.");
                        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(request.Token));
                        var value = document.RootElement.GetProperty("webSocketDebuggerUrl").GetString();
                        if (!Uri.TryCreate(value, UriKind.Absolute, out var candidate)
                            || candidate.Scheme != "ws" || candidate.Port != port
                            || !(candidate.Host == "127.0.0.1" || candidate.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                            || candidate.UserInfo.Length != 0 || candidate.Fragment.Length != 0)
                            throw new InvalidOperationException("Owned CDP discovery supplied an invalid websocket endpoint; expected ws://loopback on the reserved port.");
                        if (GetOwnedListener(port, endpoint) == owner)
                            websocket = new UriBuilder(candidate) { Host = "127.0.0.1" }.Uri;
                        else ReportReadiness("owned listener changed during discovery");
                    }
                    catch (HttpRequestException error)
                    {
                        startup.Token.ThrowIfCancellationRequested();
                        ReportReadiness($"listener={owner}; HTTP transport {error.HttpRequestError}");
                    }
                    catch (OperationCanceledException) when (!startup.IsCancellationRequested)
                    {
                        ReportReadiness($"listener={owner}; HTTP request exceeded 2s");
                    }
                }
                else ReportReadiness("waiting for owned listener");
                if (websocket is null) await Task.Delay(100, startup.Token);
            }
            startup.Token.ThrowIfCancellationRequested();
            var attachOwner = GetOwnedListener(port, endpoint);
            if (attachOwner is null) throw new InvalidOperationException("Owned CDP listener disappeared before websocket attachment.");
            var remainingStartup = timeouts.Startup - startupElapsed.Elapsed;
            if (remainingStartup <= TimeSpan.Zero) throw new TimeoutException("Desktop startup budget expired before websocket attachment.");
            RecordStage($"connecting websocket; PID={application.Id}; listener={attachOwner}; endpoint={websocket}");
            browser = await playwright.Chromium.ConnectOverCDPAsync(websocket.AbsoluteUri,
                new() { Timeout = (float)remainingStartup.TotalMilliseconds }).WaitAsync(startup.Token);
            while (Page is null)
            {
                ObserveOwnedProcesses();
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
            ObserveOwnedProcesses();
            await CaptureFailureAsync(error);
            Exception? cleanupError = null;
            try { await DisposeAsync(); } catch (Exception failure) { cleanupError = failure; }
            throw new InvalidOperationException($"Desktop startup/connection failed at {endpoint} within {timeouts.Startup.TotalSeconds}s. Check WebView2 Evergreen Runtime and port availability. {error.Message}",
                cleanupError is null ? error : new AggregateException(error, cleanupError));
        }
    }

    private void ObserveOwnedProcesses(bool cleanup = false)
    {
        if (job is null) return;
        var currentStage = stage;
        try
        {
            foreach (var observation in job.ObserveProcesses(cleanup)) RecordStage(observation);
        }
        catch (Exception error)
        {
            var reason = $"Owned process diagnostics unavailable: {error.GetType().Name}; HRESULT=0x{error.HResult:X8}";
            if (reason != lastProcessDiagnosticError)
            {
                lastProcessDiagnosticError = reason;
                RecordStage(reason);
            }
        }
        finally { stage = currentStage; }
    }

    private uint? GetOwnedListener(int port, string endpoint)
    {
        if (application!.HasExited)
            throw new InvalidOperationException($"Desktop process {application.Id} exited before WebView2 connected.");
        var owner = OwnedProcessJob.ListenerOwner(port);
        if (owner is null) return null;
        try
        {
            if (!job!.Contains(owner.Value))
                throw new InvalidOperationException($"CDP port collision at {endpoint}: listener PID {owner} is not owned by desktop PID {application.Id}.");
        }
        catch (ArgumentException) { return null; } // Listener exited between the TCP snapshot and ownership check.
        return owner;
    }

    public async Task DisposeAsync()
    {
        // Once the job is terminated, page diagnostics cannot be recovered.
        if (!failureCaptured) await SaveScreenshotAsync("cleanup.png");
        await StopTraceAsync(save: true);
        try { await CleanupCoreAsync(); }
        catch (Exception error)
        {
            RecordStage($"cleanup failed: {error}");
            throw;
        }
        if (!failureCaptured)
        {
            try
            {
                File.Delete(Path.Combine(artifactDirectory, "cleanup.png"));
                File.Delete(Path.Combine(artifactDirectory, "trace.zip"));
            }
            catch (Exception error) { RecordStage($"Could not remove successful cleanup diagnostics: {error.Message}"); }
        }
    }

    private async Task CleanupCoreAsync()
    {
        RecordStage("cleanup started");
        using var cleanup = new CancellationTokenSource(timeouts.Cleanup);
        var processesAndProfileRemoved = false;
        try
        {
            // Terminate only this Windows job, including descendants after the app exits.
            if (job is not null)
            {
                ObserveOwnedProcesses(); // Capture natural exits before test-driven termination.
                job.Terminate();
                while (job.HasProcesses)
                    await Task.Delay(50, cleanup.Token);
                ObserveOwnedProcesses(cleanup: true);
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
