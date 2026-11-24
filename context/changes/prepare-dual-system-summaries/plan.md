# Dual-system summaries Implementation Plan

## Overview

Przygotować podsumowania zwykłego dnia dla ręcznego przeniesienia do Jiry i MyWork, bez wysyłania danych do tych systemów.

## Current State Analysis

Tabela na stronie głównej grupuje wpisy według daty, pokazuje sumę i zapisuje osobne wpisy. `WorkdayTotals` określa cel 432 minut; SQLite przechowuje stabilne `creation_order` i odczytuje wpisy w tej kolejności. Nie ma jeszcze podsumowań ani potwierdzeń przeniesienia. Stan historii może być zachowany po błędzie odczytu, więc nie może samodzielnie stanowić dowodu aktualności podsumowania.

## Desired End State

Dla dnia o sumie dokładnie 7h 12m użytkownik otwiera okno z zakładkami Jira i MyWork. Jira zachowuje czasy wpisów, MyWork dolicza 48 minut do najdłuższego pojedynczego wpisu, przy remisie do pierwszego zapisanego. Oddzielne wpisy pozostają oddzielne. Dni niepełne i przekroczone nie udostępniają podsumowania.

### Key Discoveries:

- `src/TimesheetAssistant.Core/Features/Workdays/WorkEntry.cs` zawiera `CreationOrder`; `SqliteWorkEntryStore.cs` zachowuje go przy edycji i odczytuje według kolejności utworzenia.
- `src/TimesheetAssistant.Core/Features/Workdays/WorkdayTotals.cs` definiuje cel 432 minut, a `DurationParser.cs` formatuje czas zgodnie z przykładami PRD.
- `src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor` posiada grupy dni, istniejące okno dialogowe i obsługę błędów; `src/TimesheetAssistant/UI-PLAYBOOK.md` określa dostępność i motywy.
- PRD US-01 określa kolumny oraz format opisu MyWork: ID, spacja, tytuł i opcjonalnie ` - ` oraz opis.

## What We're NOT Doing

- Potwierdzenia i cofanie przeniesienia, zaległości, święta i urlopy: S-03–S-05.
- Integracje, logowanie do zewnętrznych systemów lub automatyczne przesyłanie danych.
- Zmiana schematu bazy, sumowanie wpisów według ticketu ani zmiana istniejących danych.
- Odblokowanie odłożonej bramki CI F-01 lub twierdzenie o zgodności na czystym urządzeniu.

## Implementation Approach

Wydzielić czystą logikę projekcji w Core, a w interfejsie przygotowywać podsumowanie z ponownie odczytanych danych wybranej daty. Widok wykorzystuje istniejące konwencje dialogów i stylów. Kopiowanie obsługuje adapter aplikacji i zwraca użytkownikowi wynik; dane źródłowe nie są modyfikowane.

## Phase 1: Logika podsumowań

### Overview

Określić i przetestować deterministyczną projekcję kompletnego zwykłego dnia.

### Changes Required:

#### 1. Projekcja i kontrakt

**File**: `src/TimesheetAssistant.Core/Features/Workdays/` (nowe typy podsumowania)

**Intent**: Udostępnić gotowe do prezentacji wiersze i sumy obu systemów, bez zależności od UI lub magazynu.

**Contract**: Przyjmować wpisy jednej daty; odrzucić pusty, mieszany lub niekompletny zestaw i każdy wynik inny niż 432 minuty. Zachować osobne wiersze w kolejności `CreationOrder`; maksymalny czas wybrać przed doliczeniem, przy remisie wybrać najmniejszy `CreationOrder`. Jira: oryginalne pola i czas; MyWork: `JiraId + " " + Title` oraz `" - " + Description` tylko przy niepustym opisie, czas zwycięzcy +48 minut. Sumy wynoszą 432 i 480 minut; wejście pozostaje niezmienione.

#### 2. Testy logiki

**File**: `src/TimesheetAssistant.UnitTests/Features/Workdays/` (nowe testy)

**Intent**: Zabezpieczyć reguły biznesowe i formatowanie na niezależnych danych.

