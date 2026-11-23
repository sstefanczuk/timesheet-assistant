# Verification

## Phase 1 — 2026-11-21

- Windows local Release build: `dotnet build TimesheetAssistant.sln -c Release --no-restore` — passed with no warnings or errors.
- Unit tests: `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj -c Release --no-build --no-restore` — 104 passed.
- Desktop tests: `dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj -c Release --no-restore --logger trx` with the new `artifacts/phase1-header-review/TimesheetAssistant.exe` and the accepted S-01 baseline — 16 passed, one failed. The failed copy-application scenario encountered a locked WebView2 Cookies file in the manually launched preview directory. TRX: `artifacts/desktop-ui/sstefanczuk_SSTEFANCZUK-HPR_2026-11-21_12_29_24_net10.0.trx`.
- Retried the failed `Independent_stores_exclusion_reacquisition_and_copied_application_preserve_records` scenario against fresh `artifacts/phase1-automated-review/TimesheetAssistant.exe` with `--filter` — one passed. TRX: `artifacts/desktop-ui/sstefanczuk_SSTEFANCZUK-HPR_2026-11-21_12_33_33_net10.0.trx`. The 17 scenarios therefore passed across two runs, not in one clean full-suite run.
- Negative check: temporarily changed the current page heading back to `Workday`, published separately and ran `Empty_and_short_history_keep_today_addition_usable`. All three cases failed on the expected `Timesheet` heading assertion; the production source was restored from the staged version. TRX: `artifacts/desktop-ui/sstefanczuk_SSTEFANCZUK-HPR_2026-11-21_12_34_29_net10.0.trx`.
- The author confirmed the phase 1 manual checks in conversation on 2026-11-21, including both themes, navigation and heading, typography, contrast and focus, window sizes and theme persistence after restart.

These local results do not establish desktop CI acceptance or clean-device compatibility. Full redesign verification remains in phase 3.

## Phase 2 — 2026-11-21 (automated verification)

- `dotnet build TimesheetAssistant.sln -c Release --no-restore` — passed with no warnings or errors.
- `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj -c Release --no-build --no-restore` — 104 passed.
- The first full desktop run against `artifacts/phase2-suite-test/TimesheetAssistant.exe` passed 18/19. The recording test found simultaneous loading and stale success status elements; the UI was corrected.
- Full desktop run against `artifacts/phase2-suite-retest/TimesheetAssistant.exe`, including the accepted S-01 baseline: 19 passed, zero failed or skipped. TRX: `artifacts/desktop-ui/sstefanczuk_SSTEFANCZUK-HPR_2026-11-21_16_50_35_net10.0.trx`.
- After the full run, date formatting was made culture-invariant and the draft-navigation test was extended. Final `artifacts/phase2-final-review/TimesheetAssistant.exe` passed both `EntryExperienceTests`, including validation, paging, delete, draft/navigation, edit discard and restart. TRX: `artifacts/desktop-ui/sstefanczuk_SSTEFANCZUK-HPR_2026-11-21_16_55_58_net10.0.trx`. The final package also passed the full desktop suite: 19 passed, zero failed or skipped. TRX: `artifacts/desktop-ui/sstefanczuk_SSTEFANCZUK-HPR_2026-11-21_16_59_32_net10.0.trx`.
- Keyboard coverage confirmed focus stays in the dialog while tabbing, Escape closes it, and focus returns to the initiating control. A deliberate draft-clearing mutation made the draft preservation scenario fail; the source was restored from staging. TRX: `artifacts/desktop-ui/sstefanczuk_SSTEFANCZUK-HPR_2026-11-21_16_57_44_net10.0.trx`.
- Author manual verification of phase 2 is pending. Phase 3 remains responsible for the complete acceptance and additional coverage.

## Phase 2 UI refinements — 2026-11-22

- Release publish to `artifacts/phase2-polish-0.1.0-preview.22003/` — passed.
- Unit tests: `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj --no-build` — 104 passed.
- Focused desktop verification against the fresh package: `EntryExperienceTests` and `WorkdayRecordingTests` — 4 passed, zero failed or skipped. This covers the global status strip and save/update/delete notices, no visible table caption, fixed single-line Date/Jira ID columns with long-ID truncation and title, and pending/success/warning icons. TRX: `artifacts/desktop-ui/sstefanczuk_SSTEFANCZUK-HPR_2026-11-22_07_05_27_net10.0.trx`.
- A full desktop run was started but did not finish within the observed run window; it was stopped after the test runner made no further progress. Full-suite verification remains outstanding.
- Visual review of the newly launched package and author manual verification are pending.

## Final acceptance — 2026-11-23

- Final label change: `Refresh history` is now `Refresh` in both Workdays views; Playbook and UI selectors were updated.
- Release build: `dotnet build TimesheetAssistant.sln -c Release --no-restore` — passed, zero warnings and errors.
- Unit tests: `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj -c Release --no-build --no-restore` — 104 passed.
- Published package: `artifacts/publish-0.1.0-preview.22004/TimesheetAssistant.exe`.
- Full desktop suite against the published package and accepted S-01 baseline — 19 passed, zero failed or skipped. TRX: `artifacts/desktop-ui/sstefanczuk_SSTEFANCZUK-HPR_2026-11-23_12_44_27_net10.0.trx`. Earlier attempts exposed and then corrected outdated icon/panel expectations; the final run passed.
- Author approved the final UI state and confirmed proceeding on 2026-11-23. Phase 2 and final acceptance are complete.
