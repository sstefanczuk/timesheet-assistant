# Repository Guidelines

Timesheet Assistant is a Windows desktop application built with .NET 10 MAUI Blazor Hybrid, C#, Razor, and WebView2. The repository currently contains the starter application and an empty xUnit test project.

## Critical Rules

- Keep the application Windows-only: `net10.0-windows10.0.19041.0`. Retain the `TargetFrameworks` property in @src/TimesheetAssistant/TimesheetAssistant.csproj; do not reintroduce Android, iOS, MacCatalyst, or Tizen targets without an explicit request.
- Use English for all content under `src/`, including comments, documentation, tests, and UI text; see @src/AGENTS.md.
- Treat `context/archive/` as immutable. Create new change documentation under `context/changes/` instead of editing archived changes.
- Keep credentials, signing certificates, build output, and Visual Studio state out of commits; follow @.gitignore.

## Build and Verification

Run commands from the repository root on Windows with the .NET 10 SDK and `maui-windows` workload installed.

- `dotnet build TimesheetAssistant.sln` builds both projects.
- `dotnet test src/TimesheetAssistant.Tests/TimesheetAssistant.Tests.csproj` runs xUnit discovery and tests.
- `dotnet list TimesheetAssistant.sln package --vulnerable --include-transitive --source https://api.nuget.org/v3/index.json` audits direct and transitive dependencies.

Launch the application from Visual Studio using the Windows target. Report blocked checks separately from successful checks.

## Layout and Implementation

@TimesheetAssistant.sln contains the application in `src/TimesheetAssistant/` and tests in `src/TimesheetAssistant.Tests/`. Razor pages and layouts live in `Components/`; web assets belong in `wwwroot/`, MAUI assets in `Resources/`, and Windows integration in `Platforms/Windows/`. Register application services in @src/TimesheetAssistant/MauiProgram.cs. Match neighboring file formatting; no repository-wide formatter configuration exists yet.

Use @context/foundation/prd.md for product scope and @context/foundation/tech-stack.md for stack decisions. Local persistence is required by the MVP; Jira/MyWork integrations and authentication remain out of scope.

## Tests and Delivery

@src/TimesheetAssistant.Tests/README.md records the current test setup. No tests exist yet; successful runner execution alone is not functional verification. Playwright .NET against the application's WebView2 is planned but unimplemented. The Windows preview workflow builds and packages the starter; see @context/deployment/deploy-plan.md for release gates and verification status.

Follow the observed commit style: `chore: initialize Windows MAUI Blazor solution`, `docs: add root agent instructions`.

<!-- BEGIN @przeprogramowani/10x-cli -->

## Zestaw narzędzi AI 10xDevs - Moduł 2, Lekcja 1

Przejdź od konfiguracji sprint-zero do orkiestracji projektu za pomocą **łańcucha roadmapy**:

```
(Module 1 foundation docs) -> /10x-roadmap -> backlog-ready roadmap items
```

`/10x-roadmap` jest głównym tematem lekcji. `/10x-new` jest celowo wprowadzane w Module 2, Lesson 2, gdy wybrany element roadmapy staje się folderem zmiany implementacyjnej.

### Router zadań - Od czego zacząć

| Umiejętność | Użyj jej, gdy |
| --- | --- |
| **Roadmapa (główny temat lekcji)** | |
| `/10x-roadmap` | Masz `context/foundation/prd.md` oraz bazę projektu ze szkieletem i potrzebujesz roadmapy MVP z podejściem vertical-first. Umiejętność odczytuje PRD, sprawdza bazę kodu, korzysta z dostępnych dokumentów fundamentowych, takich jak `tech-stack.md`, `infrastructure.md` i `deploy-plan.md`, a następnie zapisuje `context/foundation/roadmap.md`. Użyj jej PRZED tworzeniem folderów dla poszczególnych zmian lub planów implementacji. |
| **Ponownie uruchom wcześniejsze kroki, jeśli to konieczne** | |
| `/10x-shape` / `/10x-prd` / `/10x-tech-stack-selector` / `/10x-bootstrapper` / `/10x-agents-md` / `/10x-infra-research` | Zebrane z Module 1, aby kontrakty fundamentowe można było poprawić przed sekwencjonowaniem roadmapy. Jeśli generowanie roadmapy ujawni lukę w PRD, popraw PRD, zanim uznasz, że backlog jest gotowy. |

### Jak łańcuch przekazuje dalej

- `/10x-roadmap` łączy produkt z implementacją. Nie wybiera frameworków, nie projektuje schematów ani nie pisze planu implementacji dla poszczególnych zmian.
- Wynikiem jest `context/foundation/roadmap.md`: uporządkowane kamienie milowe, vertical slices, ograniczone fundamenty, zależności, niewiadome, ryzyko oraz pola przekazania do backlogu.
- Elementy roadmapy powinny otrzymać stabilne, czytelne dla człowieka identyfikatory w narzędziach backlogu. Właściwy folder `context/changes/<change-id>/` jest tworzony w Lesson 2 za pomocą `/10x-new`.

### Granice roadmapy

- Domyślnie stosuj vertical slices: widoczne dla użytkownika rezultaty obejmujące UI, dane, logikę biznesową i integracje.
- Praca horyzontalna jest dozwolona wyłącznie jako ograniczony enabler, który wskazuje odblokowywany przez siebie późniejszy pionowy kamień milowy.
- Unikaj osieroconej pracy horyzontalnej, takiej jak „zbuduj całą bazę danych”, „zbuduj wszystkie endpointy API” lub „zaprojektuj całe UI” przed pierwszym widocznym dla użytkownika przepływem.
- Roadmapa nie jest estymacją kalendarzową. Nie wymyślaj dat, story points ani velocity sprintu, chyba że użytkownik wyraźnie prosi o osobny artefakt planistyczny.

### Ścieżki fundamentów używane przez tę lekcję

- `context/foundation/prd.md` - wejście
- `context/foundation/tech-stack.md` - opcjonalne wejście
- `context/foundation/infrastructure.md` - opcjonalne wejście
- `context/deployment/deploy-plan.md` - opcjonalne wejście
- `context/foundation/roadmap.md` - wyjście
- `context/foundation/lessons.md` - powtarzające się reguły i pułapki
- `docs/reference/contract-surfaces.md` - rejestr kluczowych nazw

Umiejętności nie mogą zapisywać do `context/archive/`. Zarchiwizowane zmiany są niezmienne; jeśli rozstrzygnięta ścieżka docelowa zaczyna się od `context/archive/`, przerwij z komunikatem: "This change is archived. Open a new change with `/10x-new` instead."

<!-- END @przeprogramowani/10x-cli -->