**Contract**: Testować US-01, pusty opis, osobne wpisy tego samego ticketu, A 2h + 2h kontra B 3h 12m, remis przy danych przekazanych w innej kolejności, sumy poniżej i powyżej celu oraz brak mutacji wejścia; stosować Shouldly i konwencje `src/AGENTS.md`.

### Success Criteria:

#### Automated Verification:

- Testy jednostkowe projekcji i istniejący zestaw przechodzą: `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj`.

#### Manual Verification:

- Autor potwierdza, że oczekiwane wiersze US-01 i reguła remisu odpowiadają wymaganiom.

## Phase 2: Interfejs i kopiowanie

### Overview

Udostępnić podsumowania przy każdym załadowanym dniu i umożliwić kopiowanie wybranych pól.

### Changes Required:

#### 1. Otwieranie i aktualność

**File**: `src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor` i feature-local komponenty/styl

**Intent**: Powiązać akcję z datą i przed pokazaniem wyników odczytać aktualnie zapisane wpisy.

**Contract**: Przycisk przy nagłówku dnia jest widoczny, ale nieaktywny przy sumie innej niż 432 minuty, z dostępnym wyjaśnieniem. Przy aktywacji odczytać datę przez istniejącą usługę, ponownie zweryfikować sumę; błąd odczytu lub zmieniona suma nie otwiera wyników i pokazuje wyjaśnienie. Okno wskazuje datę, ma osobne zakładki Jira/MyWork, kolumny i sumy z PRD, zamykanie i obsługę klawiatury/fokusu zgodnie z playbookiem. Po zamknięciu i ponownym otwarciu zawsze ponowić odczyt.

#### 2. Kopiowanie i dostępność

**File**: `src/TimesheetAssistant/Features/Workdays/` i istniejąca infrastruktura aplikacji/schowka

**Intent**: Ułatwić wklejenie danych do konkretnych pól zewnętrznych systemów.

**Contract**: Kopiować wyłącznie niepuste Jira ID i opisy: w Jira osobno ID i opis pracy, w MyWork złożony opis. Tytuły i czasy są widoczne i zaznaczalne, bez przycisków kopiowania. Sukces oznaczyć krótkim dostępnym komunikatem `Copied`; błąd zgłosić i pozostawić tekst do ręcznego zaznaczenia. Nie kopiować pustego opisu ani nie wysyłać danych poza schowek.

#### 3. Konwencje UI

**File**: `src/TimesheetAssistant/UI-PLAYBOOK.md` i feature-local CSS

**Intent**: Zachować spójność wyglądu i interakcji w obu motywach oraz przy wąskim oknie.

**Contract**: Wykorzystać istniejące tokeny, semantyczne przyciski i dostępne zakładki; dokumentować nową uzgodnioną konwencję, jeśli zostanie wprowadzona.

### Success Criteria:

#### Automated Verification:

- Rozwiązanie buduje się: `dotnet build TimesheetAssistant.sln`.
- Testy jednostkowe przechodzą: `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj`.

#### Manual Verification:

- Autor sprawdza klawiaturę, fokus, oba motywy i wąskie okno oraz czytelność nieaktywnej akcji i komunikatów schowka.

## Phase 3: Weryfikacja desktopowa i odbiór

### Overview

Sprawdzić rzeczywistą aplikację na izolowanych danych i zapisać dowody wraz z odbiorem autora.

### Changes Required:

#### 1. Scenariusze aplikacji

**File**: `src/TimesheetAssistant.UiTests/Features/Workdays/` (nowe scenariusze i rozszerzenia screen object)

**Intent**: Zweryfikować cały przepływ z rzeczywistym EXE, SQLite i WebView2.

**Contract**: Testy zapisują dane przez UI, sprawdzają dokładne wiersze i sumy US-01, osobne wpisy, remis, blokadę przy sumach poniżej i powyżej celu, oba widoki oraz ponowne otwarcie po edycji i restarcie. Sprawdzają odpowiednie wartości kopiowania i dostępne komunikaty oraz zachowanie błędu odczytu i schowka, tam gdzie możliwe przez kontrolowany testowy mechanizm; nie polegają na globalnym profilu debugowania. Zachować izolację scenariuszy i istniejące regresje.

