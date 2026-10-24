---
project: "Timesheet Assistant"
context_type: greenfield
created: 2026-09-30
updated: 2026-10-18
product_type: desktop
target_scale:
  users: small
  qps: "Niska intensywność: pojedyncze wpisy w ciągu dnia; sporadycznie uzupełnienie całego tygodnia. Bez liczbowego założenia QPS."
  data_volume: "2–5 wpisów dziennie na użytkownika; historia bez limitu czasu."
timeline_budget:
  mvp_weeks: 5
  hard_deadline: 2026-12-06
  after_hours_only: true
checkpoint:
  current_phase: 8
  phases_completed: [1, 2, 3, 4, 5, 6, 7]
  gray_areas_resolved:
    - topic: "usage intensity and data volume"
      decision: "2–5 wpisów dziennie; zwykle pojedyncze wpisy w ciągu dnia, sporadycznie uzupełnianie całego tygodnia. Historia nadal bez limitu czasu. Pytanie zamknięto 2026-10-18; nie ustalano liczbowego QPS."
    - topic: "pain category"
      decision: "Tarcie w codziennym workflow i dane rozproszone między boardem, Epicami oraz specjalnymi taskami."
    - topic: "status quo gap"
      decision: "Jira pokazuje głównie bieżący board, ale właściwe miejsca logowania są także w Epicach i specjalnych taskach."
    - topic: "primary persona scope"
      decision: "Pierwsza wersja wyłącznie dla autora; możliwe udostępnienie kolegom pozostaje przyszłym kierunkiem poza MVP."
    - topic: "access control"
      decision: "Lokalny profil bez logowania; dane pozostają na urządzeniu. MVP służy wyłącznie autorowi, bez podziału na role."
    - topic: "backlog window"
      decision: "Przy otwarciu aplikacji okres zaczyna się w poniedziałek poprzedniego tygodnia i obejmuje poprzedni oraz aktualny tydzień; standardowo poniedziałek–piątek."
    - topic: "older backlog retention"
      decision: "Starsze zaległości pozostają na liście aż do potwierdzenia przeniesienia wymaganych wpisów, również po wypadnięciu poza domyślne okno dwóch tygodni."
    - topic: "day type"
      decision: "W MVP użytkownik ręcznie zmienia typ pojedynczego dnia na święto lub urlop. Dla tych dni Jira wymaga 7 h 12 min na specjalnym tasku, a MyWork nie wymaga wpisu."
    - topic: "transfer confirmation"
      decision: "Dzień znika z zaległości po ręcznym oznaczeniu przeniesienia wpisów, z osobnymi flagami dla Jiry i MyWork."
    - topic: "timeline acknowledgment"
      decision: "Użytkownik świadomie zaakceptował 4–5 tygodni regularnej pracy po godzinach przy obecnym zakresie; do harmonogramu przyjęto górną granicę 5 tygodni."
    - topic: "success guardrails"
      decision: "Zachowanie zapisanych wyników oraz pokazywanie dni, dla których jeszcze brakuje wpisów."
    - topic: "success criteria scope"
      decision: "Codzienne korzystanie przez jeden tydzień zamiast dwóch. Użytkownik nie potrzebuje dodatkowego kryterium Secondary."
    - topic: "editable templates"
      decision: "Użytkownik tworzy, edytuje i usuwa własne szablony z poziomu aplikacji."
    - topic: "editing transferred entries"
      decision: "Wpisy można edytować i usuwać również po potwierdzeniu przeniesienia; taka zmiana resetuje potwierdzenie przeniesienia."
    - topic: "MyWork time allocation"
      decision: "Pozostały czas do 8 h trafia do najdłuższego pojedynczego wpisu w danym dniu, bez sumowania wpisów według ticketa. Dla pełnego zwykłego dnia różnica wynosi 48 min."
    - topic: "MyWork allocation tie and eligible tasks"
      decision: "Przy remisie najdłuższego czasu różnicę otrzymuje pierwszy zalogowany spośród najdłuższych wpisów. Spotkania i komunikacja również mogą otrzymać różnicę."
    - topic: "Socratic FR-001: already settled days"
      decision: "Użytkownik może oznaczyć dzień jako już rozliczony bez odtwarzania wpisów."
    - topic: "Socratic FR-002: today"
      decision: "Dzisiejszy dzień jest pokazywany osobno jako dzień w toku."
    - topic: "Socratic FR-003: replacing entries with leave"
      decision: "Istniejące wpisy pracy można zastąpić wpisem urlopowym dopiero po ostrzeżeniu i potwierdzeniu użytkownika."
    - topic: "Socratic FR-004: template history"
      decision: "Edycja lub usunięcie szablonu nie zmienia wcześniej utworzonych wpisów; szablon wpływa tylko na nowe wpisy."
    - topic: "Socratic FR-005: repeated task entries"
      decision: "Kolejne wpisy do tego samego ticketa pozostają osobne, każdy z własnym czasem i opisem."
    - topic: "Socratic FR-006: copying source days"
      decision: "Kopiowanie z trzech ostatnich wcześniejszych dni zawierających wpisy, z możliwością załadowania kolejnego starszego dnia na żądanie."
    - topic: "Socratic FR-007: daily limit exceeded"
      decision: "Można zapisać przekroczenie 7 h 12 min; aplikacja pokazuje przekroczenie i blokuje końcowe podsumowanie do poprawienia sumy."
    - topic: "Socratic FR-008: longest entry versus task total"
      decision: "Dodatkowe 48 min trafia do najdłuższego pojedynczego wpisu. Dla A: 2 h + 2 h i B: 3 h 12 min różnicę otrzymuje B."
    - topic: "Socratic FR-009: undo confirmation"
      decision: "Można cofnąć potwierdzenie każdego systemu osobno, przywracając dzień do zaległości."
    - topic: "Socratic FR-010: reset scope"
      decision: "Edycja lub usunięcie wpisu resetuje oba potwierdzenia całego dnia; system niewymagający wpisu pozostaje w stanie nie dotyczy."
    - topic: "Socratic FR-011: history retention"
      decision: "Historia jest przechowywana bez limitu czasu, dopóki użytkownik sam nie usunie wpisów."
    - topic: "Socratic FR-012: Jira unavailable"
      decision: "Po dodaniu integracji niedostępność Jiry nie może blokować lokalnej pracy; ręczne dodawanie zadań pozostaje dostępne."
    - topic: "supported operating system"
      decision: "MVP jest aplikacją desktopową dla Windows 11."
    - topic: "entry data privacy"
      decision: "Dane wpisów pozostają lokalnie i nie trafiają do bazy w chmurze. Jedynym wskazanym wyjątkiem jest przyszłe przesyłanie danych do Jiry/MyWork przez integracje."
    - topic: "future independent users"
      decision: "Także przy ewentualnym udostępnieniu aplikacji wielu osobom każdy rozlicza własny czas niezależnie; nie są planowane wspólne dane."
    - topic: "MVP platform and synchronization boundaries"
      decision: "MVP wyłącznie dla Windows 11, bez synchronizacji między urządzeniami."
  frs_drafted: 12
  quality_check_status: accepted
