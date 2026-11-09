using System.Diagnostics;
using System.Text.Json;

namespace TimesheetAssistant.UiTests;

// Opt-in diagnostics only. All debugger output and dumps stay outside artifact upload roots.
internal sealed class NativeCrashCapture : IDisposable
{
    private readonly string executable;
    private readonly string directory;
    private readonly DateTime deadline;
    private readonly List<(uint Pid, string Creation, Process Helper)> monitors = new();
    private readonly HashSet<uint> attempted = new();
    private readonly CancellationTokenSource stop = new();
    private readonly Task watchdog;
    private string status = "waiting-for-owned-process";
    private bool disposed;
    private bool captureStopped;
    public NativeCrashCapture(string executable, string directory, DateTime deadline)
    {
        var root = Path.GetFullPath(directory);
        var temporaryRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!root.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Native capture must remain in a child of temporary storage.");
        this.executable = executable;
        this.directory = root;
        this.deadline = deadline;
        Directory.CreateDirectory(root);
        watchdog = WatchAsync();
    }
    public void Observe(OwnedProcessJob job)
    {
        lock (monitors)
        {
            if (disposed || captureStopped || DateTime.UtcNow >= deadline) return;
            foreach (var (pid, creation) in job.LiveWebViewInstances)
            {
                if (attempted.Count >= 2) break;
                if (!attempted.Add(pid)) continue;
                try
                {
                    var attached = job.WithVerifiedLiveProcess(pid, creation, () =>
                    {
                        var target = Path.Combine(directory, $"capture-{pid}.dmp");
                        var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true,
                            RedirectStandardOutput = true, RedirectStandardError = true };
                        foreach (var argument in new[] { "-accepteula", "-e", "1", "-b", "-f", "80000003", "-n", "1", "-mt", pid.ToString(), target })
                            start.ArgumentList.Add(argument);
                        var helper = Process.Start(start) ?? throw new InvalidOperationException("ProcDump did not start.");
                        // Drain without exporting or retaining unbounded raw debugger output.
                        helper.OutputDataReceived += (_, _) => { };
                        helper.ErrorDataReceived += (_, _) => { };
                        helper.BeginOutputReadLine(); helper.BeginErrorReadLine();
                        monitors.Add((pid, creation, helper));
                        File.WriteAllText(Path.Combine(directory, $"capture-{pid}.identity.json"),
                            JsonSerializer.Serialize(new { Pid = pid, CreationFileTime = creation,
                                VerifiedUtc = DateTime.UtcNow, InitialBreakpointExcluded = false,
                                CaptureKind = "first-chance-breakpoint-ambiguous" }));
                        status = "monitoring-first-chance-ambiguous";
                    });
                    if (!attached) status = "identity-changed-or-exited-before-attach";
                }
                catch { status = "attach-unavailable"; }
            }
        }
    }
    private async Task WatchAsync()
    {
        try
        {
            while (!stop.IsCancellationRequested)
            {
                lock (monitors)
                {
                    long bytes = Directory.EnumerateFiles(directory, "*.dmp").Sum(path => new FileInfo(path).Length);
                    if (bytes > 256L * 1024 * 1024) status = "private-size-limit";
                    if (DateTime.UtcNow >= deadline || status == "private-size-limit")
                    {
                        captureStopped = true;
                        CancelMonitors();
                        return;
                    }
                }
                await Task.Delay(25, stop.Token);
            }
        }
        catch (OperationCanceledException) { }
        catch { lock (monitors) { captureStopped = true; status = "watchdog-unavailable"; CancelMonitors(); } }
    }
    private void CancelMonitors()
    {
        foreach (var monitor in monitors)
        {
            if (monitor.Helper.HasExited) continue;
            // ProcDump's documented cancellation event resumes a target during collection.
            try { using var signal = EventWaitHandle.OpenExisting($"ProcDump-{monitor.Pid}"); signal.Set(); }
            catch { status = "graceful-cancel-unavailable"; }
        }
    }
    public void Dispose()
    {
        lock (monitors) { if (disposed) return; disposed = true; CancelMonitors(); }
        stop.Cancel();
        watchdog.GetAwaiter().GetResult();
        var complete = true;
        foreach (var monitor in monitors)
        {
            try
            {
                // This runs before the app job is terminated; only retained helper instances are stopped.
                if (!monitor.Helper.HasExited && !monitor.Helper.WaitForExit(250))
                { monitor.Helper.Kill(); complete &= monitor.Helper.WaitForExit(250); status = "forced-helper-stop-symptom-ambiguous"; }
            }
            catch { complete = false; }
            finally { monitor.Helper.Dispose(); }
        }
        File.WriteAllText(Path.Combine(directory, "capture-status.json"), JsonSerializer.Serialize(new {
            Status = status, Attempted = attempted.Count, HelpersStopped = complete, DeadlineUtc = deadline,
            InitialBreakpointExcluded = false }));
        stop.Dispose();
    }
}
