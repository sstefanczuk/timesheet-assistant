---
bootstrapped_at: 2026-10-22T19:04:38.6942513+00:00
starter_id: custom
starter_name: .NET MAUI Blazor Hybrid
project_name: timesheet-assistant
language_family: dotnet
package_manager: dotnet
cwd_strategy: subdir-then-move
bootstrapper_confidence: best-effort
phase_3_status: ok
audit_command: dotnet list package --vulnerable --include-transitive
---

## Hand-off

---
starter_id: custom
custom_starter:
  name: .NET MAUI Blazor Hybrid
  docs_url: https://learn.microsoft.com/aspnet/core/blazor/hybrid/tutorials/maui
package_manager: dotnet
project_name: timesheet-assistant
hints:
  language_family: dotnet
  team_size: solo
  deployment_target: github-releases
  ci_provider: github-actions
  ci_default_flow: auto-deploy-on-merge
  bootstrapper_confidence: best-effort
  path_taken: custom
  quality_override: false
  self_check_answers:
    typed: true
    from_official_starter: true
    conventions: false # Odpowiedź użytkownika: "Nie wiem", nie stwierdzona wada stosu.
    docs_current: true
    can_judge_agent: false # Odpowiedź użytkownika: "Nie wiem" — wymaga wsparcia w ocenie kodu MAUI.
  has_auth: false
  has_payments: false
  has_realtime: false
  has_ai: false
  has_background_jobs: false
---

## Why this stack

Timesheet Assistant powstaje solo na Windows 11, przy 13 latach doświadczenia autora w .NET, niewielkiej znajomości XAML oraz większej znajomości Blazor/HTML/CSS i wcześniejszej aplikacji Blazor Hybrid. Wybrano oficjalny szablon .NET MAUI Blazor Hybrid, z C# i interfejsem Razor, bez backendu chmurowego; wpisy pozostają lokalne. GitHub Actions ma budować aplikację i uruchamiać testy na Windows, a po scaleniu do main i pomyślnym przejściu testów automatycznie publikować wydanie ZIP z folderem aplikacji przez GitHub Releases. Jest to dystrybucja, nie uruchamianie aplikacji w chmurze; widoczność repozytorium pozostaje nieustalona. Do testów UI wybrano Playwright .NET podłączany do WebView2 rzeczywistej aplikacji, z fikcyjnymi danymi i odizolowanym magazynem testowym. Pierwszy eksperyment ma potwierdzić uruchomienie, interakcję i trwałość zapisu w CI; wykonalność tego zestawu nie została jeszcze sprawdzona. Starter jest poza rejestrem, więc tworzenie szkieletu ma poziom best-effort i może wymagać ręcznych kroków. Niepewność autora dotycząca konwencji i oceny kodu wymaga jawnych zasad projektu oraz przeglądu zmian. Integracje Jira/MyWork, uwierzytelnianie i przesyłanie wpisów pozostają poza MVP.

## Pre-scaffold verification

| Signal | Value | Severity | Notes |
| --- | --- | --- | --- |
| npm package | not run | n/a | Non-JS custom starter |
| GitHub repo | not run | n/a | docs_url points to Microsoft Learn; no recency signal available |
| Official generator | maui-blazor | n/a | Microsoft Learn tutorial consulted; installed CLI help confirmed options |
| Local SDK | 10.0.203, maui-windows installed | informational | CLI reported pending restart/workload verification warning during preparation |

Documentation: https://learn.microsoft.com/aspnet/core/blazor/hybrid/tutorials/maui
User explicitly approved the hand-off and exact command.

## Scaffold log

**Resolved invocation**: `dotnet new maui-blazor -n TimesheetAssistant -o .bootstrap-scaffold -f net10.0`
**Strategy**: subdir-then-move
**Exit code**: 0
**Files moved**: 43
**Conflicts (.scaffold siblings)**: none
**.gitignore handling**: absent in scaffold
**.bootstrap-scaffold cleanup**: deleted

