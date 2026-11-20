# Redesign entry experience — Implementation Plan

## Overview

Przeprojektować ekran wpisów jako zwarty, techniczny interfejs z tabelą historii, modalnym formularzem i przełączanymi motywami. Ułatwić przeglądanie i wprowadzanie pracy, zachowując lokalne dane oraz istniejące reguły domenowe.

## Current State Analysis

Trasa `/` wyświetla dzisiaj, edytor wybranego dnia i wcześniejszą historię jako odrębne sekcje ([WorkdayPage.razor](../../../src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor)); style wykorzystują Bootstrap, globalny CSS i izolowane arkusze. Historia i formularz współdziałają z usługami zapisu i odczytu; testy UI zależą od obecnej struktury tabel i nazw regionów. Szczegóły i ograniczenia zob. [research.md](research.md).

## Desired End State

Główna strona pokazuje jedną tabelę wpisów z datą, grupami dni i sumami. Dni sortowane są od najnowszego, wpisy w dniu według kolejności utworzenia; dzisiaj występuje tylko w tabeli bez specjalnego wyróżnienia. Jeden przycisk otwiera formularz dodawania w centralnym oknie; edycja uruchamiana jest z wpisu. Data początkowa dodawania to ostatnio wybrana w sesji, a przy pierwszym użyciu dzisiaj; można ją zmienić. Po udanym zapisie okno zamyka się, a tabela aktualizuje.

Szkic dodawania jest automatycznie zachowywany w pamięci do zamknięcia aplikacji i przywracany po ponownym otwarciu formularza; po zapisie zostaje wyczyszczony. Dane szkicu można zastąpić nowymi, bez osobnego przycisku odrzucenia. Edycja nie tworzy szkicu: zamknięcie okna odrzuca niezapisane zmiany bez ostrzeżenia. Błędy walidacji i zapisu pozostawiają formularz oraz dane dostępne do poprawy. Ciemny motyw szary z jasnopomarańczowym akcentem jest domyślny; jasny używa białego tła i pomarańczowego akcentu o czytelnym kontraście. Wybór motywu przetrwa restart.

### Key Discoveries

