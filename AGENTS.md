# Repository Guidelines

Timesheet Assistant is a Windows desktop application built with .NET 10 MAUI Blazor Hybrid, C#, Razor, and WebView2. The repository contains the starter application, an empty pure-logic xUnit project, and a real desktop UI smoke test.

## Critical Rules

- Write all Git commit subjects and bodies in English.

- Keep the application Windows-only: `net10.0-windows10.0.19041.0`. Retain the `TargetFrameworks` property in @src/TimesheetAssistant/TimesheetAssistant.csproj; do not reintroduce Android, iOS, MacCatalyst, or Tizen targets without an explicit request.
- Use English for all content under `src/`, including comments, documentation, tests, and UI text; see @src/AGENTS.md.
- Treat `context/archive/` as immutable. Create new change documentation under `context/changes/` instead of editing archived changes.
- Keep credentials, signing certificates, build output, and Visual Studio state out of commits; follow @.gitignore.

## Build and Verification

Run commands from the repository root on Windows with the .NET 10 SDK and `maui-windows` workload installed.

- `dotnet build TimesheetAssistant.sln` builds the application and both test projects.
- `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj` runs pure-logic tests before packaging (currently empty).
- After publishing, set `TIMESHEET_ASSISTANT_TEST_APP` to the absolute path of the Release `TimesheetAssistant.exe`, then run `dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj --logger trx`. Windows and WebView2 Evergreen Runtime are required; see @src/TimesheetAssistant.UiTests/README.md for publication commands.
- `dotnet list TimesheetAssistant.sln package --vulnerable --include-transitive --source https://api.nuget.org/v3/index.json` audits direct and transitive dependencies.

Launch the application from Visual Studio using the Windows target. Report blocked checks separately from successful checks.

## Layout and Implementation

@TimesheetAssistant.sln contains the application in `src/TimesheetAssistant/` and separate tests in `src/TimesheetAssistant.UnitTests/` and `src/TimesheetAssistant.UiTests/`. Razor pages and layouts live in `Components/`; web assets belong in `wwwroot/`, MAUI assets in `Resources/`, and Windows integration in `Platforms/Windows/`. Register application services in @src/TimesheetAssistant/MauiProgram.cs. Match neighboring file formatting; no repository-wide formatter configuration exists yet.

Use @context/foundation/prd.md for product scope and @context/foundation/tech-stack.md for stack decisions. Local persistence is required by the MVP; Jira/MyWork integrations and authentication remain out of scope.

## Tests and Delivery

@src/TimesheetAssistant.UnitTests/README.md and @src/TimesheetAssistant.UiTests/README.md record the current test setup. Playwright .NET launches the real Release application and connects to its WebView2 for the temporary Home → Counter → 0 to 1 smoke test. It does not verify work-entry persistence or clean-device compatibility. See @context/deployment/deploy-plan.md for release gates and the pending CI evidence.

The implemented test architecture uses separate `TimesheetAssistant.UnitTests` (pure logic, without Playwright, WebView2, an EXE or real storage) and `TimesheetAssistant.UiTests` (real application workflows). S-01 must add explicit persistence/restart scenarios and domain-storage isolation. Do not introduce an IntegrationTests project. See @context/foundation/tech-stack.md.

Run UnitTests before packaging and UiTests against `artifacts/publish-<version>/TimesheetAssistant.exe` after packaging. Separate suites by project paths, not category filters. `artifacts/desktop-ui/` holds TRX and per-session logs, plus screenshots/traces when available on failure. CDP and unique profiles apply only to the launched child, never global debug settings.

The desktop CI gate is deferred as of 2026-11-11. Its workflow candidate is preserved in local branch `wip/f-01-desktop-ui-gate-20261111` at `8980a91`; main retains its existing build/package/release workflow without the desktop gate. The candidate requires a successful runner exit and a TRX with at least one executed test and no failures, and uploads `desktop-ui-diagnostics` separately even after failure with three-day retention. Continue local desktop verification; a green main build does not establish desktop CI acceptance. F-01 remains incomplete until successful and controlled-failure CI runs are recorded.

