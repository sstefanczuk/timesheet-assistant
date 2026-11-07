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
before Playwright connects. Port collisions fail startup. Cleanup terminates only
that job and removes its unique temporary session directory after processes exit.
Independent WebView2 sessions are never selected by executable name. Test output
records the application PID and profile path for cleanup verification.

`DesktopTimeouts` centralizes startup (60 s), UI (15 s) and cleanup (15 s) budgets.
Controlled probes can instantiate the fixture with a custom options record.
Desktop tests do not share an instance in parallel. No global environment or
normal application debug setting changes.

Phase 1 includes a real-process connection probe that recognizes Home. Counter
interaction and failure screenshot/trace capture belong to phase 2; CI execution
belongs to phase 3. Counter will be temporary: S-01 replaces it with work-entry
tests including isolated storage and explicit restart/read verification. A
WebView2 profile is not domain-storage isolation. This probe proves no persistence.

Version evidence checked on 2026-11-07: current stable
[Microsoft.Playwright 1.64.1](https://www.nuget.org/packages/Microsoft.Playwright/1.64.1).
Connection follows the [official Playwright .NET WebView2 guide](https://playwright.dev/dotnet/docs/webview2).
