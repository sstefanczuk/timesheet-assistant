---
change_id: verify-desktop-ui-path
title: Sprawdzalna ścieżka uruchomienia aplikacji
status: implementing
created: 2026-11-02
updated: 2026-11-07
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
