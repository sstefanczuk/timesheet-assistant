---
project: "Timesheet Assistant"
version: 1
status: draft
created: 2026-10-18
context_type: greenfield
product_type: desktop
target_scale:
  users: small
  qps: "Niska intensywność: pojedyncze wpisy w ciągu dnia; sporadycznie uzupełnienie całego tygodnia. Bez liczbowego założenia QPS."
  data_volume: "2–5 wpisów dziennie na użytkownika; historia bez limitu czasu."
timeline_budget:
  mvp_weeks: 5
  hard_deadline: 2026-12-06
  after_hours_only: true
---

## Vision & Problem Statement

Timesheet Assistant rozwiązuje codzienne tarcie związane z rozproszonym i powielanym rejestrowaniem czasu. W Jirze właściwe miejsca logowania znajdują się na boardzie, w Epicach oraz specjalnych taskach. Następnie numer i tytuł ticketa, przepracowany czas oraz czasem opis pracy trzeba ręcznie przenieść do MyWork, oczyszczając przy tym skopiowany tekst z niepotrzebnych znaków. Przy odkładaniu rozliczenia rośnie trudność odtworzenia wykonanej pracy i ryzyko błędów.

Istotą problemu jest to, że bieżący board nie obejmuje wszystkich miejsc, w których rejestrowana jest praca, a te same informacje muszą zostać ponownie wprowadzone w drugim systemie. Perspektywa większej skali: także przy ewentualnych 100 użytkownikach każdy rozlicza własny czas niezależnie; wspólne dane nie są planowane. MVP nadal służy wyłącznie autorowi.

## User & Persona

Primary persona: autor aplikacji — kontraktor rozliczający własny czas w systemie Jira/Clockwork Pro klienta oraz w systemie MyWork kontraktorni. Potrzebuje aplikacji podczas bieżącego rejestrowania cząstkowych zadań, najpóźniej pod koniec dnia lub rano następnego dnia. Pierwsza wersja służy wyłącznie tej osobie. Możliwe udostępnienie aplikacji kolegom pozostaje przyszłym kierunkiem poza MVP.

## Success Criteria

### Primary

- Autor korzysta z aplikacji do codziennego rozliczania czasu przez jeden tydzień: zapisuje zadania, przygotowuje zestawienia dla obu systemów i potwierdza ich ręczne przeniesienie.
- Rozliczony dzień roboczy obejmuje 7 h 12 min w Jirze oraz 8 h w MyWork. Święto lub urlop obejmuje 7 h 12 min na specjalnym tasku Jiry i nie wymaga wpisu w MyWork.

### Secondary

- Brak dodatkowego kryterium sukcesu — świadoma decyzja użytkownika.

### Guardrails

- Zapisane wpisy i wyniki są zachowane i dostępne przy kolejnym otwarciu aplikacji, z uwzględnieniem świadomych edycji, usunięć i potwierdzonego zastąpienia wpisów urlopem.
- Aplikacja pokazuje dni wymagające uzupełnienia; dzień pozostaje zaległy do potwierdzenia przeniesienia wymaganych wpisów, również po wyjściu poza domyślne okno dwóch tygodni. Dni rozliczone wcześniej poza aplikacją można oznaczyć jako rozliczone bez odtwarzania wpisów. Dzisiaj jest pokazywane osobno jako dzień w toku.

## User Stories

### US-01: Przygotowanie podsumowań zwykłego dnia dla Jiry i MyWork

- **Given** użytkownik ma zwykły dzień roboczy bez wpisów.
- **When** dodaje „Refinement” — 1 h na T-1 Scrum Ceremonies z opisem „Refinement” oraz „X-1 Order Page” — 6 h 12 min bez opisu pracy.
- **Then** widzi podsumowanie Jiry z tymi dwoma wpisami i sumą 7 h 12 min oraz podsumowanie MyWork z czasami odpowiednio 1 h i 7 h, o sumie 8 h.

#### Acceptance Criteria

