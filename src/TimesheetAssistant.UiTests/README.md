# TimesheetAssistant.UiTests

Requires Windows x64, the SDK/workload pinned by `global.json`, and Microsoft Edge
WebView2 Evergreen Runtime. Tests launch the real unpackaged Release EXE. No MAUI
project reference, web server or separate Chromium installation is needed.
Missing configuration or EXE fails rather than skips the suite.

From the repository root, publish to a fresh ignored directory using the release
script's deployment properties, without generating a release:

```powershell
$publish = Join-Path (Get-Location) ("artifacts/desktop-ui/local-" + [Guid]::NewGuid().ToString('N'))
dotnet publish src/TimesheetAssistant/TimesheetAssistant.csproj -f net10.0-windows10.0.19041.0 -c Release -p:RuntimeIdentifierOverride=win-x64 -p:WindowsPackageType=None -p:SelfContained=true -p:WindowsAppSDKSelfContained=true -o $publish
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
$env:TIMESHEET_ASSISTANT_TEST_APP = Join-Path $publish 'TimesheetAssistant.exe'
dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj --logger trx
Remove-Item Env:TIMESHEET_ASSISTANT_TEST_APP
```

An existing package's EXE can also be assigned directly. Only the launched child
receives the unique `WEBVIEW2_USER_DATA_FOLDER` and loopback CDP arguments. The
child-only `TIMESHEET_ASSISTANT_DATA_DIRECTORY` always overrides any inherited
value with the session's unique absolute `data` directory, separate from the
`webview2` profile. The configuration stage logs this path without record content.
The session root is checked as a test-owned GUID directory before recursive
cleanup; redirected session roots are rejected. Domain data is removed with the
session only after owned processes exit. A
private Windows job verifies the listener belongs to the launched process tree
before Playwright connects. The EXE is created suspended, assigned to that job,
then resumed so every WebView2 child starts inside the job. Assignment or resume
failure terminates the suspended process. Port collisions fail startup. Cleanup terminates only
that job and removes its unique temporary session directory after processes exit.
Independent WebView2 sessions are never selected by executable name. Test output
records the application PID and profile path for cleanup verification.

`DesktopTimeouts` centralizes startup (60 s), UI (15 s) and cleanup (15 s) budgets.
Controlled probes can instantiate the fixture with a custom options record.
Desktop tests do not share an instance in parallel. No global environment or
normal application debug setting changes.

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

The single desktop smoke test recognizes Home, opens a collapsed navigation menu,
follows Counter, checks `Current count: 0`, clicks `Click me` and checks
`Current count: 1`. Locators use roles and accessible names where provided. The
status has no accessible name, so its text is asserted. Playwright waits within
the UI budget without fixed UI sleeps. Run twice to check independent profiles.

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

To check failure diagnostics, temporarily change the final expected status to
`Current count: 2`, run the test, and restore the assertion in a `finally` block.
Expect a failed TRX, screenshot, trace, stage log and completed cleanup. Never
commit the deliberately failing assertion. From this project directory, inspect
a retained trace with:

```powershell
pwsh bin/Debug/net10.0-windows/playwright.ps1 show-trace <path-to-trace.zip>
```

Counter is temporary: S-01 replaces this scenario with work-entry tests, retaining
the fixture and adding scenario-owned domain storage with explicit restart/read checks.
The current single-launch fixture already isolates domain data, but does not retain
it across session cleanup; phase 4 must add that independent scenario lifetime.
A WebView2 profile is not domain-storage isolation. This smoke test proves no
work-entry persistence.

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
