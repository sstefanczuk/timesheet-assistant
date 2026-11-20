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

## Phase 3 verification — 2026-11-17

### Identified prior version

- S-01 package: `0.1.0-preview.111402`, source commit `5622cbd00bd258b509353493eac5c57356708c4a`, as recorded in its retained README and release notes.
- Prior EXE: `C:\Repos\10xDevs-project\artifacts\publish-0.1.0-preview.111402\TimesheetAssistant.exe`.
- Prior EXE SHA-256: `c4383084841b4b0cb39aa0180fbed5ff55a23bf07d7a0b26cc0df4a692ddd0da`.
- Prior application DLL SHA-256: `a5a19d8a8819a6c0b7d76406d35e7aba68df5e5bc10ddf726a334e6066ed56d7`.
- Prior ZIP SHA-256 from retained release notes: `7e2762a3cd1d8bd32604e2845401b6957257fe0b41bea1a85412b289462a3b65`.

This identified baseline is distinct from the history implementation. The completed upgrade scenario used only its own synthetic domain data and fresh per-launch profiles.

### New package and source provenance

- New package: `0.1.0-preview.111702`, application build `11172`.
- Product source commit: `d6df48335fbee5ced563254db32b47aaf5d9b324`. Phase 3 adds tests and documentation/package descriptions; product application sources were unchanged.
- Phase 3 source/test delta: ignored `artifacts/browse-entry-history-phase3-source.patch`, SHA-256 `2d7aae71dc0d40524f9140f51a10a11b22f525ad367d4ad2647725eaac0a2b00`. Captured after the legacy helper correction and before final result documentation.
- New EXE: `C:\Repos\10xDevs-project\artifacts\publish-0.1.0-preview.111702\TimesheetAssistant.exe`; SHA-256 `f605126f407470739e3aa8967d077c8239e5053173391347ed8f3549ea553c45`.
- New application DLL SHA-256: `b77133ce7ab77971b57ba504871516fba4198bc31df386d0c5a5ba354f4af966`.
- New ZIP: `artifacts/release-0.1.0-preview.111702/TimesheetAssistant-0.1.0-preview.111702-win-x64.zip`; SHA-256 `0a7c2d9b56edb0909048b966457aa3cf67b2ebddb1272eba80905b5782a14b71`.

### Executed commands and results

Run from the repository root. Build, pure tests, package publication and desktop execution were sequential. Commands used sandbox escalation for NuGet/desktop access. There were no blocked checks.

| Command/check | Exit | Result |
|---------------|------|--------|
| `dotnet build TimesheetAssistant.sln -c Release` | 0 | 0 warnings, 0 errors. |
| `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj -c Release` | 0 | 104 passed, 0 failed, 0 skipped before publication. |
| `scripts/Package-Windows.ps1 -Version '0.1.0-preview.111702' -Commit 'd6df48335fbee5ced563254db32b47aaf5d9b324' -BuildNumber 11172` | 0 | Fresh self-contained local package and ZIP; no hosted release. |
| Initial `dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj -c Release --logger 'trx;LogFileName=browse-entry-history-phase3.trx'` | 1 | 17 executed, 16 passed, 1 failed, 0 skipped. Legacy helper incorrectly expected newer readiness attributes absent from the actual S-01 package. |
| Corrected `dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj -c Release --logger 'trx;LogFileName=browse-entry-history-phase3-retry1.trx'` | 0 | 17 executed, 17 passed, 0 failed, 0 skipped/not executed. Includes history, upgrade and all existing S-01 cases. |
| `dotnet list TimesheetAssistant.sln package --vulnerable --include-transitive --source https://api.nuget.org/v3/index.json` | 0 | No vulnerable packages reported in any of the four projects. |
| `git diff --check` and `git diff --cached --check` | 0 | No whitespace errors. |

Both desktop runs set runner-only `TIMESHEET_ASSISTANT_TEST_APP` to the absolute new EXE and `TIMESHEET_ASSISTANT_PREVIOUS_TEST_APP` to the absolute prior EXE above; both original values were restored in finally. All launched applications used scenario-owned synthetic data, unique child profiles and child-only CDP configuration. Ordinary user storage was not accessed.

### Legacy helper adaptation

