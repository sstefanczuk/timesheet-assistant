# Browse Entry History Verification

## Phase 2 local verification — 2026-11-16

Automated phase 2 checks passed. Author acceptance was confirmed on 2026-11-17 against corrected package `0.1.0-preview.111701`; Progress 2.3 is complete. Phase 3 history, upgrade and restart scenarios have not been implemented or accepted.

### Source and package provenance

- Base commit: `dcd3a84f496afd4950151a2b5ff7297f1cf2e4df` plus the staged phase 2 implementation, not the base commit alone. The package script's Commit field identifies this base.
- Source delta: ignored `artifacts/browse-entry-history-phase2-source.patch`, SHA-256 `7f9a503ed327288e029605e3a641a9f4433e4cd6ca570e0c3f89737627e30986`. Captured before publishing; later verification documentation does not change product sources.
- Version: `0.1.0-preview.111601`; application build number: `11161`.
- Published EXE: `C:\Repos\10xDevs-project\artifacts\publish-0.1.0-preview.111601\TimesheetAssistant.exe`.
- EXE SHA-256: `419e87b5f440e8cf152d402894732ff9187099ccbe7398f296b27ce858107255`.
- Application DLL SHA-256: `63af94af8b1a123b6a90fe01e381cfe02618c887735d4b6a37acbd0fc61b713d`.
- ZIP: `artifacts/release-0.1.0-preview.111601/TimesheetAssistant-0.1.0-preview.111601-win-x64.zip`.
- ZIP SHA-256: `5c618fe1ac9b17ca7173d1a38565818c0efa9e2fb77bb9a29920fb75ba119ded`.

### Commands and results

Executed sequentially from the repository root, with sandbox escalation for required NuGet configuration and desktop launch access:

| Command | Exit | Result |
|---------|------|--------|
| `dotnet build TimesheetAssistant.sln -c Release` | 0 | 0 warnings, 0 errors; includes existing desktop helper compilation. |
| `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj -c Release` | 0 | 104 passed, 0 failed, 0 skipped. |
| `git diff --check` | 0 | No whitespace errors. |
| Negative control: disable required identity validation in `WorkdayService`, then `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj -c Release --no-restore --filter FullyQualifiedName~Unavailable_destination_entry_preserves_the_previous_editor_snapshot` | 1 expected | Missing-identity case failed its success assertion; storage-failure case passed. Source restored unconditionally from staging in finally. |
| `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj -c Release --no-restore` after restoration | 0 | 104 passed, 0 failed, 0 skipped. |
| `scripts/Package-Windows.ps1 -Version '0.1.0-preview.111601' -Commit 'dcd3a84f496afd4950151a2b5ff7297f1cf2e4df' -BuildNumber 11161` | 0 | Fresh local self-contained package and ZIP created. No hosted release. |
| `dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj -c Release --no-build --logger 'trx;LogFileName=browse-entry-history-phase2.trx'` | 0 | 11 executed, 11 passed, 0 failed, 0 skipped/not executed. |

The desktop runner received `TIMESHEET_ASSISTANT_TEST_APP` pointing to the published EXE above, with the caller's value restored in finally. Child-only data/profile/CDP isolation remained unchanged. No ordinary user database was accessed by automated scenarios.

### Desktop diagnostics

- TRX: `artifacts/desktop-ui/browse-entry-history-phase2.trx`.
- Run: 2026-11-16 19:37:56–19:39:44 Europe/Warsaw.
- All 21 launch session logs from this run contain both `cleanup completed` and `scenario cleanup completed`; no test failures were reported. Session logs remain under ignored `artifacts/desktop-ui/<session-id>/desktop.log`.
- This is the existing S-01 regression suite with selectors scoped to the selected-day editor. Its success does not replace the new phase 3 history or old-package-to-new-package compatibility scenarios.

### Author acceptance

The original checklist for `0.1.0-preview.111601` was:

1. Confirm Today is separate and earlier groups show complete saved fields and daily totals.
2. Load older days and directly Edit an entry from another day; verify the correct fields populate and save.
3. Use Add entry for today to open a blank form for today.
4. Enter a draft, request historical Edit, cancel discard and verify the date, input and loaded history remain intact.
5. Verify keyboard navigation and confirmation controls.

