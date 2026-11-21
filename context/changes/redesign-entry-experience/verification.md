# Verification

## Phase 1 — 2026-11-21

- Windows local Release build: `dotnet build TimesheetAssistant.sln -c Release --no-restore` — passed with no warnings or errors.
- Unit tests: `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj -c Release --no-build --no-restore` — 104 passed.
- Desktop tests: `dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj -c Release --no-restore --logger trx` with the new `artifacts/phase1-header-review/TimesheetAssistant.exe` and the accepted S-01 baseline — 16 passed, one failed. The failed copy-application scenario encountered a locked WebView2 Cookies file in the manually launched preview directory. TRX: `artifacts/desktop-ui/sstefanczuk_SSTEFANCZUK-HPR_2026-11-21_12_29_24_net10.0.trx`.
- Retried the failed `Independent_stores_exclusion_reacquisition_and_copied_application_preserve_records` scenario against fresh `artifacts/phase1-automated-review/TimesheetAssistant.exe` with `--filter` — one passed. TRX: `artifacts/desktop-ui/sstefanczuk_SSTEFANCZUK-HPR_2026-11-21_12_33_33_net10.0.trx`. The 17 scenarios therefore passed across two runs, not in one clean full-suite run.
- Negative check: temporarily changed the current page heading back to `Workday`, published separately and ran `Empty_and_short_history_keep_today_addition_usable`. All three cases failed on the expected `Timesheet` heading assertion; the production source was restored from the staged version. TRX: `artifacts/desktop-ui/sstefanczuk_SSTEFANCZUK-HPR_2026-11-21_12_34_29_net10.0.trx`.
- The author confirmed the phase 1 manual checks in conversation on 2026-11-21, including both themes, navigation and heading, typography, contrast and focus, window sizes and theme persistence after restart.

These local results do not establish desktop CI acceptance or clean-device compatibility. Full redesign verification remains in phase 3.
