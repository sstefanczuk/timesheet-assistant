namespace TimesheetAssistant.UiTests;

internal sealed class DesktopScenario : IAsyncDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "TimesheetAssistant.UiScenarios", Guid.NewGuid().ToString("N"));
    private readonly List<DesktopAppFixture> _launches = [];

    public DesktopScenario()
    {
        Directory.CreateDirectory(DataDirectory);
    }

    public string DataDirectory => Path.Combine(_root, "data");

    public string DatabasePath => Path.Combine(DataDirectory, "timesheet.db");

    public async Task<DesktopAppFixture> LaunchAsync(string? dataDirectory = null, string? executable = null)
    {
        var launch = new DesktopAppFixture(dataDirectory ?? DataDirectory, executable);
        _launches.Add(launch);
        await launch.InitializeAsync();

        return launch;
    }

    public string CopyApplication()
    {
        var executable = Environment.GetEnvironmentVariable("TIMESHEET_ASSISTANT_TEST_APP")
            ?? throw new InvalidOperationException("Configure the published executable before copying it.");
        var source = Path.GetDirectoryName(Path.GetFullPath(executable))!;
        var target = Path.Combine(_root, "application-copy");
        Directory.CreateDirectory(target);

        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(target, Path.GetRelativePath(source, directory)));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, Path.Combine(target, Path.GetRelativePath(source, file)));
        }

        return Path.Combine(target, Path.GetFileName(executable));
    }

    public async Task RunAsync(Func<Task> scenario)
    {
        Exception? original = null;

        try
        {
            await scenario();
        }
        catch (Exception error)
        {
            original = error;

            foreach (var launch in _launches)
            {
                await launch.CaptureFailureAsync(error);
            }

            throw;
        }
        finally
        {
            try
            {
                await DisposeAsync();
            }
            catch (Exception cleanup) when (original is not null)
            {
                throw new AggregateException("Scenario failed and cleanup also failed.", original, cleanup);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        var errors = new List<Exception>();

        foreach (var launch in _launches.AsEnumerable().Reverse())
        {
            try
            {
                await launch.DisposeAsync();
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        }

        if (errors.Count > 0)
        {
            throw new AggregateException("Owned processes must exit before domain cleanup.", errors);
        }

        var parent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "TimesheetAssistant.UiScenarios"));
        var root = Path.GetFullPath(_root);

        if (!string.Equals(Path.GetDirectoryName(root), parent, StringComparison.OrdinalIgnoreCase)
            || !Guid.TryParseExact(Path.GetFileName(root), "N", out _))
        {
            throw new InvalidOperationException("Domain cleanup requires a unique test-owned scenario root.");
        }

        if (!Directory.Exists(root))
        {
            return;
        }

        // Never traverse redirected directories during recursive cleanup.
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.TryPop(out var directory))
        {
            if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidOperationException("Scenario cleanup cannot traverse redirected directories.");
            }

            foreach (var child in Directory.EnumerateDirectories(directory))
            {
                pending.Push(child);
            }
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        while (Directory.Exists(root))
        {
            timeout.Token.ThrowIfCancellationRequested();

            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch (IOException)
            {
                await Task.Delay(50, timeout.Token);
            }
            catch (UnauthorizedAccessException)
            {
                await Task.Delay(50, timeout.Token);
            }
        }

        foreach (var launch in _launches)
        {
            launch.RecordStage($"scenario cleanup completed; data={DataDirectory}");
        }
    }
}
