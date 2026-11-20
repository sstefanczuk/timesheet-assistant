---
date: 2026-11-20T16:22:39+01:00
researcher: Codex
git_commit: 6d1324e9992d72e13ac159ae3ee13ec6a6437fa0
branch: main
repository: 10xDevs-project
topic: "Styl UI oraz przepływ historii i dodawania wpisów"
tags: [research, codebase, ui, workdays]
status: complete
last_updated: 2026-11-20
last_updated_by: Codex
---

# Research: Styl UI oraz przepływ historii i dodawania wpisów

## Pytanie badawcze

Jak obecny interfejs, przepływ historii i edycji oraz testy wpływają na przygotowanie zmiany stylu, kolorystyki i UX przeglądania oraz dodawania wpisów na jednej stronie?

## Podsumowanie

Historia i edytor już znajdują się na stronie głównej, ale są osobnymi sekcjami w pionowym przepływie. Zmiana dotyczy więc sposobu ich organizacji i interakcji, a nie początkowego połączenia osobnych stron ([WorkdayPage.razor:1](../../../src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor#L1), [WorkdayPage.razor:123](../../../src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor#L123)). Konkretna kompozycja, styl i paleta pozostają decyzjami produktu. Aktualna implementacja chroni niezapisane dane i odświeża historię po udanych zmianach; te zachowania należy uwzględnić w planowaniu.

## Ustalenia

### Układ i styl

- `MainLayout` umieszcza nawigację boczną obok zawartości; w sprawdzonym CSS przy szerokości od 900 px pasek ma 220 px, a poniżej układ jest pionowy ([MainLayout.razor:3](../../../src/TimesheetAssistant/Components/Layout/MainLayout.razor#L3), [MainLayout.razor.css:1](../../../src/TimesheetAssistant/Components/Layout/MainLayout.razor.css#L1)). Nawigacja wskazuje bieżący widok „Workday and history” ([NavMenu.razor:1](../../../src/TimesheetAssistant/Components/Layout/NavMenu.razor#L1)).
- WebView ładuje lokalny Bootstrap, globalny `app.css` i style izolowane ([index.html:8](../../../src/TimesheetAssistant/wwwroot/index.html#L8)). Obecne kolory przycisku, odsyłaczy i fokusu są w globalnym CSS, a ciemnoniebieskie tło nawigacji w CSS układu ([app.css:5](../../../src/TimesheetAssistant/wwwroot/app.css#L5), [MainLayout.razor.css:3](../../../src/TimesheetAssistant/Components/Layout/MainLayout.razor.css#L3)). Style historii i edytora obejmują m.in. odstępy, zawijanie opisów i układ sum ([WorkEntryHistory.razor.css:1](../../../src/TimesheetAssistant/Features/Workdays/WorkEntryHistory.razor.css#L1), [WorkdayPage.razor.css:1](../../../src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor.css#L1)).

### Historia i edytor

- Na trasie `/` kolejność znaczników to sekcja dzisiejsza, edytor wybranego dnia i wcześniejsze dni ([WorkdayPage.razor:1](../../../src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor#L1), [WorkdayPage.razor:11](../../../src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor#L11), [WorkdayPage.razor:123](../../../src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor#L123)). Przy wspólnym wyświetlaniu dzisiejszych wpisów komponent odsyła do listy w edytorze zamiast jej powielać ([WorkEntryHistory.razor:32](../../../src/TimesheetAssistant/Features/Workdays/WorkEntryHistory.razor#L32)).
- Edytor zawiera datę, pola Jira ID, tytuł, opcjonalny opis i czas, zapis oraz listę z edycją i usuwaniem ([WorkdayPage.razor:15](../../../src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor#L15), [WorkdayPage.razor:60](../../../src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor#L60), [WorkdayPage.razor:93](../../../src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor#L93)). Historia wyświetla wpisy w tabelach i pozwala wybrać wpis do edycji ([WorkEntryHistory.razor:64](../../../src/TimesheetAssistant/Features/Workdays/WorkEntryHistory.razor#L64)).
- W sprawdzonych ścieżkach przejście do innego kontekstu przez `WithDiscardAsync` wymaga potwierdzenia przy zmienionym formularzu; po udanym zapisie historia jest ponownie odczytywana ([WorkdayPage.razor:373](../../../src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor#L373), [WorkdayPage.razor:418](../../../src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor#L418)). Błąd odświeżania przy istniejącym obrazie danych zachowuje dotychczasowe wpisy i pozycję historii ([WorkdayPage.razor:346](../../../src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor#L346)).
- PRD wymaga lokalnego zachowania danych i historii oraz przewiduje wybór dni z wpisami dla przyszłego kopiowania ([prd.md:95](../../foundation/prd.md#L95), [prd.md:120](../../foundation/prd.md#L120)). Roadmapa umieszcza zmianę S-09 przed kopiowaniem S-08 ([roadmap.md:57](../../foundation/roadmap.md#L57)).

### Weryfikacja i zależności testów

- Istnieją scenariusze rzeczywistego UI dotyczące pustej i krótkiej historii, stronicowania, edycji, niezapisanego formularza, restartu oraz błędu odczytu ([WorkEntryHistoryTests.cs:13](../../../src/TimesheetAssistant.UiTests/Features/Workdays/WorkEntryHistoryTests.cs#L13), [WorkEntryHistoryTests.cs:60](../../../src/TimesheetAssistant.UiTests/Features/Workdays/WorkEntryHistoryTests.cs#L60), [WorkEntryHistoryTests.cs:147](../../../src/TimesheetAssistant.UiTests/Features/Workdays/WorkEntryHistoryTests.cs#L147)). Osobny scenariusz sprawdza trwałość i mutacje wpisów przez uruchomienia ([WorkdayRecordingTests.cs:13](../../../src/TimesheetAssistant.UiTests/Features/Workdays/WorkdayRecordingTests.cs#L13)).
- Pomocnik testów zależy od dostępnych nazw regionów, struktury sekcji, wierszy i komórek tabeli; zmiana znaczników lub nazw może wymagać aktualizacji selektorów przy zachowaniu sprawdzanych zachowań ([WorkEntryHistoryScreen.cs:9](../../../src/TimesheetAssistant.UiTests/Features/Workdays/WorkEntryHistoryScreen.cs#L9), [WorkEntryHistoryScreen.cs:50](../../../src/TimesheetAssistant.UiTests/Features/Workdays/WorkEntryHistoryScreen.cs#L50)). Testów nie uruchamiano: badanie dotyczyło kodu i istniejącego pokrycia, a nie bieżącego wyniku wykonania.

## Kontekst historyczny i ograniczenia

S-07 został oznaczony jako ukończony w roadmapie, która opisuje dzisiejszy dzień, wcześniejsze dni i doładowanie historii ([roadmap.md:110](../../foundation/roadmap.md#L110)). To opis zakresu i stanu odbioru, a nie dowód wizualnej jakości nowego projektu. Nie przeprowadzono wizualnej inspekcji działającej aplikacji; powyższe ustalenia o układzie pochodzą ze źródeł. Delegowane badania nie miały dostępu do plików; zakres sprawdzono lokalnie.

## Otwarte decyzje

- Jaki charakter wizualny, paleta, typografia i poziom gęstości informacji odpowiadają użytkownikowi?
- Jak ma wyglądać docelowa relacja historii, wybranej daty i formularza oraz jak rozpoczynać i kończyć edycję?
- Jak potwierdzić projekt na różnych rozmiarach okna oraz zachować dostępność klawiatury, stanów błędów i przyszłe kopiowanie S-08?