The author initially reported correct functionality with spurious warning text, documented below. Final phase 2 acceptance applies to corrected package `0.1.0-preview.111701`. No desktop CI, clean-device, full history-suite or upgrade acceptance is claimed. No automated check is blocked.

## Phase 2 author feedback and correction — 2026-11-17

The author reported that functionality looked correct, but both Today and Earlier days displayed the literal `_historyError`. Package `0.1.0-preview.111601` is superseded for manual acceptance by `0.1.0-preview.111701`.

### Correction and regression evidence

Both component calls passed a string literal via `Error="_historyError"`. Changed them to `Error="@_historyError"` so a successful read passes null and renders no warning; an actual storage failure still passes its message. Extended the existing desktop `WorkdayScreen.ReadyAsync` to assert no alerts in Today and Earlier days after successful initialization.

| Command/check | Exit | Result |
|---------------|------|--------|
| `dotnet build TimesheetAssistant.sln -c Release` | 0 | 0 warnings, 0 errors. |
| `dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj -c Release --filter FullyQualifiedName~Date_selection_waits_until_the_application_has_loaded_the_requested_day --logger 'trx;LogFileName=browse-entry-history-binding-negative.trx'` against old package `111601` | 1 expected | 1 executed, 1 failed, 0 skipped. New readiness assertion detected one Today alert instead of zero. This reproduces the reported defect and proves regression protection. |
| `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj -c Release` | 0 | 104 passed, 0 failed, 0 skipped, before replacement packaging. |
| `scripts/Package-Windows.ps1 -Version '0.1.0-preview.111701' -Commit 'dcd3a84f496afd4950151a2b5ff7297f1cf2e4df' -BuildNumber 11171` | 0 | Fresh corrected local package created. |
| `dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj -c Release --no-build --logger 'trx;LogFileName=browse-entry-history-phase2-binding-fixed.trx'` against corrected package `111701` | 0 | 11 executed, 11 passed, 0 failed, 0 skipped/not executed. Both alert-free readiness checks pass. |
| `git diff --check` | 0 | No whitespace errors. |

Desktop environment overrides were restored in finally. Corrected-package TRX: `artifacts/desktop-ui/browse-entry-history-phase2-binding-fixed.trx`; run 2026-11-17 06:07:48–06:09:35 Europe/Warsaw. All 21 session logs contain process and scenario cleanup completion. Negative-control TRX and failure diagnostics remain under ignored `artifacts/desktop-ui/`.

### Replacement package provenance

- Base commit remains `dcd3a84f496afd4950151a2b5ff7297f1cf2e4df` plus staged phase 2 sources and this binding correction.
- Source delta: `artifacts/browse-entry-history-phase2-binding-fix-source.patch`, SHA-256 `2a000f881dfb8943661f9d04d6f37170dcdcee143339876ab5b1c13fda23f869`.
- EXE: `C:\Repos\10xDevs-project\artifacts\publish-0.1.0-preview.111701\TimesheetAssistant.exe`; SHA-256 `d31217b7809a1fb45ed5c42fae3ebbf0037c9aacfaa84400d1476c2d3b03b5c8`.
- Application DLL SHA-256: `3a9955e9559226edb47bfb9620ec62062258170c97d64ecb546ba09e8242d8f2`.
- ZIP: `artifacts/release-0.1.0-preview.111701/TimesheetAssistant-0.1.0-preview.111701-win-x64.zip`; SHA-256 `3d3a2ac603b3879501c83c8b273243249e189f2f7c77aaeaf85b1872906dd3d9`.

On 2026-11-17 the author confirmed: “Tak — komunikaty zniknęły, potwierdzam odbiór”, answering the explicit corrected-package phase 2 acceptance question. Together with the earlier confirmation that functionality looked correct, this closes the phase 2 manual gate for Today, history and totals, older paging, direct cross-day editing, Add entry for today, keyboard operation and cancelled draft discard. Progress 2.3 is complete. Phase 3 acceptance remains pending.
