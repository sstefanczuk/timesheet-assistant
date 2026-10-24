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