Project display/assembly name: TimesheetAssistant. Hand-off project slug retained as timesheet-assistant.
Existing files and context/ preserved. No source modifications after generation.

**CLI stdout/stderr (combined)**:

```text
The template ".NET MAUI Blazor Hybrid App" was created successfully.
```

### File move log

- MOVE: App.xaml
- MOVE: App.xaml.cs
- MOVE: MainPage.xaml
- MOVE: MainPage.xaml.cs
- MOVE: MauiProgram.cs
- MOVE: TimesheetAssistant.csproj
- MOVE: Components\_Imports.razor
- MOVE: Components\Routes.razor
- MOVE: Components\Layout\MainLayout.razor
- MOVE: Components\Layout\MainLayout.razor.css
- MOVE: Components\Layout\NavMenu.razor
- MOVE: Components\Layout\NavMenu.razor.css
- MOVE: Components\Pages\Counter.razor
- MOVE: Components\Pages\Home.razor
- MOVE: Components\Pages\NotFound.razor
- MOVE: Components\Pages\Weather.razor
- MOVE: Platforms\Android\AndroidManifest.xml
- MOVE: Platforms\Android\MainActivity.cs
- MOVE: Platforms\Android\MainApplication.cs
- MOVE: Platforms\Android\Resources\values\colors.xml
- MOVE: Platforms\iOS\AppDelegate.cs
- MOVE: Platforms\iOS\Info.plist
- MOVE: Platforms\iOS\Program.cs
- MOVE: Platforms\iOS\Resources\PrivacyInfo.xcprivacy
- MOVE: Platforms\MacCatalyst\AppDelegate.cs
- MOVE: Platforms\MacCatalyst\Entitlements.plist
- MOVE: Platforms\MacCatalyst\Info.plist
- MOVE: Platforms\MacCatalyst\Program.cs
- MOVE: Platforms\Windows\app.manifest
- MOVE: Platforms\Windows\App.xaml
- MOVE: Platforms\Windows\App.xaml.cs
- MOVE: Platforms\Windows\Package.appxmanifest
- MOVE: Properties\launchSettings.json
- MOVE: Resources\AppIcon\appicon.svg
- MOVE: Resources\AppIcon\appiconfg.svg
- MOVE: Resources\Fonts\OpenSans-Regular.ttf
- MOVE: Resources\Images\dotnet_bot.svg
- MOVE: Resources\Raw\AboutAssets.txt
- MOVE: Resources\Splash\splash.svg
- MOVE: wwwroot\app.css
- MOVE: wwwroot\index.html
- MOVE: wwwroot\lib\bootstrap\dist\css\bootstrap.min.css
- MOVE: wwwroot\lib\bootstrap\dist\css\bootstrap.min.css.map

## Post-scaffold audit

**Tool**: `dotnet list package --vulnerable --include-transitive`
**Status**: failed to run
**Exit code**: 1
**Reason**: Sandbox denied access to local SDK directory during project evaluation.
**Severity counts / direct vs transitive**: unavailable; this is not a clean audit result.

**Captured output**:

```text
Trwa określanie projektów do przywrócenia...
C:\Program Files\dotnet\sdk\10.0.203\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.targets(1497,5): error MSB4184: Nie można ocenić wyrażenia „[Microsoft.Build.Utilities.ToolLocationHelper]::GetPlatformSDKDisplayName(android, 0.0)”. Access to the path 'C:\Users\sstefanczuk\AppData\Local\Microsoft SDKs' is denied. [C:\Repos\10xDevs-project\TimesheetAssistant.csproj]
```

A retry with `--framework net10.0-windows10.0.19041.0` failed with the same error. An intermediate attempt using `-p:TargetFrameworks=net10.0-windows10.0.19041.0` was rejected because dotnet list package does not accept MSBuild property arguments.
Build and application launch were not verified. Default template includes Android, iOS and MacCatalyst targets in addition to Windows; only maui-windows is installed locally.

## Hints recorded but not acted on

