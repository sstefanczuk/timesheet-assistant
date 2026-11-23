# TimesheetAssistant.UiTests

Requires Windows x64, the SDK/workload pinned by `global.json`, and Microsoft Edge
WebView2 Evergreen Runtime. Tests launch the real unpackaged Release EXE. No MAUI
project reference, web server or separate Chromium installation is needed.
Missing configuration or EXE fails rather than skips the suite.

From the repository root, build Release, run pure UnitTests and package sequentially.
Do not overlap a build with publishing. Use fresh version/build numbers and retain
the accepted S-01 `artifacts/publish-0.1.0-preview.111402/TimesheetAssistant.exe`
(source `5622cbd00bd258b509353493eac5c57356708c4a`) for compatibility verification.
Set both absolute EXE paths only for the desktop runner and restore the caller's
environment afterward. Replace the new-version/build placeholders before running:

```powershell
dotnet build TimesheetAssistant.sln -c Release
if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj -c Release --no-build
if ($LASTEXITCODE -ne 0) { throw 'Pure verification failed.' }
./scripts/Package-Windows.ps1 -Version '0.1.0-preview.<new-number>' -BuildNumber <new-build-number> -Commit (git rev-parse HEAD)

$previous = $env:TIMESHEET_ASSISTANT_TEST_APP
$previousBaseline = $env:TIMESHEET_ASSISTANT_PREVIOUS_TEST_APP
try {
    $env:TIMESHEET_ASSISTANT_TEST_APP = Join-Path (Get-Location) 'artifacts/publish-<version>/TimesheetAssistant.exe'
    $env:TIMESHEET_ASSISTANT_PREVIOUS_TEST_APP = Join-Path (Get-Location) 'artifacts/publish-0.1.0-preview.111402/TimesheetAssistant.exe'
    dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj -c Release --logger trx
    if ($LASTEXITCODE -ne 0) { throw 'Desktop verification failed.' }
}
finally {
    $env:TIMESHEET_ASSISTANT_TEST_APP = $previous
    $env:TIMESHEET_ASSISTANT_PREVIOUS_TEST_APP = $previousBaseline
}
```

Each test creates its own `DesktopScenario`, with a unique GUID root beneath
`%TEMP%/TimesheetAssistant.UiScenarios`. Its domain `data` directory survives all
launches in that scenario. Every launch has a separate fixture and fresh WebView2
profile under `%TEMP%/TimesheetAssistant.UiTests`; disposing a launch stops only
its owned process job and removes only that session. The scenario removes its
validated GUID root only after all launches have cleaned up successfully. Cleanup
rejects redirected directories and retains data when process cleanup fails.
Original failures are preserved alongside cleanup failures.

Only the launched child receives `TIMESHEET_ASSISTANT_DATA_DIRECTORY`,
`WEBVIEW2_USER_DATA_FOLDER` and loopback CDP arguments. Any caller data override is
replaced. The invalid-configuration scenario deliberately supplies a relative
child override. Logs identify domain/profile paths and launch stages without
record content; screenshots and traces use synthetic test records only.
A private Windows job verifies listener ownership. The EXE starts suspended,
is assigned to that job, then resumes; no global debug settings change.
`DesktopTimeouts` bounds startup (60 s), UI (15 s) and cleanup (15 s).
The Desktop collection runs scenarios sequentially, without shared class fixtures.

CDP readiness polls only the owned loopback `/json/version/` endpoint with an
HTTP client that disables proxies and redirects. Each request has at most two
seconds within the same 60-second startup budget. Transient HTTP transport errors
and request timeouts retry discovery; malformed responses or foreign listeners
fail immediately. Ownership is checked before requests, after discovery and before
the single websocket attachment. The returned websocket URL must use `ws`, the
reserved port and IPv4 loopback (`localhost` is normalized to `127.0.0.1`). Only
changed readiness reasons are logged. UI actions and assertions are not retried.

