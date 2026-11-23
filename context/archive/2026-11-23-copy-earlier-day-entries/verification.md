# S-08 Phase 2 Verification

## Package provenance

- **Version**: `0.1.0-preview.23002` (Windows x64, unpackaged Release app)
- **Source commit**: `4272ba045ee3b34bb3c3b9841817b5d6f0e006c3`
- **Build worktree**: contained the Phase 2 UI test additions; production app source was unchanged from the source commit.
- **Packaging note**: updated the generated package/release text to describe individual entry duplication; the earlier preview package was superseded before manual acceptance.
- **Executable**: `artifacts/publish-0.1.0-preview.23002/TimesheetAssistant.exe`
- **Release archive**: `artifacts/release-0.1.0-preview.23002/TimesheetAssistant-0.1.0-preview.23002-win-x64.zip`
- **Archive SHA-256**: `db947a62a4361b283109cc43dffe26f41c5c4bbb2c90f437e74b1eb4e7b4f215`
- **Upgrade baseline**: `artifacts/publish-0.1.0-preview.111402/TimesheetAssistant.exe` (accepted S-01 baseline from `5622cbd00bd258b509353493eac5c57356708c4a`).

## Automated verification

Commands ran from the repository root on Windows with .NET SDK `10.0.403` and the configured MAUI workload:

| Command | Result |
| --- | --- |
| `dotnet build TimesheetAssistant.sln -c Release` | PASS — 0 warnings, 0 errors. |
| `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj -c Release --no-build` | PASS — 110 passed, 0 failed, 0 skipped. |
| `./scripts/Package-Windows.ps1 -Version '0.1.0-preview.23002' -BuildNumber 23002 -Commit 4272ba045ee3b34bb3c3b9841817b5d6f0e006c3` | PASS — published EXE and release ZIP with SHA-256 above. |
| `dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj -c Release --no-build --filter 'FullyQualifiedName~WorkEntryDuplicationTests' --logger trx` | PASS — 3 passed, 0 failed, 0 skipped; isolated per-scenario data and diagnostics. |
| `dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj -c Release --no-build --logger trx` with `TIMESHEET_ASSISTANT_TEST_APP` set to the fresh EXE and `TIMESHEET_ASSISTANT_PREVIOUS_TEST_APP` set to the accepted baseline | PASS — 22 passed, 0 failed, 0 skipped. TRX: `artifacts/desktop-ui/sstefanczuk_SSTEFANCZUK-HPR_2026-11-23_17_44_57_net10.0.trx` (SHA-256 `18e91992d866396d322d971ec442eb4bc571da7b8edb91359de75d723d8d5ee5`). |

The first two full-suite attempts each reported one failure because the new test asserted focus on the wrong input. The dialog focuses its first enabled input, `Work date`; the test was corrected to assert that existing behavior. A later full-suite attempt while this package was open reported two failures: the existing app-copy scenario encountered the package's locked WebView2 profile, and an existing history-storage scenario raised an `IndexOutOfRangeException` while checking seeded identity arrays. The package process was closed; the history scenario passed in isolation (1/1), all duplication tests passed (3/3), and the clean final full suite passed (22/22). No production fix was required.

The desktop suite included the old-to-new upgrade scenario. Its per-session diagnostics recorded completed cleanup for the owned processes, profiles and scenario data. No test scenarios were skipped.

## Manual author acceptance

- **Status**: Accepted by the author on 2026-11-23.
- **Package inspected**: `artifacts/publish-0.1.0-preview.23002/TimesheetAssistant.exe`.
- **Isolated manual data**: `artifacts/s08-manual-23002-data`; WebView2 profile: `artifacts/s08-manual-23002-webview`.
- **Author confirmation**: The author confirmed the manual acceptance checklist passed, including keyboard access, the shared Add entry form and suggested date, save/cancel behavior, independent copies and light/dark themes.
- **Desktop observation**: Windows Computer Use activated the package window and captured the Timesheet page with the expected entry list. The detailed keyboard and theme checks are based on the author's confirmation.
