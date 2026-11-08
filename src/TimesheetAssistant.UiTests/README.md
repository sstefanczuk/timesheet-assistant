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
receives the unique `WEBVIEW2_USER_DATA_FOLDER` and loopback CDP arguments. A
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

The single desktop smoke test recognizes Home, opens a collapsed navigation menu,
follows Counter, checks `Current count: 0`, clicks `Click me` and checks
`Current count: 1`. Locators use roles and accessible names where provided. The
status has no accessible name, so its text is asserted. Playwright waits within
the UI budget without fixed UI sleeps. Run twice to check independent profiles.

`desktop.runsettings` routes `--logger trx` results to the ignored repository
`artifacts/desktop-ui/` directory. Each fixture creates a unique subdirectory
there containing `desktop.log`: timestamped startup, profile, PID, CDP endpoint,
interaction stage and cleanup evidence. Logs remain on success and failure.
On failure, `failure.png` and `trace.zip` are saved if the page and tracing are
available. Failures before connection cannot produce page artifacts. Diagnostic
write failures do not replace the original exception. Successful traces are discarded.

To check failure diagnostics, temporarily change the final expected status to
`Current count: 2`, run the test, and restore the assertion in a `finally` block.
Expect a failed TRX, screenshot, trace, stage log and completed cleanup. Never
commit the deliberately failing assertion. From this project directory, inspect
a retained trace with:

```powershell
pwsh bin/Debug/net10.0-windows/playwright.ps1 show-trace <path-to-trace.zip>
```

Counter is temporary: S-01 replaces this scenario with work-entry tests, retaining
the fixture and adding isolated domain storage and explicit restart/read checks.
A WebView2 profile is not domain-storage isolation. This smoke test proves no
work-entry persistence. CI execution belongs to phase 3.

Version evidence checked on 2026-11-07: current stable
[Microsoft.Playwright 1.64.1](https://www.nuget.org/packages/Microsoft.Playwright/1.64.1).
Connection follows the [official Playwright .NET WebView2 guide](https://playwright.dev/dotnet/docs/webview2).