- Po pierwszym wpisie suma dnia wynosi 1 h, a do 7 h 12 min pozostaje 6 h 12 min; po drugim wpisie pozostaje 0 min.
- Podsumowanie Jiry zawiera kolumny Jira ID, Item title, Work description, Time oraz następujące wiersze:

| Jira ID | Item title | Work description | Time |
| --- | --- | --- | --- |
| T-1 | Scrum Ceremonies | Refinement | 1h |
| X-1 | Order Page | | 6h 12m |

- Podsumowanie MyWork zawiera kolumny Work description, Time oraz następujące wiersze:

| Work description | Time |
| --- | --- |
| T-1 Scrum Ceremonies - Refinement | 1h |
| X-1 Order Page | 7h |

- W tym przykładzie opis MyWork łączy ID i tytuł ticketa oraz, jeśli podano opis pracy, dodaje go po „ - ”. Pusty opis nie dodaje separatora.
- Czas Refinement pozostaje niezmieniony, a różnica 48 min między sumami systemów trafia w całości do X-1, ponieważ to zadanie ma najdłuższy zalogowany czas w tym dniu.

## Functional Requirements

Must-have obejmuje opisany przepływ MVP. Pobieranie ticketów z Jiry jest opcjonalnym rozszerzeniem poza MVP.

### Dni i zaległości

Po otwarciu aplikacji użytkownik widzi zaległe dni do uzupełnienia, od najstarszego do wczoraj. Okres zaczyna się w poniedziałek poprzedniego tygodnia i obejmuje poprzedni oraz aktualny tydzień. Standardowo rozliczane są dni poniedziałek–piątek. Dzisiejszy dzień jest pokazywany osobno jako dzień w toku i pozwala dodawać bieżące wpisy. Dni rozliczone wcześniej w systemach zewnętrznych można oznaczyć jako rozliczone bez odtwarzania wpisów.

- FR-001: User can view outstanding weekdays from the previous and current week, plus older unresolved days, ordered from oldest to newest, and mark previously settled days as settled without recreating their entries. Priority: must-have
  > Socratic: Kontrargument: przy pierwszym uruchomieniu dni rozliczone wcześniej w obu systemach mogą wyglądać na zaległe. Decyzja: pozwolić oznaczyć dzień jako rozliczony bez odtwarzania wpisów.
- FR-002: User can select an outstanding day or today to record work incrementally, with today displayed separately as a day in progress. Priority: must-have
  > Socratic: Kontrargument: niepełny dzisiejszy dzień jest normalną pracą w toku, więc pokazywanie go jako zaległości byłoby mylące. Decyzja: dzisiaj pokazywać osobno jako dzień w toku.
- FR-003: User can manually change an individual day's type to a holiday or leave day, requiring 7 h 12 min on a special Jira task and no MyWork entry, with existing work entries replaced by a leave entry only after a warning and explicit confirmation. Priority: must-have
  > Socratic: Kontrargument: zmiana na urlop mogłaby przypadkowo zastąpić istniejące wpisy pracy. Decyzja: zastąpić wpisy wpisem urlopowym dopiero po ostrzeżeniu i potwierdzeniu.

### Wpisy i szablony

Aplikacja wygodnie podpowiada zadania do zapisania. Przykłady podane przez użytkownika: Refinement — 1 h, T-1 Scrum Ceremonies, opis „Refinement”; Sprint Planning — 30 min, ten sam T-1, opis „Sprint Planning”; CAB Call — 30 min, T-2 Internal Communication, opis „CAB call”.

- FR-004: User can create, edit and delete reusable templates with a Jira task, work description and duration, and use them to add time entries without later template changes affecting existing entries. Priority: must-have
  > Socratic: Kontrargument: edycja lub usunięcie szablonu mogłyby zmienić wcześniej rozliczone dni. Decyzja: istniejące wpisy pozostają bez zmian; szablon wpływa tylko na nowe wpisy.
