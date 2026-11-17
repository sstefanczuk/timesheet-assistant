using Microsoft.Data.Sqlite;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace TimesheetAssistant.UiTests.Features.Workdays;

[Collection("Desktop")]
public sealed class WorkdayStorageTests
{
    private const string WorkDate = "2026-01-12";

    [Fact]
    public async Task Independent_stores_exclusion_reacquisition_and_copied_application_preserve_records()
    {
        var scenario = new DesktopScenario();
        await scenario.RunAsync(async () =>
        {
            var owner = await scenario.LaunchAsync();
            var screen = new WorkdayScreen(owner);
            await screen.ReadyAsync();
            await screen.DateAsync(WorkDate);
            await screen.AddAsync("OWN-1", "Retained record", "Owned store", "1h");

            var independentPath = Path.Combine(Path.GetDirectoryName(scenario.DataDirectory)!, "independent");
            var independent = await scenario.LaunchAsync(independentPath);
            var independentScreen = new WorkdayScreen(independent);
            await independentScreen.ReadyAsync();
            await independentScreen.DateAsync(WorkDate);
            await independentScreen.EntriesAsync();
            await independentScreen.TotalsAsync("0m", "Remaining: 7h 12m");
            await screen.EntriesAsync(["OWN-1", "Retained record", "Owned store", "1h"]);
            await independent.DisposeAsync();

            var rejected = await scenario.LaunchAsync();
            await UnavailableAsync(rejected, "Another Timesheet Assistant instance");
            await rejected.DisposeAsync();
            await screen.EntriesAsync(["OWN-1", "Retained record", "Owned store", "1h"]);
            await owner.DisposeAsync();

            var reacquired = await scenario.LaunchAsync();
            screen = new WorkdayScreen(reacquired);
            await screen.ReadyAsync();
            await screen.DateAsync(WorkDate);
            await screen.EntriesAsync(["OWN-1", "Retained record", "Owned store", "1h"]);
            await reacquired.DisposeAsync();

            var copy = scenario.CopyApplication();
            var alternate = await scenario.LaunchAsync(executable: copy);
            screen = new WorkdayScreen(alternate);
            await screen.ReadyAsync();
            await screen.DateAsync(WorkDate);
            await screen.EntriesAsync(["OWN-1", "Retained record", "Owned store", "1h"]);
            await screen.TotalsAsync("1h", "Remaining: 6h 12m");
        });
    }

