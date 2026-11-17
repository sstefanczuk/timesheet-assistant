using System.Globalization;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace TimesheetAssistant.UiTests.Features.Workdays;

[Collection("Desktop")]
public sealed class WorkEntryHistoryUpgradeTests
{
    [Fact]
    public async Task Accepted_S01_package_data_keeps_exact_identities_and_order_in_new_history_and_accepts_further_writes()
    {
        var scenario = new DesktopScenario();
        await scenario.RunAsync(async () =>
        {
            var previousExecutable = PreviousExecutable();
            var currentExecutable = Environment.GetEnvironmentVariable("TIMESHEET_ASSISTANT_TEST_APP")
                ?? throw new InvalidOperationException("Configure the new package using TIMESHEET_ASSISTANT_TEST_APP.");
            Assert.False(string.Equals(previousExecutable, Path.GetFullPath(currentExecutable), StringComparison.OrdinalIgnoreCase),
                "Upgrade verification requires two different application packages.");
            var previous = await scenario.LaunchAsync(executable: previousExecutable);
            previous.RecordStage("upgrade baseline: accepted S-01 preview.111402; source 5622cbd00bd258b509353493eac5c57356708c4a");
            var legacy = new LegacyWorkdayScreen(previous);
            await legacy.ReadyAsync();
            var today = await legacy.DisplayedTodayAsync();
            var dates = HistoryTestData.SparseDates(today);
            var days = new List<HistoryTestDay>();

            for (var index = 0; index < dates.Length; index++)
            {
                await legacy.DateAsync(dates[index]);
                var entries = HistoryTestData.Entries(index);

                foreach (var entry in entries)
                {
                    await legacy.AddAsync(entry);
                }

                var ids = await legacy.IdentitiesAsync();
                Assert.Equal(entries.Length, ids.Distinct().Count());
                days.Add(new HistoryTestDay(dates[index], entries, ids, HistoryTestData.Total(index)));
            }

            await legacy.DateAsync(days[0].Date);
            await previous.Page.ScreenshotAsync(new() { Path = Path.Combine(previous.ArtifactDirectory, "upgrade-before.png"), FullPage = true });
            previous.RecordStage($"upgrade baseline seeded through UI; nonempty dates={days.Count}; closing all baseline-owned processes");
            await previous.DisposeAsync();
            Assert.False(Directory.Exists(previous.ProfileDirectory));

            var current = await scenario.LaunchAsync();
            current.RecordStage("upgrade new package launched with the same scenario-owned data after baseline cleanup");
            var editor = new WorkdayScreen(current);
            var history = new WorkEntryHistoryScreen(current);
            await editor.ReadyAsync();
            Assert.NotEqual(previous.ProfileDirectory, current.ProfileDirectory);
            Assert.Equal(today, await history.LocalTodayAsync());
            await history.DatesAsync(dates.Take(3).ToArray());

            foreach (var day in days.Take(3))
            {
                await history.DayAsync(day);
            }

            for (var count = 4; count <= dates.Length; count++)
            {
                await history.OlderAsync(dates.Take(count).ToArray());
                await history.DayAsync(days[count - 1]);
            }

            await history.ExhaustedAsync();
            await current.Page.ScreenshotAsync(new() { Path = Path.Combine(current.ArtifactDirectory, "upgrade-after.png"), FullPage = true });
            current.RecordStage($"upgrade baseline identities, creation order, fields and totals preserved; nonempty dates={days.Count}");

            // The upgraded store must still accept writes and discover history older than the baseline.
            var additionalDate = today.AddYears(-4);
            await editor.DateAsync(WorkEntryHistoryScreen.DateText(additionalDate));
            await editor.AddAsync("UPGRADE-NEW", "Written by new package", "Older than all baseline entries", "4m");
            var newIds = await editor.Rows.EvaluateAllAsync<string[]>("rows => rows.map(row => row.dataset.entryId)");
            var additional = new HistoryTestDay(additionalDate,
                [["UPGRADE-NEW", "Written by new package", "Older than all baseline entries", "4m"]], newIds, "4m");
            await history.OlderAsync(dates.Append(additionalDate).ToArray());
            await history.DayAsync(additional);
            await history.ExhaustedAsync();
            await current.DisposeAsync();

            var restarted = await scenario.LaunchAsync();
            await new WorkdayScreen(restarted).ReadyAsync();
            history = new WorkEntryHistoryScreen(restarted);
            await history.DatesAsync(dates.Take(3).ToArray());
            await history.DayAsync(days[0]);

            for (var count = 4; count <= dates.Length + 1; count++)
            {
                await history.OlderAsync(dates.Append(additionalDate).Take(count).ToArray());
            }

            await history.DayAsync(additional);
            await history.ExhaustedAsync();
            restarted.RecordStage("upgrade restart retained baseline identities and the additional new-package entry");
        });
    }

