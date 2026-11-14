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

@TimesheetAssistant.sln contains the application in `src/TimesheetAssistant/` and separate tests in `src/TimesheetAssistant.UnitTests/` and `src/TimesheetAssistant.UiTests/`. Organize product code and tests by feature under `Features/<FeatureName>/` within each applicable project, following @src/AGENTS.md; keep feature-owned Razor pages with their feature. Shared Razor layouts/routing remain in `Components/`; web assets belong in `wwwroot/`, MAUI assets in `Resources/`, and Windows integration in `Platforms/Windows/`. Register application services in @src/TimesheetAssistant/MauiProgram.cs. Follow @.editorconfig and the C# style and spacing rules in @src/AGENTS.md.

Use @context/foundation/prd.md for product scope and @context/foundation/tech-stack.md for stack decisions. Local persistence is required by the MVP; Jira/MyWork integrations and authentication remain out of scope.

## Tests and Delivery

@src/TimesheetAssistant.UnitTests/README.md and @src/TimesheetAssistant.UiTests/README.md record the current test setup. Playwright .NET launches the real Release application and connects to its WebView2 for the temporary Home → Counter → 0 to 1 smoke test. It does not verify work-entry persistence or clean-device compatibility. See @context/deployment/deploy-plan.md for release gates and the pending CI evidence.

The implemented test architecture uses separate `TimesheetAssistant.UnitTests` (pure logic, without Playwright, WebView2, an EXE or real storage) and `TimesheetAssistant.UiTests` (real application workflows). S-01 must add explicit persistence/restart scenarios and domain-storage isolation. Do not introduce an IntegrationTests project. See @context/foundation/tech-stack.md.

Run UnitTests before packaging and UiTests against `artifacts/publish-<version>/TimesheetAssistant.exe` after packaging. Separate suites by project paths, not category filters. `artifacts/desktop-ui/` holds TRX and per-session logs, plus screenshots/traces when available on failure. CDP and unique profiles apply only to the launched child, never global debug settings.

The desktop CI gate is deferred as of 2026-11-11. Its workflow candidate is preserved in local branch `wip/f-01-desktop-ui-gate-20261111` at `8980a91`; main retains its existing build/package/release workflow without the desktop gate. The candidate requires a successful runner exit and a TRX with at least one executed test and no failures, and uploads `desktop-ui-diagnostics` separately even after failure with three-day retention. Continue local desktop verification; a green main build does not establish desktop CI acceptance. F-01 remains incomplete until successful and controlled-failure CI runs are recorded.

Under roadmap v3, F-01 does not block planning, implementation or local acceptance of product slices S-01–S-08. Verify each feature with appropriate local unit and real desktop UI tests and record the results; S-01 must cover isolated storage and save/restart/read behavior. Resume the remaining CI work in the existing F-01 after all product slices, before closing M-1. Do not create a duplicate CI item or treat the earlier Counter smoke as evidence for new features.

Follow the observed commit style: `chore: initialize Windows MAUI Blazor solution`, `docs: add root agent instructions`.

<!-- BEGIN @przeprogramowani/10x-cli -->

## Zestaw narzędzi AI 10xDevs — Moduł 2, Lekcja 4

Przygotuj się na trudniejszy strumień implementacji z **łańcuchem planowania opartym na badaniach**:

```
internal research (/10x-research) + external research (exa.ai, Context7) -> /10x-plan -> /10x-implement -> success
```

Lekcja koncentruje się na odróżnianiu badań wewnętrznych od zewnętrznych oraz wykorzystywaniu dowodów do uzasadniania decyzji planistycznych.

### Router zadań — od czego zacząć

| Skill | Użyj, gdy |
| --- | --- |
| **Badania wewnętrzne (temat lekcji)** | |
| `/10x-research <change-id>` | Potrzebujesz dowodów z istniejącego codebase’u — wzorców, konwencji, punktów integracji lub istniejących implementacji. Uruchamia równoległych sub-agentów w repozytorium i zapisuje ustrukturyzowane ustalenia w `research.md`. |
| **Badania zewnętrzne (temat lekcji)** | |
| exa.ai | Potrzebujesz wyszukiwania w sieci natywnie wspieranego przez AI do porównań bibliotek, najlepszych praktyk lub kontekstu ekosystemu, na które codebase nie potrafi odpowiedzieć. |
| Context7 (`resolve-library-id` → `get-library-docs`) | Potrzebujesz aktualnej, bieżącej dokumentacji dla konkretnej biblioteki lub frameworka. Najpierw rozwiązuje ID biblioteki, a następnie pobiera odpowiednie strony dokumentacji. |
| **Koło zapasowe do ramowania problemu** | |
| `/10x-frame <change-id>` | Plan nie może się ustabilizować, plan nie daje oczekiwanych wyników albo uporczywy dryf stale psuje implementację. Użyj jako wyjścia awaryjnego dla osobnego problemu (zademonstrowanego na przykładzie Space Explorers), a nie jako rytuału przed badaniami. |
| **Planowanie i wykonanie** | |
| `/10x-plan <change-id>` / `/10x-implement <change-id> phase <n>` | Użyj tego samego łańcucha planowania i wykonania co w Lekcji 2, teraz z dowodami z wcześniejszych badań zasilającymi plan. |

### Dyscyplina badawcza

- Badania wewnętrzne (`/10x-research`) odpowiadają na pytanie „co nasz codebase już robi?” — wzorce, schematy, konwencje, punkty integracji.
- Badania zewnętrzne (exa.ai, Context7) odpowiadają na pytanie „co powinniśmy zrobić?” — możliwości bibliotek, dokumentacja API, najlepsze praktyki ekosystemu.
- Połącz oba jako wkład oparty na dowodach dla `/10x-plan`. Plan bez dowodów badawczych w nietrywialnym strumieniu jest zgadywaniem.
- Dokumentacja przyjazna agentom (`llms.txt`, markdown-for-agents, endpointy `/md`) jest sygnałem jakości przy wyborze bibliotek — biblioteki publikujące dokumentację czytelną dla agentów integrują się szybciej.

### `/10x-frame` jako koło zapasowe

Trzy sygnały, by sięgnąć po `/10x-frame`:
1. Plan nie może się ustabilizować — badania stale otwierają kolejne pytania, zamiast zawężać się do kontraktu.
2. Plan nie dostarcza rezultatów — implementacja wielokrotnie nie spełnia kryteriów sukcesu.
3. Uporczywy dryf — implementacja stale odbiega od planu w sposób sugerujący, że problem został błędnie sformułowany.

Zademonstrowano na przykładzie Space Explorers, a nie na ścieżce SRS. To wyjście awaryjne, a nie obowiązkowy krok.

### Ścieżki używane przez tę lekcję

- `context/changes/<change-id>/research.md` - wynik badań wewnętrznych
- `context/changes/<change-id>/frame.md` - wynik ramowania problemu, gdy jest potrzebny
- `context/changes/<change-id>/plan.md` - kontrakt implementacyjny oparty na dowodach
- `context/foundation/lessons.md` - powtarzające się reguły i pułapki

Skills nie mogą zapisywać do `context/archive/`. Zarchiwizowane zmiany są niezmienne; jeśli rozstrzygnięta docelowa ścieżka zaczyna się od `context/archive/`, przerwij z komunikatem: „This change is archived. Open a new change with `/10x-new` instead.”

<!-- END @przeprogramowani/10x-cli -->