Elevated test runners launch only the application with a non-elevated,
medium-integrity token: the linked filtered token when available, otherwise a
LUA restricted token with medium integrity. WebView2 ignores environment
overrides in elevated hosts, so both the launch token and actual suspended child
token are checked before resume. Failure to create or validate that token fails
startup and cleans up the owned child. Standard local launches retain their
existing process-creation path. No machine policy or registry settings change.

The Workdays tests replace the historical Counter smoke. They assert exact rows,
identity/order, duplicate tickets, totals, validation and cancelled discard/delete;
three launches retain committed adds, edits and deletions on two dates including
a weekend. Other scenarios cover independent stores, second-process exclusion,
reacquisition, an entire copied app directory, invalid configuration, corrupt and
future databases, an unreadable file followed by Retry, first-start lease failure
followed by Retry, and failed update under a test-owned SQLite transaction.
Only failure fixtures use direct database preparation; ordinary entries are saved
through the visible UI. Native SQLite is a UiTests dependency for bounded failure
setup, never a UnitTests dependency.

`WorkEntryHistoryTests` cover empty and one/two-day history, Today/add-from-scratch,
six sparse dates including Saturday and multi-year-old work, full field values,
duplicate-ticket identities/creation order, daily totals, initial three dates,
one complete day per append, exhaustion, cross-day editing, cancelled draft discard,
last-entry deletion with the retained inclusive boundary, correct further paging
and restart with committed changes and the initial window. A test-owned
`FileShare.None` database handle induces an actual read failure after paging;
the scenario requires an error/retry and unchanged complete groups/cursor,
releases the handle in finally, then verifies successful retry. A normal reserved
SQLite write transaction would not establish that reads were blocked.

`WorkEntryDuplicationTests` cover duplicates from initially visible and paged older
entries, copied field values, weekday suggestions across weekends and exact or
excess daily totals, cancellation, a same-date duplicate with the same Jira ID,
independent identity and contents after restart, and a storage read failure that
must not open the form or create a partial entry. Keyboard and light/dark theme
acceptance remains a manual check on the published package.

`WorkEntryHistoryUpgradeTests` launch the configured prior EXE and create all
baseline entries through its legacy UI, capture their exact IDs/order, completely
dispose its owned process job, then launch the new package against the same
scenario-owned data. They verify every complete history group, further paging,
new writes and another restart. A missing, relative or nonexistent
`TIMESHEET_ASSISTANT_PREVIOUS_TEST_APP` fails this dedicated test; it never skips
compatibility verification. The variable is read by the runner only and does not
select an ordinary application data directory. The existing copied-application
scenario remains the separate folder-independence check; copying the new binaries
does not count as old-to-new compatibility.

Upgrade sessions retain synthetic `upgrade-before.png` and `upgrade-after.png`
screenshots in their normal per-launch artifact directories for author review.
Logs identify the baseline version/source and successful counts. Record the new
package/source provenance, actual TRX counts, cleanup results and author acceptance
in `context/changes/browse-entry-history/verification.md`. Successful local runs
do not establish desktop CI or clean-device acceptance.

Date selection waits for the rendered snapshot's `data-work-date` and idle
`aria-busy` state, not just the browser input's echoed value. Successful saves
wait for the cleared editor and its requested focus before the next interaction.
`WorkdayInputTests` holds a browser date-change event to prove that the helper
cannot report readiness before application acknowledgement, then releases it in
finally. Its bounded negative completion assertion is not an action retry or a
fixed UI sleep. A second scenario enters twelve records rapidly and verifies
every saved field again after restart. Both scenarios use test-owned data only.

`desktop.runsettings` routes `--logger trx` results to the ignored repository
`artifacts/desktop-ui/` directory. Each fixture creates a unique subdirectory
there containing `desktop.log`: timestamped startup, profile, PID, CDP endpoint,
interaction stage and cleanup evidence. Logs remain on success and failure.
Process lifecycle diagnostics read IDs only from the private Windows job, retain
verified handles against PID reuse, and log basename/PID/exit-code transitions.
They distinguish natural exits from test-driven cleanup and never enumerate
unrelated processes or record command lines. Diagnostic errors preserve the
original test failure.
The launched child also receives Chromium's `--enable-logging` and an explicit
`--log-file` inside that session's artifact directory. `webview2.log`, when the
runtime produces it, helps diagnose browser startup/exit before CDP attachment.
Verbose logging, sandbox/GPU overrides and global browser settings are not enabled.
On failure, `failure.png` and `trace.zip` are saved if the page and tracing are
available. Failures before connection cannot produce page artifacts. Diagnostic
write failures do not replace the original exception. Before terminating the app,
cleanup saves `cleanup.png` and the trace if no test failure was already captured.
These temporary diagnostics are deleted only after the entire cleanup succeeds;
a cleanup failure retains them and logs its exception. Test-failure artifacts are
retained even when cleanup succeeds.