- FR-005: User can manually add time entries using a task ID, title, duration and optional work description, keep repeated entries for the same task separate with their own times and descriptions, and edit or delete saved entries even after transfer confirmation. Priority: must-have
  > Socratic: Kontrargument: automatyczne łączenie kolejnych wpisów do tego samego ticketa mogłoby zatrzeć szczegóły pracy. Decyzja: zachować osobne wpisy z własnym czasem i opisem.
- FR-006: User can copy selected entries from the three most recent earlier days containing entries and load one additional older day on demand. Priority: must-have
  > Socratic: Kontrargument: poprzedni dzień kalendarzowy, np. niedziela przed poniedziałkiem, może nie mieć wpisów. Decyzja użytkownika: pokazywać trzy ostatnie dni zawierające wpisy i umożliwiać doładowanie kolejnego wcześniejszego dnia na żądanie.
- FR-007: User can see the selected day's total recorded time and the remaining time to 7 h 12 min after each entry, save entries exceeding that total with the excess shown, and access the final summary only after correcting the total. Priority: must-have
  > Socratic: Kontrargument: przypadkowe przekroczenie 7 h 12 min mogłoby prowadzić do błędnego rozliczenia. Decyzja: pozwolić zapisać, pokazać przekroczenie i zablokować końcowe podsumowanie do poprawienia sumy.

### Podsumowania i przenoszenie

Po uzupełnieniu całego dnia dostępne jest podsumowanie do ręcznego przeniesienia: lista zadań z tekstem do łatwego skopiowania oraz czasem dla danego systemu. Jira i MyWork mają różne formaty tekstu i osobne zakładki.

- FR-008: User can open separate Jira and MyWork summaries for a fully recorded day, with text ready to copy and the extra 48 minutes in MyWork assigned to the longest individual entry rather than the largest task total. Priority: must-have
  > Socratic: Kontrargument: kilka wpisów do tego samego ticketa powoduje niejednoznaczność pojęcia najdłuższego zadania. Decyzja: wybierać najdłuższy pojedynczy wpis, bez sumowania po tickecie; przy A: 2 h + 2 h i B: 3 h 12 min doliczenie otrzymuje B.
- FR-009: User can confirm or undo transfer confirmation for Jira and MyWork separately, with a day leaving the outstanding list once all required transfers are confirmed and returning when a required confirmation is undone. Priority: must-have
  > Socratic: Kontrargument: omyłkowe potwierdzenie mogłoby ukryć rzeczywistą zaległość. Decyzja: pozwolić cofnąć potwierdzenie każdego systemu osobno i przywrócić dzień do zaległości.
- FR-010: User can see both transfer confirmations for the entire day reset after editing or deleting an entry, while a system not requiring an entry remains not applicable. Priority: must-have
  > Socratic: Kontrargument: zmiana wpisu może zmienić odbiorcę dodatkowych 48 min w MyWork i wpłynąć na zestawienie całego dnia. Decyzja: resetować oba potwierdzenia całego dnia; system niewymagający wpisu pozostaje w stanie „nie dotyczy”.
- FR-011: User can browse saved time entries in history without a retention time limit until they explicitly delete the entries. Priority: must-have
  > Socratic: Kontrargument: automatyczne usuwanie starych rozliczonych dni ogranicza historię i ponowne wykorzystanie wpisów. Decyzja: przechowywać bez limitu czasu, dopóki użytkownik sam nie usunie wpisów.

### Opcjonalne rozszerzenie poza MVP

- FR-012: User can retrieve task suggestions from Jira while retaining manual task entry and local work when Jira is unavailable. Priority: nice-to-have
  > Socratic: Kontrargument: awaria API lub wygasły token mogłyby zablokować wpisywanie czasu po dodaniu integracji. Decyzja: brak dostępu do Jiry nie może blokować lokalnej pracy; ręczne dodawanie pozostaje dostępne. Integracja nadal poza MVP.

## Non-Functional Requirements