---

## Seed Idea

Historyczna treść wejściowa. Późniejsze ustalenia w sekcjach poniżej mają pierwszeństwo; kryterium dwóch tygodni użytkownik zmienił na jeden tydzień 2026-10-17.

## Timesheet Assistant - MVP

### Główny problem

Manuale logowanie czasu pracy do dwóch systemów jest czasochłonne. Jeden z nich to Jira (i dodatek Clockwork Pro) w organizacji klienta. Drugi z nich to system wewnętrzny MyWork z firmy w której jestem zatrudniony. Wymagane jest wejście w odpowiedni ticket na Jirze i dodanie czasu pracy. Obejmuje to zarówno zwykłe tickety z bieżącymi zadaniami, jak i dedykowane tickety na Scrum Ceremonies, Internal Communication, Knowledge Sharing, a nawet Holiday. MyWork oczekuje zalogowanych 8h, podczas gdy Jira 90% z tego czasu, czyli 7h 12m.

### Preferowana technologia

Osobista aplikacja desktopowa w .NET MAUI.
Doświadczenie w .NET / C#: 12 lat
Doświadczenie w MAUI: zerowe

### Najmniejszy zestaw funkcjonalności

- Użytkownik przypisuje czas do kategorii takich jak bieżące zadanie, komunikacja, ceremonie Scrum, knowledge sharing i nieobecność.
- Aplikacja oblicza osobne zestawienie dla Jiry/Clockwork Pro — 7 h 12 min — oraz MyWork — 8 h
- Lokalne zarządzanie wpisami
- Reguły podziału
- Walidację
- Przeglądanie poprzednich wpisów (historię)
- Opcja łatwego kopiowania wybranych wpisów z poprzedniego dnia na kolejny dzień
- instalowalna aplikacja desktopowa i dokumentacja kontekstowa