For a deliberate-break check, stage the intended files, temporarily break the
production ordering/query or mutation behavior, publish to a separate ignored
folder and run the targeted persistence test. Restore production source from
staging in a finally block. Require a failing assertion, original failure evidence
and successful cleanup; never commit the mutation. Local acceptance also requires
the author's manual save/reopen/edit/delete/alternate-folder confirmation for the
identified package. Test success alone does not satisfy that manual criterion.

## Deferred CI release gate

As of 2026-11-11, the desktop CI gate is deferred. Its exact workflow candidate
is preserved in local branch `wip/f-01-desktop-ui-gate-20261111`, commit `8980a91`.
Main retains its existing build/package/release workflow without this gate.
Keep running this suite locally against the published EXE. A green main build
does not establish desktop UI acceptance.

The candidate `Windows preview` workflow on `windows-2025` restores and builds
the solution, runs UnitTests, audits dependencies and packages the application. It checks the
machine/user WebView2 Evergreen installation directories before running this
project. A missing runtime fails with `runtime.log`; it never skips the suite.
After packaging, the workflow runs:

```powershell
$env:TIMESHEET_ASSISTANT_TEST_APP = Join-Path $env:GITHUB_WORKSPACE "artifacts/publish-$env:PREVIEW_VERSION/TimesheetAssistant.exe"
dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj -c Release --no-build --logger 'trx;LogFileName=desktop-ui.trx' --results-directory artifacts/desktop-ui
```

`PREVIEW_VERSION` comes from the workflow's packaging step. For a local package,
assign its absolute EXE path directly instead. The candidate requires a zero
runner exit code (including fixture cleanup), an existing completed TRX, at least one executed
test and all executed tests passing. Release-input upload follows this gate;
the release job depends on the successful build job.

In the candidate, the separate `desktop-ui-diagnostics` artifact uploads
`artifacts/desktop-ui/` even on failure and expires after three days. It contains `runtime.log`,
`desktop-ui.trx` when the runner produced it, and the per-session diagnostics
described above. Failure before a page connection cannot provide a screenshot
or trace. Failure before these steps may leave no diagnostics to upload.

CI acceptance remains pending. After two correction rounds, the latest
[normal attempt](https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42478901525)
observed owned WebView2 processes exit with `0x80000003` before cleanup, while the
application remained alive. It never connected to a page. It retained TRX,
runtime and own-process lifecycle logs, completed cleanup and blocked release.
The matching [negative attempt](https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42478904280)
is recorded with the final investigation outcome in `context/deployment/deploy-plan.md`.
No page connection means no page screenshot/trace was available. An unreachable
HTTP proxy and standard-token profile writes were checked separately; neither is
established as the cause. The exact failing native module/check remains unknown.
F-01 remains incomplete until a real passing run and a controlled assertion-failure
run establish the required outcomes. Local success does not establish CI success
or clean-device compatibility.

The subsequent [native diagnosis report](../../context/changes/diagnose-webview2-native-crash/diagnosis.md)
is inconclusive: no failing native module or stack was established. The final
active analysis returned `budget-exhausted` without identifying the exact limit;
cleanup succeeded. Deferral does not authorize another diagnostic run.

Version evidence checked on 2026-11-07: current stable
[Microsoft.Playwright 1.64.1](https://www.nuget.org/packages/Microsoft.Playwright/1.64.1).
Connection follows the [official Playwright .NET WebView2 guide](https://playwright.dev/dotnet/docs/webview2).
