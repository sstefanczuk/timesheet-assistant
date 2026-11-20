# Redesign entry experience — Plan Brief

> Full plan: [plan.md](plan.md)  
> Research: [research.md](research.md)

## What & Why

Zmiana nadaje aplikacji zwarty, techniczny styl i upraszcza codzienną pracę z wpisami. Historia stanie się głównym obszarem, a formularz będzie otwierany w oknie dialogowym. Menu i nagłówek strony otrzymają nazwę `Timesheet`, zbędny opis zniknie, a tekst i nagłówki będą znacznie mniejsze.

## Starting Point

Historia, dzisiaj i edytor są już na stronie głównej, ale występują jako odrębne sekcje. Istnieją usługi lokalnego zapisu, testy rzeczywistego UI i przyrostowe ładowanie historii.

## Desired End State

Jedna tabela pokazuje dni od najnowszego, sumy i wpisy w kolejności ich utworzenia. Formularz dodawania i edycji otwiera się centralnie; aplikacja oferuje ciemny i jasny motyw, a wybór zostaje zapamiętany. Niezapisany nowy wpis można przywrócić w bieżącej sesji.

## Key Decisions Made

| Decision | Choice | Why | Source |
| --- | --- | --- | --- |
| Styl | Zwarty, techniczny; ciemnoszary i jasnopomarańczowy, opcjonalnie biały z pomarańczem | Preferencja autora i czytelność. | Plan |
| Typografia i nazwa | Znacznie mniejszy czytelny tekst i nagłówki; `Timesheet` w menu i nagłówku, bez opisu celu dnia | Zwarty interfejs zgodnie z preferencją autora. | Plan |
| Komponenty UI | Blazor, HTML i CSS bez nowej biblioteki komponentów | Decyzja autora; dialog i fokus wymagają własnej weryfikacji. | Plan |
| Motyw | Domyślnie ciemny; zapamiętanie wyboru | Zachować preferencję po restarcie. | Plan |
| Historia | Jedna tabela; dzisiaj tylko w tabeli, grupy z sumami | Autor wybrał spójny widok; aktualne PRD i road map wymagają synchronizacji. | Plan |
| Formularz | Centralny dialog, zamknięcie po udanym zapisie | Historia pozostaje głównym widokiem. | Plan |
| Data | Ostatnio wybrana w sesji, początkowo dzisiaj; edytowalna | Przyspiesza wprowadzanie kolejnych wpisów. | Plan |
| Szkic | Tylko dodawanie; automatyczny powrót do zamknięcia aplikacji | Pozwala przerwać dodawanie bez trwałego przechowywania. | Plan |
| Edycja | Zamknięcie odrzuca zmiany bez ostrzeżenia | Jawna decyzja autora. | Plan |
| Trwałość i historia | Zachować istniejące usługi i przyrostowe ładowanie | Dane i historia pozostają dostępne. | Research |

## Scope

**In scope:** motywy, tabela, dialog, szkic dodawania w sesji, aktualizacja wymagań produktu i testy rzeczywistego UI.

**Out of scope:** S-08, pozostałe nowe funkcje produktu, trwałe szkice, migracja danych, nowa zewnętrzna biblioteka komponentów UI oraz desktopowe CI F-01.

## Architecture / Approach

Warstwa Razor prezentuje istniejące dane z usług historii i wpisów; wspólne style obsługują motywy. Preferencja motywu jest lokalna i trwała, a szkic dodawania pozostaje w pamięci aplikacji. Błędy zachowują dostępne dane i możliwość ponowienia operacji.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Motywy i fundament wizualny | Spójne style i trwała preferencja | Kontrast i czytelność w obu trybach. |
| 2. Tabela i formularz | Nowy przepływ i szkic sesji | Regresja CRUD, historii lub stanu formularza. |
| 3. Weryfikacja i odbiór | Testy oraz dowody odbioru | Środowisko Windows/WebView2 i dostępność autora. |

**Prerequisites:** .NET 10, workload MAUI Windows, WebView2 i lokalna publikacja Release do testów UI.
**Estimated effort:** średnia zmiana, trzy fazy; czas zależy od iteracji wizualnej i odbioru.

## Open Risks & Assumptions

- Nie wykonano wizualnej inspekcji działającej aplikacji w ramach badania; szczegóły wyglądu wymagają odbioru autora.
- Dzisiejszy dzień wyłącznie w tabeli zmienia dotychczasowe wymaganie osobnej prezentacji; plan aktualizuje bieżące PRD i roadmapę, zachowując historię S-07.

## Success Criteria (Summary)

- Autor akceptuje zwarty interfejs i oba motywy, a wybór motywu przetrwa restart.
- Tabela i formularz realizują uzgodniony przepływ, w tym szkic dodawania i odrzucanie zmian edycji.
- Wpisy i historia pozostają trwałe, a lokalne testy rzeczywistej aplikacji i odbiór są udokumentowane.