### Opcjonalnie rozszerzenie MVP

- Integracja z Jira w celu pobrania listy ticketów, na które można zalogować czas

### Co NIE wchodzi w zakres MVP

- Automatyczne logowanie czasu do Jira / Clockwork Pro
- Automatyczne logowanie czasu pracy do MyWork

### Kryteria sukcesu

- Aplikacja używana do logowania czasu pracy codziennie przez kolejne 2 tygodnie
- Każdy dzień zawiera zalogowane 8h

### Dostępność

Budżet: około 4h tygodniowo
Termin: 6 grudnia 2026

## Vision & Problem Statement

Timesheet Assistant rozwiązuje codzienne tarcie związane z rozproszonym i powielanym rejestrowaniem czasu. W Jirze właściwe miejsca logowania znajdują się na boardzie, w Epicach oraz specjalnych taskach. Następnie numer i tytuł ticketa, przepracowany czas oraz czasem opis pracy trzeba ręcznie przenieść do MyWork, oczyszczając przy tym skopiowany tekst z niepotrzebnych znaków. Przy odkładaniu rozliczenia rośnie trudność odtworzenia wykonanej pracy i ryzyko błędów.

Istotą problemu jest to, że bieżący board nie obejmuje wszystkich miejsc, w których rejestrowana jest praca, a te same informacje muszą zostać ponownie wprowadzone w drugim systemie.

Perspektywa większej skali: także przy ewentualnych 100 użytkownikach każdy rozlicza własny czas niezależnie; wspólne dane nie są planowane. MVP nadal służy wyłącznie autorowi.

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

Wymagania na podstawie przepływu podanego przez użytkownika; zapisano US-01. Faza 4 została ukończona: zapisano odpowiedzi na wszystkie 12 kontrargumentów do FR-001–FR-012. Must-have obejmuje opisany przepływ MVP. Pobieranie ticketów z Jiry jest opcjonalnym rozszerzeniem poza MVP.

### Dni i zaległości

- FR-001: User can view outstanding weekdays from the previous and current week, plus older unresolved days, ordered from oldest to newest, and mark previously settled days as settled without recreating their entries. Priority: must-have
  > Socratic: Kontrargument: przy pierwszym uruchomieniu dni rozliczone wcześniej w obu systemach mogą wyglądać na zaległe. Decyzja: pozwolić oznaczyć dzień jako rozliczony bez odtwarzania wpisów.
- FR-002: User can select an outstanding day or today to record work incrementally, with today displayed separately as a day in progress. Priority: must-have
  > Socratic: Kontrargument: niepełny dzisiejszy dzień jest normalną pracą w toku, więc pokazywanie go jako zaległości byłoby mylące. Decyzja: dzisiaj pokazywać osobno jako dzień w toku.
- FR-003: User can manually change an individual day's type to a holiday or leave day, requiring 7 h 12 min on a special Jira task and no MyWork entry, with existing work entries replaced by a leave entry only after a warning and explicit confirmation. Priority: must-have
  > Socratic: Kontrargument: zmiana na urlop mogłaby przypadkowo zastąpić istniejące wpisy pracy. Decyzja: zastąpić wpisy wpisem urlopowym dopiero po ostrzeżeniu i potwierdzeniu.

### Wpisy i szablony