Under roadmap v3, F-01 does not block planning, implementation or local acceptance of product slices S-01–S-08. Verify each feature with appropriate local unit and real desktop UI tests and record the results; S-01 must cover isolated storage and save/restart/read behavior. Resume the remaining CI work in the existing F-01 after all product slices, before closing M-1. Do not create a duplicate CI item or treat the earlier Counter smoke as evidence for new features.

Follow the observed commit style: `chore: initialize Windows MAUI Blazor solution`, `docs: add root agent instructions`.

<!-- BEGIN @przeprogramowani/10x-cli -->

## Zestaw narzędzi AI 10xDevs — Moduł 2, Lekcja 3

Przed scaleniem przejrzyj kod wygenerowany przez AI za pomocą **łańcucha przeglądu implementacji**:

```
/10x-implement -> /10x-impl-review -> triage -> (/10x-lesson | fix | skip | disagree)
```

`/10x-impl-review` jest głównym tematem lekcji. Przegląd jest bramką jakości, a nie poleceniem naprawienia każdego znaleziska.

### Router zadań — od czego zacząć

| Umiejętność | Użyj jej, gdy |
| --- | --- |
| **Przegląd kodu (główny temat lekcji)** | |
| `/10x-impl-review <change-id>` | Zaimplementowałeś kod i chcesz przeprowadzić ustrukturyzowany przegląd przed scaleniem. Umiejętność sprawdza zgodność z planem, dyscyplinę zakresu, bezpieczeństwo i jakość, architekturę, spójność wzorców oraz kryteria sukcesu, a następnie przedstawia znaleziska do triage. |
| **Rezultat powtarzającej się lekcji** | |
| `/10x-lesson` | Znalezisko ujawnia powtarzającą się regułę projektu lub wzorzec błędów agenta. Zapisz je w `context/foundation/lessons.md` zamiast traktować jako jednorazową notatkę. |

### Dyscyplina triage

- Severity określa, jak poważne jest znalezisko. Impact określa, jak duże znaczenie ma teraz decyzja.
- Prawidłowe rezultaty: napraw teraz, napraw inaczej, pomiń, zaakceptuj jako ryzyko, zapisz jako powtarzającą się regułę (`/10x-lesson`), nie zgódź się.
- Napraw krytyczne znaleziska. Nie poświęcaj godzin na obserwacje o niskim wpływie tylko dlatego, że agent je znalazł.
- Świadome pomijanie znalezisk o niskim wpływie jest prawidłowym wynikiem przeglądu, a nie zaniedbaniem.
- Jeśli nie zgadzasz się ze znaleziskiem, zapisz dlaczego. Błędne rozumowanie agenta również jest sygnałem.

### Granice przeglądu

- Ta lekcja dotyczy przeglądu zaimplementowanego kodu. Nie tworzy planu, nie wykonuje nowych faz ani nie uczy przeglądu CI.
- Strategia testowania i bramki jakości są wprowadzane w Module 3.
- W tej lekcji nie używaj `/10x-contract` jako wyniku triage.

### Ścieżki używane przez tę lekcję

- `context/changes/<change-id>/plan.md` — oczekiwany kontrakt implementacji
- `context/changes/<change-id>/reviews/` — wynik przeglądu
- `context/foundation/lessons.md` — powtarzające się lekcje

Umiejętności nie mogą zapisywać do `context/archive/`. Zarchiwizowane zmiany są niezmienne; jeśli rozwiązana ścieżka docelowa zaczyna się od `context/archive/`, przerwij z komunikatem: „Ta zmiana jest zarchiwizowana. Zamiast tego otwórz nową zmianę za pomocą `/10x-new`.”

<!-- END @przeprogramowani/10x-cli -->