Inspection of the identified S-01 source confirmed that its totals region lacks `aria-busy` and `data-work-date`. The feature-local legacy helper now waits for the exact keyed date input node to be replaced, then for the enabled replacement to display the requested date and Save to become enabled. This acknowledges the old application's rendered handler/load completion without relying on browser input echo alone, fixed sleeps or action retries. New-package helpers retain their date/idle acknowledgements. No baseline binary or product code was changed to make the test pass.

### Desktop and upgrade evidence

- Successful full run: 2026-11-17 06:34:34–06:37:30 Europe/Warsaw; TRX `artifacts/desktop-ui/browse-entry-history-phase3-retry1.trx`.
- All 34 launch session logs contain `cleanup completed` and `scenario cleanup completed`. Each scenario disposed its owned process jobs and profiles before removing its validated synthetic data root. No failure was reported in the successful run.
- Short/empty cases cover 0, 1 and 2 earlier dates plus usable Today addition. The sparse scenario covers six dates including Saturday and two-/three-year-old entries, full fields, duplicate identities/order, totals, initial three-date selection, complete one-day paging, direct edit, cancelled discard, deleted boundary retention and restart.
- The failure scenario acquires a test-owned `FileShare.None` database handle after successful paging, requires an actual visible read failure/retry, checks unchanged full contents and boundary, releases the handle in finally and verifies retry plus continued paging. It does not assume an ordinary reserved writer lock prevents reads.
- Upgrade demonstration creates six dates/eight entries through the real old package UI, records their IDs, closes all baseline-owned processes, then verifies exact IDs, fields, creation order and totals across all six days in the new history. It adds a seventh older date through the new package and verifies persistence after another restart. Missing baseline configuration fails this dedicated test rather than skipping it.
- Baseline demonstration image: `artifacts/desktop-ui/d9cb838dd2984bb3b26319de55ea9814/upgrade-before.png`.
- New-package demonstration image: `artifacts/desktop-ui/757f8f711548498297549650cd81eb51/upgrade-after.png`.
- The matching session logs record the identified baseline/source, completed baseline cleanup, preserved group count and successful new-package restart. Screenshots contain synthetic test entries only and are retained for author review.

### Deliberate-break control

After staging the intended files, temporarily changed only the limited SQLite date query from `ORDER BY work_date DESC LIMIT $limit` to `ORDER BY work_date ASC LIMIT $limit` in the worktree. Published a separate ignored negative package with:

```powershell
dotnet publish src/TimesheetAssistant/TimesheetAssistant.csproj -f net10.0-windows10.0.19041.0 -c Release -p:RuntimeIdentifierOverride=win-x64 -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true -p:SelfContained=true -p:Version=0.1.0-preview.111702 -p:ApplicationDisplayVersion=0.1.0 -p:ApplicationVersion=11172 -o artifacts/publish-history-order-negative-111702
```

Publication exited 0. With the runner's app variable temporarily pointing to that negative EXE, ran:

```powershell
dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj -c Release --no-build --filter FullyQualifiedName~Sparse_history_preserves_exact_entries_paging_mutations_discard_and_restart --logger 'trx;LogFileName=browse-entry-history-phase3-order-negative.trx'
```

Expected exit 1: 1 executed, 1 failed, 0 skipped. The assertion expected newest date `2026-11-16` but received `2023-11-17`, establishing that the test detects broken real SQLite date selection. Both launch sessions logged process and scenario cleanup completion. Source was unconditionally restored from staging with `git checkout -- src/TimesheetAssistant/Features/Workdays/SqliteWorkEntryStore.cs` in finally, and the original runner environment was restored. The negative package was never used for acceptance and remains separate from the unchanged final package.

### Final author acceptance

After restoring the deliberate mutation, re-ran `dotnet build TimesheetAssistant.sln -c Release` (exit 0, 0 warnings/errors), followed by `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj -c Release --no-build` (exit 0, 104 passed, 0 failed/skipped). Normal build outputs are restored. The final package used by the successful 17-test desktop run remained unchanged throughout the negative control.

On 2026-11-17 the author confirmed “Potwierdzam końcowy odbiór” in response to the explicit acceptance question for `0.1.0-preview.111702`: browsing/loading older history, direct editing, adding from scratch, retained committed entries after reopening, and the isolated `111402` → `111702` demonstration with its before/after images. Progress 3.4 is complete. Local acceptance of this change is complete. No desktop CI, clean-device or full-MVP acceptance is claimed; F-01 remains deferred and untouched.