- FR-004: User can create, edit and delete reusable templates with a Jira task, work description and duration, and use them to add time entries without later template changes affecting existing entries. Priority: must-have
  > Socratic: Kontrargument: edycja lub usunięcie szablonu mogłyby zmienić wcześniej rozliczone dni. Decyzja: istniejące wpisy pozostają bez zmian; szablon wpływa tylko na nowe wpisy.
- FR-005: User can manually add time entries using a task ID, title, duration and optional work description, keep repeated entries for the same task separate with their own times and descriptions, and edit or delete saved entries even after transfer confirmation. Priority: must-have
  > Socratic: Kontrargument: automatyczne łączenie kolejnych wpisów do tego samego ticketa mogłoby zatrzeć szczegóły pracy. Decyzja: zachować osobne wpisy z własnym czasem i opisem.
- FR-006: User can copy selected entries from the three most recent earlier days containing entries and load one additional older day on demand. Priority: must-have
  > Socratic: Kontrargument: poprzedni dzień kalendarzowy, np. niedziela przed poniedziałkiem, może nie mieć wpisów. Decyzja użytkownika: pokazywać trzy ostatnie dni zawierające wpisy i umożliwiać doładowanie kolejnego wcześniejszego dnia na żądanie.
- FR-007: User can see the selected day's total recorded time and the remaining time to 7 h 12 min after each entry, save entries exceeding that total with the excess shown, and access the final summary only after correcting the total. Priority: must-have
  > Socratic: Kontrargument: przypadkowe przekroczenie 7 h 12 min mogłoby prowadzić do błędnego rozliczenia. Decyzja: pozwolić zapisać, pokazać przekroczenie i zablokować końcowe podsumowanie do poprawienia sumy.

### Podsumowania i przenoszenie

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

Faza 5 ukończona na podstawie odpowiedzi użytkownika. Nie ustalono liczbowych limitów szybkości działania.

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

Faza 6 ukończona. Typ produktu, skala użytkowników i termin wynikają z wcześniejszych odpowiedzi; końcowe granice zakresu zatwierdzono 2026-10-18.

## Open Questions

Brak otwartych pytań. Intensywność użycia i wolumen danych doprecyzowano 2026-10-18 w target_scale.

## MVP flow

Poniższe ustalenia pochodzą z odpowiedzi użytkownika. Faza 3 została ukończona. Użytkownik zaakceptował termin 4–5 tygodni, warunki Guardrails, jeden tydzień codziennego używania jako kryterium Primary oraz brak dodatkowego kryterium Secondary.

1. Po otwarciu aplikacji użytkownik widzi zaległe dni do uzupełnienia, od najstarszego do wczoraj. Okres zaczyna się w poniedziałek poprzedniego tygodnia i obejmuje poprzedni oraz aktualny tydzień. Standardowo rozliczane są dni poniedziałek–piątek. Dzisiejszy dzień jest pokazywany osobno jako dzień w toku i pozwala dodawać bieżące wpisy. Dni rozliczone wcześniej w systemach zewnętrznych można oznaczyć jako rozliczone bez odtwarzania wpisów.
2. Aplikacja wygodnie podpowiada zadania do zapisania. Przykłady podane przez użytkownika: Refinement — 1 h, T-1 Scrum Ceremonies, opis „Refinement”; Sprint Planning — 30 min, ten sam T-1, opis „Sprint Planning”; CAB Call — 30 min, T-2 Internal Communication, opis „CAB call”.
3. Użytkownik może kopiować wybrane wpisy z trzech ostatnich wcześniejszych dni zawierających wpisy, np. X-1 Order Page - implement cart, oraz doładować kolejny starszy dzień na żądanie. Nowe zadanie, np. X-2 Add order reports, dodaje ręcznie przez podanie ID i nazwy. Kolejne wpisy do tego samego ticketa są osobne i mają własny czas oraz opis. Pobieranie listy z Jiry pozostaje przyszłym rozszerzeniem.
4. Po każdym lokalnym wpisie aplikacja pokazuje sumę czasu dnia i czas pozostały do 7 h 12 min. Przekroczenie można zapisać, ale jest widoczne i blokuje końcowe podsumowanie do poprawienia sumy.
5. Po uzupełnieniu całego dnia dostępne jest podsumowanie do ręcznego przeniesienia: lista zadań z tekstem do łatwego skopiowania oraz czasem dla danego systemu. Jira i MyWork mają różne formaty tekstu i osobne zakładki.
6. Użytkownik osobno oznacza przeniesienie wpisów do Jiry oraz MyWork. Może cofnąć potwierdzenie każdego systemu niezależnie, przywracając dzień do zaległości. Edycja lub usunięcie wpisu resetuje oba potwierdzenia całego dnia; system niewymagający wpisu pozostaje w stanie „nie dotyczy”. Samo wypełnienie czasu lokalnie nie usuwa dnia z zaległości.

