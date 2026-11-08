---
change_id: verify-desktop-ui-path
title: Sprawdzalna ścieżka uruchomienia aplikacji
status: implementing
created: 2026-11-02
updated: 2026-11-08
archived_at: null
---

## Notes

F-01 z roadmapy M-1. Plan przygotowany z umiejętnością 10x-plan po uzgodnieniu zakresu i trzech faz. Counter jest tymczasowym scenariuszem; S-01 zastąpi go testem rzeczywistej pracy. Problemy runnera zatrzymują odbiór i wymagają uzgodnienia dalszego kroku.

## Phase 1 verification — 2026-11-07

- User approved updating the pinned SDK from 10.0.402 to installed 10.0.403. Retained workload set 10.0.402.1; `dotnet workload --info` resolves maui-windows 10.0.110. Local solution build passed with zero warnings/errors. CI reads SDK selection from global.json; no remote CI run has been claimed.
- Sandbox build could not read the developer Windows SDK path. The same build outside the sandbox passed. All native application probes ran outside the sandbox.
- Published Release win-x64 with SelfContained=true, WindowsAppSDKSelfContained=true and WindowsPackageType=None to ignored `artifacts/phase1-20261107/publish` without creating a release.
- Real-process Home connection probe passed (1 executed test; approximately 10 seconds including fixture). PID 69180 and profile 6771b917bdd74bd486d7b9c8e0db0f6a were confirmed removed after completion. TRX: `artifacts/phase1-20261107/results/connection.trx`.
- Initial probe exposed xUnit's single-public-constructor requirement; the options constructor is now internal. No application was launched by that failed attempt.
- Missing executable produced FileNotFoundException. Controlled unavailable-endpoint probe used a temporary 3-second startup budget and a different CDP port, produced the expected bounded startup error, and left no new application processes or profiles. Source was restored in finally. TRX: `missing-exe.trx` and `unavailable-endpoint.trx` in the same ignored results folder.
- Counter interaction, failure screenshot/trace capture and a real windows-2025 run remain phases 2–3. Persistence is outside F-01.
- Break-check passed: a temporary production Home heading change made the same desktop test fail its visibility assertion after 15 seconds. Home source was unconditionally restored from Git; the broken package remains only in ignored artifacts. TRX: `break-check.trx`.
- Final Release restore/build with CI properties passed (zero warnings/errors), UnitTests runner completed with zero tests as expected, UiTests executed and passed one real-process test, actionlint and staged whitespace checks passed, and dependency audit reported no vulnerable packages in all three projects. Final successful TRX: `final-connection.trx`. No remote workflow was launched during phase 1.

## Phase 1 review correction — 2026-11-07

- Fixed the process-start/job-assignment race identified in review: CreateProcessW now creates the EXE suspended with its child-only Unicode environment; AssignProcessToJobObject precedes ResumeThread. Creation handles and environment memory are released on every path. Assignment/resume failure terminates and waits for the created process within the cleanup budget.
- Release solution build passed with zero warnings/errors, and the existing real-WebView2 Home probe passed with the new launcher.
- A controlled temporary probe observed the initial thread in Wait/Suspended state before assignment, forced an assignment-path failure, and confirmed the unassigned PID was removed. The probe source was restored in finally. Evidence: ignored `artifacts/phase1-20261107/results/assignment-failure.trx`; its failed test result is expected for this negative probe.
- Final restored-source probe passed (1 test), and its exact application PID and profile were confirmed removed. TRX: `artifacts/phase1-20261107/results/suspended-launch-final.trx`.

## Phase 2 verification — 2026-11-08

- Replaced the Home-only probe with the temporary role-based Home → Counter → 0 → 1 workflow. Added timestamped startup/PID/profile/endpoint/stage logs and protected screenshot/trace capture before cleanup. Runsettings routes TRX into ignored `artifacts/desktop-ui/`.
- Used the existing correct Release win-x64 publication at `artifacts/phase1-20261107/publish/TimesheetAssistant.exe`. Two consecutive runs each executed and passed one test on different profiles (ad6c13bf8e6040baad61e9a0b93e5b72 and c1258d3e309148489537f83485927db3). Their TRX files end in `07_07_04_net10.0.trx` and `07_07_25_net10.0.trx`.
- A temporary final expectation of 2 produced the intended assertion failure with actual count 1. Source was restored in finally. Evidence: `artifacts/desktop-ui/phase2-forced-assertion.trx` and session `1bbabfd3de6a4ffdbf0406f30321a6d0` containing `desktop.log`, `failure.png` (visually inspected), and readable `trace.zip`. Exact app PIDs 44868, 53956 and 64648 and all three corresponding profile directories were confirmed removed.
- Break-check changed the production increment to +2, published to the separate ignored `artifacts/desktop-ui/broken-counter-publish`, and observed the unchanged test fail with expected 1 / actual 2. Counter source was unconditionally restored from the Git index. Evidence: `phase2-break-check.trx`; this deliberately broken package is not a release input.
- A temporary viewport of 500 × 800 exercised collapsed navigation successfully (`phase2-narrow.trx`); the source was restored in finally. Final unchanged-source run executed and passed one test (`phase2-final.trx`).
- Solution build passed with zero warnings/errors, UnitTests runner completed with zero tests (no functional coverage claimed), actionlint and staged whitespace checks passed. NuGet audit against nuget.org reported no vulnerable packages in all three projects.
- The sandbox denied local Windows SDK access. Build, native application probes and audit completed outside the sandbox. No CI workflow or release was launched. Counter remains temporary; persistence is outside this phase. The author confirmed manual criterion 2.4 on 2026-11-08: visible navigation/count change, normal application startup and independent WebView2 sessions.