#### 2. Dowody

**File**: `context/changes/prepare-dual-system-summaries/verification.md`

**Intent**: Pozostawić możliwy do sprawdzenia wynik uruchomień i ręcznego odbioru.

**Contract**: Zapisać wersję i źródło pakietu, komendy, wyniki i liczbę testów z TRX, ścieżki diagnostyki, ewentualne blokady oraz potwierdzenie autora; osobno zaznaczyć nieweryfikowane CI i czyste urządzenie.

### Success Criteria:

#### Automated Verification:

- Release build i UnitTests przechodzą przed pakowaniem według `src/TimesheetAssistant.UiTests/README.md`.
- Wszystkie UiTests przechodzą na nowym opublikowanym EXE z wymaganym poprzednim EXE; TRX zawiera wykonane testy i brak błędów.

#### Manual Verification:

- Autor na wskazanym pakiecie sprawdza US-01, kopiowanie, ponowne otwarcie po edycji, klawiaturę i oba motywy oraz potwierdza odbiór w verification.md.

## Testing Strategy

### Unit Tests:

Sprawdzić projekcję, granice sumy, format opisu, powtórzone tickety, kolejność i remis bez rzeczywistego magazynu.

### Integration Tests:

Nie tworzyć osobnego projektu; rzeczywiste przepływy i awarie magazynu należą do UiTests na izolowanym pakiecie.

### Manual Testing Steps:

1. Wprowadzić dane US-01, obejrzeć obie zakładki i wkleić kopiowane wartości.
2. Zmienić czas lub opis, ponownie otworzyć i sprawdzić aktualne wyniki; sprawdzić blokadę dla niepełnej i przekroczonej sumy.
3. Sprawdzić sterowanie klawiaturą, fokus, wąskie okno i jasny oraz ciemny motyw.

## Performance Considerations

Jednorazowy lokalny odczyt dnia i liniowa projekcja odpowiadają przewidywanym 2–5 wpisom dziennie; brak uzgodnionego liczbowego limitu opóźnienia.

## Migration Notes

Nie jest wymagana migracja: podsumowania są wyliczane z istniejących wpisów i nie zmieniają schematu ani danych. Wycofanie funkcji pozostawia dane nienaruszone.

## References

- `context/foundation/prd.md` — US-01, FR-007, FR-008 i reguły biznesowe.
- `context/foundation/roadmap.md` — S-02 i zależności.
- `src/TimesheetAssistant/Features/Workdays/SqliteWorkEntryStore.cs` — kolejność zapisu i odczytu.
- `src/TimesheetAssistant.UiTests/README.md` — pakowanie i desktopowa weryfikacja.

## Progress

### Phase 1: Logika podsumowań

#### Automated

- [x] 1.1 Testy jednostkowe projekcji i istniejący zestaw przechodzą: `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj`.

#### Manual

- [ ] 1.2 Autor potwierdza, że oczekiwane wiersze US-01 i reguła remisu odpowiadają wymaganiom.

### Phase 2: Interfejs i kopiowanie

#### Automated

- [ ] 2.1 Rozwiązanie buduje się: `dotnet build TimesheetAssistant.sln`.
- [ ] 2.2 Testy jednostkowe przechodzą: `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj`.

#### Manual

- [ ] 2.3 Autor sprawdza klawiaturę, fokus, oba motywy i wąskie okno oraz czytelność nieaktywnej akcji i komunikatów schowka.

### Phase 3: Weryfikacja desktopowa i odbiór

#### Automated

- [ ] 3.1 Release build i UnitTests przechodzą przed pakowaniem według `src/TimesheetAssistant.UiTests/README.md`.
- [ ] 3.2 Wszystkie UiTests przechodzą na nowym opublikowanym EXE z wymaganym poprzednim EXE; TRX zawiera wykonane testy i brak błędów.

#### Manual

- [ ] 3.3 Autor na wskazanym pakiecie sprawdza US-01, kopiowanie, ponowne otwarcie po edycji, klawiaturę i oba motywy oraz potwierdza odbiór w verification.md.