### Święta i urlopy

W MVP użytkownik ręcznie zmienia typ pojedynczego dnia. Święto lub urlop wymaga wpisu 7 h 12 min na specjalnym tasku Jiry. MyWork nie wymaga wtedy żadnego wpisu. Nie jest to zwykły dzień wymagający 8 h w MyWork; zasada doprecyzowuje ogólne kryterium z Seed Idea.

Zmiana dnia zawierającego już wpisy pracy na urlop zastępuje te wpisy wpisem urlopowym dopiero po ostrzeżeniu i wyraźnym potwierdzeniu użytkownika.

### Starsze zaległości

Niepotwierdzone dni pozostają na liście zaległości także wtedy, gdy wypadają poza domyślny okres poprzedniego i aktualnego tygodnia. Upływ czasu nie zamyka zaległości. Użytkownik przewiduje uzupełnianie poprzedniego tygodnia najpóźniej w poniedziałek kolejnego tygodnia; jest to zwyczaj pracy, a nie automatyczna reguła zamykania dni.

## Timeline acknowledgment

Acknowledged on 2026-10-17: 5-week MVP requires sustained dedication; user accepted.

Szacunek użytkownika: 4–5 tygodni przy około 4 godzinach tygodniowo. Pole mvp_weeks przyjmuje górną granicę przedziału: 5. Użytkownik świadomie zaakceptował regularną pracę po godzinach przy opisanym zakresie. Planowana pierwsza wersja: 14–21 listopada 2026, następnie jeden tydzień codziennego używania wymagany przez kryterium sukcesu oraz czas na poprawki przed terminem 6 grudnia 2026. Skrócenie okresu używania nie zmienia zaakceptowanego szacunku wykonania MVP.

## Forward: tech-stack

Użytkownik wskazał .NET MAUI jako preferowaną technologię i opisał doświadczenie w .NET/C#. Jest to wejście do późniejszego wyboru stacka, a nie decyzja architektoniczna podejmowana w tej sesji ani część sekcji PRD.

## Quality cross-check

Przegląd wykonano 2026-10-18. Użytkownik zaakceptował notatki 2026-10-18. Sesja planistyczna zakończona; checkpoint znajduje się w fazie 8 ze statusem accepted. Notatki są gotowe jako wejście do /10x-prd.

| Kontrola | Wynik | Uzasadnienie |
| --- | --- | --- |
| Access Control | present | Jeden użytkownik, lokalny profil bez logowania. |
| Business Logic | present | Jednozdaniowa reguła doliczenia 48 min do najdłuższego pojedynczego wpisu; określony remis i wyjątki dla urlopu/święta. |
| Project artifacts | present | shape-notes.md z checkpointem, siedmioma ukończonymi fazami i 12 wymaganiami po rundzie kontrargumentów. |
| Timeline-cost acknowledgment | present | Użytkownik zaakceptował 4–5 tygodni regularnej pracy po godzinach 2026-10-17. |
| Non-Goals | present | Jawne wykluczenia integracji, chmury, innych systemów i synchronizacji oraz pracy zespołowej. |
| Preserved behavior | n/a | Projekt greenfield. |

Brak brakujących elementów w powyższej kontroli. Pytanie o intensywność użycia i wolumen danych zamknięto 2026-10-18: 2–5 wpisów dziennie, zwykle dodawanych pojedynczo, ze sporadycznym uzupełnianiem całego tygodnia. Nie ustalono liczbowych limitów szybkości działania.