- Zapisane wpisy pozostają dostępne po zamknięciu i ponownym otwarciu aplikacji, z uwzględnieniem świadomych zmian użytkownika.
- Historia wpisów nie jest automatycznie usuwana wskutek upływu czasu; wpisy pozostają dostępne, dopóki użytkownik ich nie usunie.
- Niedostępność Jiry nie uniemożliwia lokalnej pracy i ręcznego dodawania zadań, również po wdrożeniu opcjonalnej integracji.
- Aplikacja działa na Windows 11.
- Dane wpisów pozostają na urządzeniu użytkownika i nie są wysyłane do żadnej bazy w chmurze. Przyszłe integracje mogą przesyłać je wyłącznie do Jiry/MyWork w ramach rejestrowania czasu; takie wysyłanie pozostaje poza MVP.

Nie ustalono liczbowych limitów szybkości działania.

## Business Logic

Dla zwykłego dnia uzupełnionego do 7 h 12 min aplikacja przygotowuje 8 h w MyWork, doliczając całe brakujące 48 min do najdłuższego pojedynczego wpisu w danym dniu, a czasy pozostałych wpisów pozostawia bez zmian.

Wejściem są wpisy czasu użytkownika dla wybranego dnia. Zestawienie Jiry zachowuje zapisane czasy. Zestawienie MyWork uwzględnia powyższe doliczenie. Przy remisie najdłuższych czasów różnicę otrzymuje pierwszy zalogowany spośród najdłuższych wpisów. Reguła obejmuje także spotkania i komunikację. Wpisów nie sumuje się według ticketa przy wyborze odbiorcy doliczenia: dla A: 2 h + 2 h oraz B: 3 h 12 min doliczenie otrzymuje B (4 h w MyWork). W US-01 Refinement zachowuje 1 h, a X-1 Order Page otrzymuje 7 h zamiast 6 h 12 min.

Można zapisać wpis powodujący przekroczenie 7 h 12 min. Aplikacja pokazuje przekroczenie i blokuje końcowe podsumowanie do poprawienia sumy; powyższe rozliczenie 8 h dotyczy dnia uzupełnionego dokładnie do 7 h 12 min.

Święto lub urlop wymaga 7 h 12 min na specjalnym tasku Jiry, bez wpisu w MyWork; doliczenie do 8 h nie dotyczy tych dni.

## Access Control

Jeden użytkownik — autor aplikacji — korzysta z lokalnego profilu bez logowania do konta aplikacji. Dane pozostają na urządzeniu. MVP nie wprowadza podziału na role ani współdzielenia danych z innymi użytkownikami. Uwierzytelnianie integracji z Jirą jest osobną kwestią od dostępu do samej aplikacji.

## Non-Goals

- Automatyczne rejestrowanie czasu w Jirze/Clockwork Pro — MVP przygotowuje zestawienie do ręcznego przeniesienia.
- Automatyczne rejestrowanie czasu w MyWork — MVP przygotowuje zestawienie do ręcznego przeniesienia.
- Pobieranie listy ticketów z Jiry — FR-012 pozostaje opcjonalnym rozszerzeniem poza MVP; w MVP zadania są podawane ręcznie.
- Obsługa kolegów z zespołu jako użytkowników MVP — pierwsza wersja służy autorowi, a udostępnienie innym pozostaje przyszłym kierunkiem.
- Przechowywanie wpisów w bazie w chmurze — dane mają pozostać lokalne, z opisanym osobno wyjątkiem przyszłych integracji z docelowymi systemami.
- Wersje na inne systemy niż Windows 11 — MVP obsługuje tylko wskazany system autora.
- Synchronizacja między urządzeniami — MVP służy pracy na lokalnych danych bez ich synchronizowania.
- Wspólne dane i rozliczenia zespołu — nawet przyszli użytkownicy mają rozliczać własny czas niezależnie.

## Open Questions

Brak otwartych pytań. Intensywność użycia i wolumen danych doprecyzowano 2026-10-18 w target_scale.