| Hint | Value |
| --- | --- |
| bootstrapper_confidence | best-effort |
| quality_override | false |
| path_taken | custom |
| self_check_answers | typed: true; from_official_starter: true; conventions: false (user unsure); docs_current: true; can_judge_agent: false (user unsure) |
| team_size | solo |
| deployment_target | github-releases |
| ci_provider | github-actions |
| ci_default_flow | auto-deploy-on-merge |
| has_auth | false |
| has_payments | false |
| has_realtime | false |
| has_ai | false |
| has_background_jobs | false |

## Next steps

### Korekta struktury po bootstrapie

Na życzenie użytkownika przeniesiono 43 pliki aplikacji do `src/TimesheetAssistant/`, zachowując ich zawartość. Utworzono `TimesheetAssistant.sln` w katalogu głównym i projekt xUnit `src/TimesheetAssistant.Tests/TimesheetAssistant.Tests.csproj` (.NET 10). Oba projekty dodano do solucji; potwierdzono je poleceniem `dotnet sln TimesheetAssistant.sln list`. Usunięto pusty przykładowy test. Projekt testowy nie ma jeszcze testów ani referencji do aplikacji MAUI; konfiguracja Playwright pozostaje dalszym etapem.

Próba `dotnet test src/TimesheetAssistant.Tests/TimesheetAssistant.Tests.csproj --verbosity minimal` zakończyła się przed kompilacją: brak dostępu do `C:\Users\sstefanczuk\AppData\Roaming\NuGet\NuGet.Config` w sandboxie. Testy i kompilacja pozostają niezweryfikowane. Historyczne ścieżki wyżej opisują pierwotny bootstrap; aktualna ścieżka aplikacji to `src/TimesheetAssistant/TimesheetAssistant.csproj`.

Agent onboarding can be handled separately with /10x-agents-md. Existing AGENTS.md was preserved.

- Open TimesheetAssistant.csproj in Visual Studio and verify the Windows build and launch.
- Decide whether to restrict target frameworks to Windows before restoring/building on this Windows-only workstation.
- Rerun the dependency audit with access to the local SDK directory; vulnerability findings remain unknown.
- Set up CI and release automation separately as recorded in the hand-off.

## Ponowna weryfikacja po bootstrapie — 2026-10-23

- Konfiguracja: `TimesheetAssistant.sln` zawiera aplikację w `src/TimesheetAssistant/` i projekt xUnit w `src/TimesheetAssistant.Tests/`.
- Aplikacja targetuje wyłącznie `net10.0-windows10.0.19041.0`.
- `dotnet build TimesheetAssistant.sln --verbosity minimal`: exit 0; **0 ostrzeżeń, 0 błędów**. Zbudowano oba projekty. Czas: 19.95 s.
- `dotnet test src/TimesheetAssistant.Tests/TimesheetAssistant.Tests.csproj --no-build --no-restore --verbosity minimal`: exit 0; runner działa, ale **nie znaleziono żadnych testów**. Nie jest to potwierdzenie poprawności funkcjonalnej aplikacji.
- `dotnet list TimesheetAssistant.sln package --vulnerable --include-transitive --no-restore --source https://api.nuget.org/v3/index.json`: exit 0; **0 znanych podatności** w obu projektach, włącznie z zależnościami przechodnimi (CRITICAL/HIGH/MODERATE/LOW: 0). Źródło: publiczne nuget.org, stan na moment audytu.
- Pierwsze próby audytu w sandboxie były blokowane przez brak dostępu do użytkowego NuGet.Config, również z osobnym plikiem konfiguracji. Audyt zakończono pomyślnie po zatwierdzonym rozszerzeniu dostępu. Globalnej konfiguracji NuGet nie zmieniano.
- Uruchomienie aplikacji z Visual Studio zostało potwierdzone przez użytkownika; w tej sesji nie wykonywano automatycznej weryfikacji interfejsu. Playwright/WebView2 pozostaje do wdrożenia.

Ta ponowna weryfikacja zastępuje wcześniejsze statusy „build/audit niezweryfikowany” opisane historycznie powyżej. Bootstrap i build są zweryfikowane; testy funkcjonalne pozostają do napisania.