    private static string PreviousExecutable()
    {
        var configured = Environment.GetEnvironmentVariable("TIMESHEET_ASSISTANT_PREVIOUS_TEST_APP");

        if (string.IsNullOrWhiteSpace(configured) || !Path.IsPathFullyQualified(configured))
        {
            throw new InvalidOperationException("Set TIMESHEET_ASSISTANT_PREVIOUS_TEST_APP to the absolute accepted S-01 artifacts/publish-0.1.0-preview.111402/TimesheetAssistant.exe. Upgrade verification never skips a missing baseline.");
        }

        if (!File.Exists(configured))
        {
            throw new FileNotFoundException("The configured prior application package is missing.", configured);
        }

        return Path.GetFullPath(configured);
    }

    // S-01 has one global editor/table and predates the named selected-day editor region.
    private sealed class LegacyWorkdayScreen(DesktopAppFixture application)
    {
        private ILocator Field(string name) => application.Page.GetByLabel(name, new() { Exact = true });

        private ILocator Totals => application.Page.GetByRole(AriaRole.Region, new() { Name = "Daily totals", Exact = true });

        private ILocator Save => application.Page.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true });

        public async Task ReadyAsync()
        {
            await Expect(application.Page.GetByRole(AriaRole.Heading, new() { Name = "Workday", Exact = true })).ToBeVisibleAsync();
            await Expect(Totals).ToBeVisibleAsync();
            await Expect(Save).ToBeEnabledAsync();
        }

        public async Task<DateOnly> DisplayedTodayAsync()
        {
            var displayed = await Field("Work date").InputValueAsync();

            return DateOnly.ParseExact(displayed, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        public async Task DateAsync(DateOnly date)
        {
            var text = WorkEntryHistoryScreen.DateText(date);
            await using var originalInput = await Field("Work date").ElementHandleAsync()
                ?? throw new InvalidOperationException("The legacy work-date input is unavailable.");
            await Field("Work date").FillAsync(text);
            await Field("Work date").PressAsync("Tab");

            // S-01 replaces its keyed input when handling the change and again after loading.
            // A replacement that is enabled and displays the requested date acknowledges success.
            await using var acknowledged = await application.Page.WaitForFunctionAsync("element => !element.isConnected", originalInput,
                new() { Timeout = application.UiTimeoutMilliseconds });
            await Expect(Field("Work date")).ToBeEnabledAsync();
            await Expect(Field("Work date")).ToHaveValueAsync(text);
            await Expect(Save).ToBeEnabledAsync();
        }

        public async Task AddAsync(string[] entry)
        {
            await Field("Jira ID").FillAsync(entry[0]);
            await Field("Item title").FillAsync(entry[1]);
            await Field("Work description (optional)").FillAsync(entry[2]);
            await Field("Duration").FillAsync(entry[3]);
            await Save.ClickAsync();
            await Expect(application.Page.GetByRole(AriaRole.Status)).ToHaveTextAsync("Entry saved.");
            await Expect(Field("Item title")).ToHaveValueAsync("");
            await Expect(Field("Jira ID")).ToBeFocusedAsync();
        }

        public Task<string[]> IdentitiesAsync() => application.Page.Locator("tbody tr")
            .EvaluateAllAsync<string[]>("rows => rows.map(row => row.dataset.entryId)");
    }
}