    [Fact]
    public async Task Invalid_relative_override_is_visible_without_falling_back()
    {
        var scenario = new DesktopScenario();
        await scenario.RunAsync(async () =>
        {
            var launch = await scenario.LaunchAsync("invalid-relative-test-directory");
            await UnavailableAsync(launch, "must be an absolute path");
            Assert.False(File.Exists(scenario.DatabasePath));
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Corrupt_or_future_database_is_rejected_without_replacing_original_bytes(bool futureSchema)
    {
        var scenario = new DesktopScenario();
        await scenario.RunAsync(async () =>
        {
            if (futureSchema)
            {
                using var connection = OpenDatabase(scenario.DatabasePath, create: true);
                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA user_version = 999;";
                command.ExecuteNonQuery();
            }
            else
            {
                await File.WriteAllTextAsync(scenario.DatabasePath, "Test-owned corrupt database, never production data.");
            }

            var original = await File.ReadAllBytesAsync(scenario.DatabasePath);
            var launch = await scenario.LaunchAsync();
            await UnavailableAsync(launch, futureSchema ? "version is not supported" : "Existing data has not been replaced");
            await launch.DisposeAsync();
            Assert.Equal(original, await File.ReadAllBytesAsync(scenario.DatabasePath));
        });
    }

    [Fact]
    public async Task Unreadable_database_preserves_bytes_and_retry_recovers_after_handle_release()
    {
        var scenario = new DesktopScenario();
        await scenario.RunAsync(async () =>
        {
            var initial = await scenario.LaunchAsync();
            await new WorkdayScreen(initial).ReadyAsync();
            await initial.DisposeAsync();
            var original = await File.ReadAllBytesAsync(scenario.DatabasePath);
            DesktopAppFixture launch;

            using (var locked = new FileStream(scenario.DatabasePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                launch = await scenario.LaunchAsync();
                await UnavailableAsync(launch, "Existing data has not been replaced");
            }

            Assert.Equal(original, await File.ReadAllBytesAsync(scenario.DatabasePath));
            await launch.Page.GetByRole(AriaRole.Button, new() { Name = "Retry", Exact = true }).ClickAsync();
            await new WorkdayScreen(launch).ReadyAsync();
        });
    }

    [Fact]
    public async Task Failed_first_initialization_can_retry_without_publishing_an_incomplete_database()
    {
        var scenario = new DesktopScenario();
        await scenario.RunAsync(async () =>
        {
            // Block lease acquisition before the application can create its first database.
            DesktopAppFixture launch;

            using (var lease = new FileStream(Path.Combine(scenario.DataDirectory, "timesheet.lock"),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                launch = await scenario.LaunchAsync();
                await UnavailableAsync(launch, "Another Timesheet Assistant instance");
                Assert.False(File.Exists(scenario.DatabasePath));
            }

            await launch.Page.GetByRole(AriaRole.Button, new() { Name = "Retry", Exact = true }).ClickAsync();
            var screen = new WorkdayScreen(launch);
            await screen.ReadyAsync();
            await screen.DateAsync(WorkDate);
            await screen.AddAsync("RETRY-1", "Recovered initialization", "", "1m");
            await launch.DisposeAsync();
            var restarted = await scenario.LaunchAsync();
            screen = new WorkdayScreen(restarted);
            await screen.ReadyAsync();
            await screen.DateAsync(WorkDate);
            await screen.EntriesAsync(["RETRY-1", "Recovered initialization", "", "1m"]);
        });
    }

    [Fact]
    public async Task Failed_update_keeps_input_and_committed_values_after_recovery_and_restart()
    {
        var scenario = new DesktopScenario();
        await scenario.RunAsync(async () =>
        {
            var launch = await scenario.LaunchAsync();
            var screen = new WorkdayScreen(launch);
            await screen.ReadyAsync();
            await screen.DateAsync(WorkDate);
            await screen.AddAsync("LOCK-1", "Committed title", "Original", "1h");
            await screen.EditAsync(0);
            await screen.FillAsync("LOCK-1", "Unsaved title", "Kept draft", "2h");

            using (var connection = OpenDatabase(scenario.DatabasePath))
            using (var transaction = connection.BeginTransaction())
            {
                launch.RecordStage("test-owned SQLite write lock acquired");
                await screen.Button("Save").ClickAsync();
                await Expect(screen.Alerts.Filter(new() { HasText = "Could not save this entry" }))
                    .ToBeVisibleAsync(new() { Timeout = launch.UiTimeoutMilliseconds });
                await Expect(screen.Field("Item title")).ToHaveValueAsync("Unsaved title");
                await Expect(screen.Field("Work description (optional)")).ToHaveValueAsync("Kept draft");
                await Expect(screen.Field("Duration")).ToHaveValueAsync("2h");
                await Expect(screen.Status).ToHaveCountAsync(0);
                await screen.EntriesAsync(["LOCK-1", "Committed title", "Original", "1h"]);
                await screen.TotalsAsync("1h", "Remaining: 6h 12m");
                transaction.Rollback();
            }

            launch.RecordStage("test-owned SQLite write lock released");
            await launch.DisposeAsync();
            var restarted = await scenario.LaunchAsync();
            screen = new WorkdayScreen(restarted);
            await screen.ReadyAsync();
            await screen.DateAsync(WorkDate);
            await screen.EntriesAsync(["LOCK-1", "Committed title", "Original", "1h"]);
            await screen.TotalsAsync("1h", "Remaining: 6h 12m");
            await screen.EditAsync(0);
            await screen.Field("Duration").FillAsync("2h");
            await screen.SaveAsync("Entry updated.");
            await screen.TotalsAsync("2h", "Remaining: 5h 12m");
        });
    }

    private static SqliteConnection OpenDatabase(string path, bool create = false)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = create ? SqliteOpenMode.ReadWriteCreate : SqliteOpenMode.ReadWrite,
            Pooling = false,
            DefaultTimeout = 1
        }.ToString());
        connection.Open();

        return connection;
    }

    private static async Task UnavailableAsync(DesktopAppFixture launch, string message)
    {
        launch.RecordStage("assert visible storage failure without editor");
        await Expect(launch.Page.GetByRole(AriaRole.Alert)).ToContainTextAsync(message);
        await Expect(launch.Page.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true })).ToHaveCountAsync(0);
        await Expect(launch.Page.GetByRole(AriaRole.Button, new() { Name = "Retry", Exact = true })).ToBeVisibleAsync();
    }
}
