# Dual-system summaries — Plan Brief

> Pełny plan: `context/changes/prepare-dual-system-summaries/plan.md`

## What & Why

Przygotować osobne zestawienia zwykłego dnia dla ręcznego przeniesienia do Jiry i MyWork. Aplikacja obliczy różnicę czasu i przygotuje tekst, ograniczając ręczne przepisywanie.

## Starting Point

Tabela przechowuje osobne wpisy, pokazuje sumy i kolejność zapisu. Istniejące dane nie zawierają jeszcze podsumowań ani potwierdzeń transferu.

## Desired End State

Przy dniu o dokładnej sumie 7h 12m użytkownik otwiera okno z zakładkami Jira i MyWork. Widzi osobne wiersze, odpowiednie sumy i może kopiować ID oraz opisy; MyWork ma łącznie 8h. Niepełny lub przekroczony dzień nie udostępnia podsumowania.

## Key Decisions Made

| Decyzja | Wybór | Dlaczego |
| --- | --- | --- |
| Doliczenie | 48 minut do najdłuższego pojedynczego wpisu; remis wygrywa pierwszy zapisany | PRD i roadmapa określają tę regułę bez sumowania ticketów. |
| Tekst MyWork | ID i tytuł, opcjonalnie ` - ` i opis | Zgodność z US-01; pusty opis nie dodaje separatora. |
| Prezentacja | Akcja przy dacie, okno z zakładkami | Zachowuje kontekst wybranego dnia. |
| Niepełny dzień | Nieaktywny przycisk z wyjaśnieniem | Funkcja pozostaje widoczna, ale dostępna tylko przy dokładnej sumie. |
| Aktualność | Ponowny odczyt i walidacja przed otwarciem | Dane po błędzie odświeżania nie są prezentowane jako aktualne. |
| Schowek | Przyciski dla niepustych Jira ID i opisów; `Copied` lub błąd, tekst zaznaczalny | Ułatwia wklejanie i pozostawia ręczną alternatywę. |

## Scope

**In scope:** logika i formaty obu podsumowań, dostępne UI, kopiowanie wybranych pól, lokalne testy oraz odbiór autora.

**Out of scope:** automatyczne transfery, potwierdzenia, zaległości, urlopy i święta, migracja danych oraz odłożona bramka CI F-01.

## Architecture / Approach

Czysta logika Core przelicza aktualnie odczytane wpisy wybranej daty. Komponent aplikacji pokazuje wynik w dialogu i obsługuje schowek; żadne wyniki nie są trwale zapisywane ani wysyłane do systemów zewnętrznych.

## Phases at a Glance

| Faza | Rezultat | Główne ryzyko |
| --- | --- | --- |
| 1. Logika podsumowań | Projekcja i testy reguł | Niewłaściwe rozstrzygnięcie remisu lub grupowanie wpisów. |
| 2. Interfejs i kopiowanie | Dialog, ponowny odczyt i schowek | Nieaktualne dane lub niedostępna interakcja. |
| 3. Weryfikacja desktopowa i odbiór | Testy rzeczywistego EXE i dowody | Środowisko WebView2 lub pakowanie może zablokować lokalny test. |

**Prerequisites:** ukończone S-08, .NET 10, workload Windows i WebView2; wcześniejszy EXE wymagany przez istniejący zestaw UiTests.
**Estimated effort:** około 2–3 sesji w trzech fazach; zależy od lokalnego środowiska desktopowego.

## Open Risks & Assumptions

- Zestawienie dotyczy obecnie zwykłych dni; S-05 osobno wprowadzi typy dni i wyjątek dla MyWork.
- Lokalny wynik desktopowy nie potwierdza odłożonej bramki CI ani zgodności na czystym urządzeniu.

## Success Criteria (Summary)

- US-01 pokazuje w Jira 1h i 6h 12m, a w MyWork 1h i 7h; powtórzone tickety pozostają osobnymi wpisami.
- Podsumowanie jest dostępne tylko dla 7h 12m, po ponownym odczycie, a wybrane wartości można skopiować.
- Testy jednostkowe i rzeczywistego desktopu przechodzą, a autor potwierdza odbiór na zidentyfikowanym pakiecie.
