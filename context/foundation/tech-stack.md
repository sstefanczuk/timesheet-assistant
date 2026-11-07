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

## Test strategy

Decyzja użytkownika z 2026-11-07: dwa osobne projekty xUnit, bez projektu IntegrationTests.

- `src/TimesheetAssistant.UnitTests/`: czysta logika, m.in. przeliczenia czasu, walidacja i reguły potwierdzeń. Bez Playwright, WebView2, uruchamiania EXE i rzeczywistego magazynu danych.
- `src/TimesheetAssistant.UiTests/`: pełne przepływy przez rzeczywistą aplikację Windows i WebView2 z Playwright .NET. Od S-01 wymagane są jawne scenariusze zapisu, zamknięcia, ponownego uruchomienia i odczytu na odizolowanym magazynie. Współpracę komponentów sprawdzamy przez UI, bez osobnego zestawu IntegrationTests.

CI uruchamia UnitTests przed pakowaniem, UiTests po publikacji aplikacji, przez osobne ścieżki csproj zamiast filtrów kategorii. UnitTests nie wymaga EXE; UiTests wymaga Windows, WebView2 i wskazanego pakietu. Wolniejsze wykonanie i trudniejszą diagnozę testów UI akceptujemy dla skali MVP; ich obecność nie zastępuje jawnego planowania scenariuszy trwałości i korekt.

Podział zostanie wdrożony w F-01: istniejący pusty TimesheetAssistant.Tests zostanie przemianowany na TimesheetAssistant.UnitTests, a TimesheetAssistant.UiTests zostanie dodany. F-01 używa tymczasowo Counter; S-01 zastąpi ten scenariusz zapisywaniem pracy i weryfikacją trwałości danych.
