# Sprawdzalna ścieżka uruchomienia aplikacji — Plan Brief

> Full plan: [plan.md](plan.md)

## What & Why

F-01 dostarcza automatyczny test rzeczywistej aplikacji Windows, potrzebny do sprawdzania przyszłych funkcji MVP. Test uruchamia aplikację Release i obsługuje jej WebView2 przez Playwright .NET.

## Starting Point

Istnieją starter MAUI Blazor Hybrid, pusty projekt xUnit i workflow publikujący pakiet Windows. Pakiet sprawdzono ręcznie na komputerze autora; CI nie uruchamia jeszcze interfejsu.

## Desired End State

Test rozpoznaje Home, przechodzi do Counter i sprawdza zmianę licznika 0 → 1 lokalnie oraz na windows-2025. Ma własny profil, cleanup i diagnostykę. Niepowodzenie blokuje wydanie; puste discovery nie oznacza sukcesu.

## Key Decisions Made

| Decision | Choice | Why | Source |
| --- | --- | --- | --- |
| Podział testów | Osobne UnitTests i UiTests; bez IntegrationTests | Niezależne zależności i uruchamianie | Wybór użytkownika 2026-11-07 |
| Narzędzie | Playwright .NET z rzeczywistym WebView2 | Sprawdza aplikację Hybrid | Tech-stack |
| Scenariusz | Tymczasowo Home → Counter → 0 → 1 | Dowodzi działania automatyzacji bez dodawania funkcji | Plan, potwierdzone przez użytkownika |
| Następca scenariusza | S-01 zastępuje Counter testem zapisywania pracy | Starter nie jest docelowym produktem | Plan, potwierdzone przez użytkownika |
| Trwałość danych | Osobno w S-01 | F-01 weryfikuje uruchomienie i interakcję | Roadmapa |
| Problem runnera | Zatrzymać odbiór i uzgodnić dalszy krok | Nie deklarować niepotwierdzonego CI | Plan, wybór użytkownika |
| Publikacja testowa | Istniejący EXE Release z folderu publish | Unika ponownej publikacji dla testu | Badanie CI |

## Scope

**In scope:**

- Fixture, izolacja profilu WebView2, limity oczekiwania i cleanup.
- Przemianowanie pustego Tests na UnitTests oraz dodanie UiTests.
- Jeden test UI i diagnostyka błędów.
- CI, raporty TRX i aktualizacja dokumentacji.

**Out of scope:**

- Wpisy pracy, baza danych i trwałość po restarcie.
- Weather, docelowe utrzymywanie testów startera i osobna aplikacja przeglądarkowa.
- Własny runner oraz test czystego urządzenia.

## Architecture / Approach

xUnit uruchamia wskazany EXE z unikalnym profilem i lokalnym endpointem CDP, po czym Playwright obsługuje stronę WebView2. Ustawienia trafiają wyłącznie do procesu testowanej aplikacji. UnitTests sprawdza czystą logikę bez Playwright i EXE; UiTests pełne przepływy. CI uruchamia UnitTests przed pakowaniem, UiTests po pakowaniu, przez osobne ścieżki projektów bez filtrów kategorii. Nie tworzymy IntegrationTests. Profil WebView2 nie izoluje przyszłej bazy — S-01 doda osobny katalog danych domenowych.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Uruchomienie i połączenie | Dwa projekty testowe, fixture i połączenie z Release | Inicjalizacja CDP i cykl życia procesów |
| 2. Test interakcji i diagnostyka | Home → Counter → 0 → 1, raporty | Ukryta nawigacja i błędy cleanup |
| 3. CI i dokumentacja | Test runnera i bramka wydania | Możliwość uruchomienia MAUI na windows-2025 |

**Prerequisites:** Windows, .NET 10 i maui-windows, WebView2 Evergreen; do dowodu CI dostęp do uruchomienia workflow repozytorium.
**Estimated effort:** Nie estymowano; zakres obejmuje trzy zatwierdzone fazy.

## Open Risks & Assumptions

- Wykonalność na runnerze jest wynikiem eksperymentu F-01, nie potwierdzonym faktem. Niepowodzenie zatrzymuje odbiór.
- Startup ma limit 60 s, operacje UI 15 s, cleanup 15 s; ewentualna korekta wymaga diagnozy.
- Brak dostępu do CI pozostawia kryteria CI niezweryfikowane.

## Success Criteria (Summary)

- Powtarzalny test rzeczywistego Release przechodzi lokalnie na oddzielnych profilach.
- CI wykonuje co najmniej jeden test desktopowy; błędy blokują wydanie i pozostawiają diagnostykę.
- Nie ma deklaracji trwałości danych ani docelowego pokrycia produktu; S-01 zastąpi Counter i jawnie sprawdzi zapis, restart oraz odczyt przez UI.

## References

- [Roadmapa](../../foundation/roadmap.md), [tech-stack](../../foundation/tech-stack.md).
- [Dokumentacja wdrożenia](../../deployment/deploy-plan.md).
- [Playwright .NET WebView2](https://playwright.dev/dotnet/docs/webview2).