- Historia i edytor istnieją już na jednej stronie ([WorkdayPage.razor:11](../../../src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor#L11)).
- Bieżąca implementacja chroni niezapisane zmiany i zachowuje dane historii przy błędzie odświeżania ([WorkdayPage.razor:346](../../../src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor#L346)); reguła wyjścia z formularza jest świadomie zmieniana zgodnie z decyzją użytkownika.
- PRD i roadmapa przewidują osobne dzisiaj ([prd.md:78](../../foundation/prd.md#L78)); użytkownik wybrał zamiast tego dzisiaj wyłącznie w tabeli. Zaktualizować docelowe wymagania bez zmiany historycznych zapisów zakończonego S-07.

## What We're NOT Doing

- Kopiowanie wpisów S-08, zaległości, podsumowania i inne przyszłe funkcje.
- Zmiana schematu danych wpisów, migracja SQLite, integracje lub konta.
- Trwałe szkice, szkice edycji ani automatyczny zapis wpisu podczas zamknięcia okna.
- Desktopowa bramka CI F-01 lub edycja archiwum.
- Dodawanie zewnętrznej biblioteki komponentów UI.

## Implementation Approach

Zachować usługi domenowe, przeorganizować prezentację i stan formularza, a style oprzeć na wspólnych tokenach motywu. Budować interfejs w Blazorze, HTML i CSS, korzystając z istniejących zasobów projektu bez dodawania zewnętrznej biblioteki komponentów UI; dostępność dialogu i obsługę fokusu zweryfikować samodzielnie. Preferencję motywu przechowywać w lokalnych ustawieniach aplikacji, odrębnie od danych wpisów; szkic przechowywać w pamięci procesu. Odświeżanie historii, obsługę błędów, dostępność klawiatury i testy dostosować do nowego interfejsu. Zmianę wymagania dziś zsynchronizować z PRD i aktywną roadmapą jako decyzję S-09, zachowując historię S-07.

## Phase 1: Motywy i fundament wizualny

### Overview

Zbudować spójny techniczny styl i przełączanie motywów, ze znacznie mniejszą typografią oraz uproszczonym nagłówkiem strony.

### Changes Required

#### 1. Motywy i układ

**Files**: `src/TimesheetAssistant/wwwroot/app.css`, `src/TimesheetAssistant/Components/Layout/`, `src/TimesheetAssistant/MauiProgram.cs` oraz właściwe pliki funkcji ustawień.

**Intent**: Nadać aplikacji zwarty styl i umożliwić wybór motywu. Zmniejszyć tekst i nagłówki względem obecnego interfejsu, zachowując czytelność.

**Contract**: Ciemnoszary i jasny motyw z pomarańczowymi akcentami; ciemny przy braku zapisanej preferencji, zmiana dostępna w UI i zachowana po restarcie. Menu i główny nagłówek strony mają tekst `Timesheet`; usunąć zdanie `Record your work and track your daily target of 7h 12m.`. Zastosować wyraźnie mniejszą czcionkę tekstu i nagłówków w całym interfejsie, z czytelną hierarchią. Style obejmują interaktywne, walidacyjne, błędne i fokusowane stany oraz czytelny kontrast; obsłużyć wąskie okno i błąd odczytu preferencji bez blokowania aplikacji.

### Success Criteria

#### Automated Verification

- Rozwiązanie buduje się: `dotnet build TimesheetAssistant.sln`.
- Testy jednostkowe przechodzą: `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj`.

#### Manual Verification

- W opublikowanej aplikacji autor sprawdza oba motywy, nazwę `Timesheet` w menu i nagłówku, brak usuniętego opisu, wyraźnie mniejszą czytelną typografię, kontrast, fokus, wąskie i szerokie okno oraz zachowanie wybranego motywu po restarcie.

## Phase 2: Tabela i formularz

### Overview

Przekształcić obecny ekran w tabelę historii i centralny formularz, zachowując operacje i lokalną trwałość.

### Changes Required

#### 1. Historia i operacje

**Files**: `src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor`, `WorkEntryHistory.razor` i ich arkusze; powiązane komponenty funkcji.

**Intent**: Pokazać wpisy w jednym miejscu i umożliwić dodawanie, edycję oraz usuwanie bez osobnego edytora dnia.

**Contract**: Jedna tabela z kolumną daty, grupami i sumami, daty malejąco i wpisy w kolejności utworzenia; dzisiaj tylko w tabeli. Zachować pusty stan, doładowanie starszego dnia, odświeżanie, stany ładowania, retry i wcześniej załadowane dane przy błędzie. Zachować dostęp do dodawania dla dowolnej daty, operacje edycji/usuwania z potwierdzeniem usunięcia oraz sumę, brak lub przekroczenie celu dla odpowiedniego dnia. Nie usuwać danych z powodu upływu czasu.

#### 2. Okno i stan formularza

**Files**: `src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor`, komponent formularza/dialogu i ich style.

**Intent**: Uprościć wprowadzanie wpisów i umożliwić powrót do przerwanego dodawania.

**Contract**: Centralny dostępny dialog z etykietami, fokusem przy otwarciu i powrotem fokusu po zamknięciu; klawiatura i zamknięcie nie mogą zostawić fokusu za otwartym dialogiem. Jeden szkic dodawania obejmuje datę i pola, zachowuje się w pamięci sesji po zamknięciu i wraca automatycznie; udany zapis czyści go, a błędny pozostawia formularz. Pierwsza data w sesji to dzisiaj, następne to ostatnio wybrana data; zmiana daty dostępna w formularzu. Przy edycji zamknięcie odrzuca zmiany bez potwierdzenia i bez szkicu. Udany zapis zamyka dialog i odświeża dane; nieudany zachowuje dane i przedstawia błąd. Usunięcie wymaga potwierdzenia.

#### 3. Kontrakt produktu

**Files**: `context/foundation/prd.md`, `context/foundation/roadmap.md`.

**Intent**: Odzwierciedlić potwierdzoną zmianę sposobu pokazywania dzisiaj.

**Contract**: Zaktualizować aktualne wymagania i opisy docelowego przepływu: dzisiaj znajduje się w tabeli; zachować możliwość pracy dla dzisiaj oraz przyszłe wymagania zaległości. Historyczne opisy odbioru S-07 i archiwum pozostają niezmienione; odnotować datę i decyzję S-09.

### Success Criteria

#### Automated Verification

- Testy jednostkowe przechodzą: `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj`.
- Testy UI dla historii, dodawania, edycji, usuwania, błędów i szkicu przechodzą w opublikowanej aplikacji przez `dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj --logger trx`.

#### Manual Verification

- Autor sprawdza dodawanie z datą, powrót do szkicu w sesji, utratę zmian po zamknięciu edycji, kolejność tabeli, sumy i doładowanie starszych dni.

## Phase 3: Weryfikacja i odbiór

### Overview

Sprawdzić pełny przepływ i zapisać dowody lokalnego odbioru.

### Changes Required

#### 1. Testy i dowody

**Files**: `src/TimesheetAssistant.UiTests/Features/Workdays/`, odpowiednie testy jednostkowe oraz `context/changes/redesign-entry-experience/verification.md`.

**Intent**: Chronić zachowanie produktu podczas zmiany interfejsu i udokumentować wynik.

**Contract**: Zaktualizować selektory bez usuwania istotnych scenariuszy; objąć oba motywy i ich trwałość, szkic w sesji i jego brak po restarcie, brak szkicu edycji, zapis/restart/odczyt, puste i długie dane, usunięcie, paging, błędy oraz dostępność klawiatury. Przed publikacją uruchomić UnitTests; po publikacji uruchomić UiTests na wskazanym Release EXE z izolowanymi danymi. Zapisać komendy, wyniki, środowisko i potwierdzenie autora lub jawne blokady.

### Success Criteria

#### Automated Verification

- Po publikacji testy UI rzeczywistej aplikacji przechodzą: `dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj --logger trx` z `TIMESHEET_ASSISTANT_TEST_APP` wskazującym opublikowany Release EXE.
- `dotnet build TimesheetAssistant.sln` i testy jednostkowe przechodzą; wyniki są zapisane w verification.md.

#### Manual Verification

- Autor akceptuje wygląd i przepływ w obu motywach, w szerokim i wąskim oknie, oraz potwierdza działanie klawiatury i zachowanie danych po restarcie; wynik zapisano w verification.md.

## Testing Strategy

Testy jednostkowe obejmują nową nietrywialną logikę stanu, o ile zostanie wydzielona; testy desktopowe sprawdzają rzeczywisty WebView2 i odizolowane dane. Zachować scenariusze CRUD, restartu, walidacji, obsługi awarii i historii, dostosowując oczekiwania do nowego UX. Ręcznie ocenić czytelność, kontrast, długie opisy, fokus oraz rozmiary okna.

## Performance Considerations

Nie ładować całej nieograniczonej historii przy starcie; zachować istniejące przyrostowe doładowywanie. Unikać utraty załadowanych dni przy błędzie.

## Migration Notes

Schemat wpisów pozostaje bez zmian. Brak preferencji motywu oznacza ciemny motyw; szkic nie jest migrowany ani przechowywany między sesjami. Odrębne wymaganie pokazywania dzisiaj jest zmieniane w aktualnym PRD i roadmapie, bez przepisywania historii S-07.

## References

- [Research](research.md)
- [PRD](../../foundation/prd.md)
- [Roadmap](../../foundation/roadmap.md)
- [Desktop UI test instructions](../../../src/TimesheetAssistant.UiTests/README.md)

## Progress

> `- [ ]` pending, `- [x]` done. Append commit SHA when a step lands.

### Phase 1: Motywy i fundament wizualny

#### Automated

- [ ] 1.1 Rozwiązanie buduje się: `dotnet build TimesheetAssistant.sln`.
- [ ] 1.2 Testy jednostkowe przechodzą: `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj`.

#### Manual

- [ ] 1.3 W opublikowanej aplikacji autor sprawdza oba motywy, nazwę `Timesheet` w menu i nagłówku, brak usuniętego opisu, wyraźnie mniejszą czytelną typografię, kontrast, fokus, wąskie i szerokie okno oraz zachowanie wybranego motywu po restarcie.

### Phase 2: Tabela i formularz

#### Automated

- [ ] 2.1 Testy jednostkowe przechodzą: `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj`.
- [ ] 2.2 Testy UI dla historii, dodawania, edycji, usuwania, błędów i szkicu przechodzą w opublikowanej aplikacji przez `dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj --logger trx`.

#### Manual

- [ ] 2.3 Autor sprawdza dodawanie z datą, powrót do szkicu w sesji, utratę zmian po zamknięciu edycji, kolejność tabeli, sumy i doładowanie starszych dni.

### Phase 3: Weryfikacja i odbiór

#### Automated

- [ ] 3.1 Po publikacji testy UI rzeczywistej aplikacji przechodzą: `dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj --logger trx` z `TIMESHEET_ASSISTANT_TEST_APP` wskazującym opublikowany Release EXE.
- [ ] 3.2 `dotnet build TimesheetAssistant.sln` i testy jednostkowe przechodzą; wyniki są zapisane w verification.md.

#### Manual

- [ ] 3.3 Autor akceptuje wygląd i przepływ w obu motywach, w szerokim i wąskim oknie, oraz potwierdza działanie klawiatury i zachowanie danych po restarcie; wynik zapisano w verification.md.
