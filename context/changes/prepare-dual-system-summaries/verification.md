# Phase 3 Verification

## Automated verification

- **Package:** `0.1.0-preview.241124`, published from application commit `ef5e89a0380810a16fa88e1f152fa1d25f5c5e09`.
- **Executable:** `artifacts/publish-0.1.0-preview.241124/TimesheetAssistant.exe`.
- **Release archive:** `artifacts/release-0.1.0-preview.241124/TimesheetAssistant-0.1.0-preview.241124-win-x64.zip`.
- **Archive SHA-256:** `5d8759e0d6a3a61566cb53a05f39f7cf6db6402ebb7f998257f8544325289c94`.
- **Build:** `dotnet build TimesheetAssistant.sln -c Release` — passed, 0 warnings and 0 errors.
- **Unit tests:** `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj -c Release --no-build` — passed, 117/117.
- **Desktop tests:** `dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj -c Release --no-build --logger trx` — passed, 26/26, 0 failed, 0 skipped.
- **Accepted baseline:** `artifacts/publish-0.1.0-preview.111402/TimesheetAssistant.exe` (source `5622cbd00bd258b509353493eac5c57356708c4a`).
- **TRX:** `artifacts/desktop-ui/sstefanczuk_SSTEFANCZUK-HPR_2026-11-24_21_54_06_net10.0.trx`.
- **Diagnostics:** per-session logs under `artifacts/desktop-ui/`; the final run completed scenario and profile cleanup.
- **Break check:** a throwaway package with the MyWork adjustment changed from 48 to 47 minutes failed the summary assertions at `6h 59m`, `7h 59m` and `3h 59m`. The production source was restored to 48 minutes; the final package and final desktop run use 48 minutes.
- **Test harness correction:** the first full run exposed that WebView cached the clipboard function, so replacing it after a successful copy did not simulate denial. The test now uses one stable clipboard function controlled by a scenario flag. The final US-01 test verifies both copied values and the accessible denial message.
- **Desktop CI:** not run; CI acceptance remains deferred under F-01.
- **Clean-device compatibility:** not verified.

## Author acceptance

- **Restart persistence:** the author reports that entries were still present after closing and reopening the app against the isolated data directory.
- **Overall acceptance:** accepted by the author on 2026-11-25. The author confirms the phase works as expected, including the remaining manual checks below.

Manual checks confirmed by the author on the published executable above:

1. Enter the US-01 records (`T-1 / Scrum Ceremonies / Refinement / 1h` and `X-1 / Order Page / empty description / 6h 12m`); inspect the exact Jira and MyWork rows and totals.
2. Copy the Jira ID, work description and MyWork description; confirm the pasted values. Edit an entry, reopen Summaries and confirm the changed title/description.
3. Close and reopen the app, then reopen Summaries and confirm the saved values.
4. Check keyboard navigation and focus, then check both light and dark themes.
