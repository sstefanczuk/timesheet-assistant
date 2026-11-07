---
project: Timesheet Assistant
version: 2
status: active
created: 2026-11-01
updated: 2026-11-02
prd_version: 1
main_goal: low-complexity
top_blocker: capacity
milestone_id: daily-local-settlement
milestone_seq: 1
milestone_status: open
---

# Roadmap: Timesheet Assistant

> Źródłem zakresu jest PRD v1 oraz automatycznie zbadany i potwierdzony przez użytkownika stan kodu.
> Dokument jest rozwijany w miejscu; kolejny kamień milowy kontynuuje ten plik.
> Elementy są zapisane w kolejności zależności, bez estymacji i terminów wykonania.

## Milestone

**M-1: Lokalne rozliczenie dnia dla Jiry i MyWork** — Status: open

- **Approval:** Użytkownik zatwierdził roadmapę w wersji 2 dnia 2026-11-02, z przepływem: ostatnie wpisy → kopiowanie na dzisiaj lub dodanie od zera → podsumowania Jiry i MyWork.
- **Intent:** Dostarczyć lokalny przepływ od zapisania pracy przez poprawne podsumowania do potwierdzenia ręcznego przeniesienia. Objąć również zaległości, nieobecności i ponowne wykorzystanie wpisów wymagane przez MVP.
- **Source materials:** `context/foundation/prd.md` (v1); pomocniczo tech-stack.md, shape-notes.md, infrastructure.md i dokument wykonania wdrożenia.
- **Done when:** every F-NN and S-NN below is `done`. Dowody odbioru muszą obejmować US-01, zachowanie wpisów po ponownym uruchomieniu oraz zasady zaległości i potwierdzeń. Kryterium produktu dotyczące tygodnia codziennego używania pozostaje osobną walidacją autora; ukończenie implementacji samo go nie potwierdza.
- **Scope anchors:** US-01, FR-001–FR-011.
- **Sequencing:** Użytkownik wybrał prostotę rozwiązania, przepływ: ostatnie wpisy po otwarciu aplikacji → kopiowanie na dzisiaj lub dodanie od zera → podsumowania US-01 jako pierwszy dowód wartości i ograniczoną dostępność jako główne ograniczenie. Zalecana jest jedna aktywna zmiana naraz; możliwości równoległej pracy są informacją o zależnościach.
- **Investment:** Najwięcej uwagi wymaga trwałość danych i poprawność przeliczeń wskazana w kryteriach sukcesu oraz wymaganiach niefunkcjonalnych. Interfejs, brak osobnego serwera i istniejąca dystrybucja pozostają proste. Szczegóły techniczne rozstrzyga plan danej zmiany.

## Vision recap

Timesheet Assistant rozwiązuje codzienne tarcie związane z rozproszonym i powielanym rejestrowaniem czasu. W Jirze właściwe miejsca logowania znajdują się na boardzie, w Epicach oraz specjalnych taskach. Następnie numer i tytuł ticketa, przepracowany czas oraz czasem opis pracy trzeba ręcznie przenieść do MyWork, oczyszczając przy tym skopiowany tekst z niepotrzebnych znaków.

## North star

North star oznacza tutaj najmniejszy kompletny przepływ, który potwierdza główną wartość aplikacji.

**S-02: Podsumowania Jiry i MyWork** — po zapisaniu dnia użytkownik otrzymuje poprawne teksty i sumy 7 h 12 min oraz 8 h z US-01. S-01 dostarcza potrzebne wpisy, a F-01 umożliwia sprawdzanie rzeczywistego interfejsu; następnie S-07 pokazuje ostatnie dni po uruchomieniu, S-08 pozwala kopiować wpis na dzisiaj, a S-02 zamyka pierwszy przepływ podsumowaniami. Zalecana kolejność: F-01 → S-01 → S-07 → S-08 → S-02. Zarządzanie zaległościami dochodzi później.

## At a glance

| ID | Change ID | Outcome (user can …) | Prerequisites | PRD refs | Status |
| --- | --- | --- | --- | --- | --- |
| F-01 | verify-desktop-ui-path | (foundation) Umożliwić automatyczne uruchomienie aplikacji i interakcję z jej interfejsem na odizolowanych danych w CI. | — | US-01; Non-Functional Requirements | planning |
| S-01 | record-persistent-workday | Użytkownik może wybrać datę, dodawać, edytować i usuwać osobne wpisy pracy oraz widzieć sumę, brakujący czas lub przekroczenie 7 h 12 min; wpisy pozostają po restarcie. | F-01 | FR-002, FR-005, FR-007; Non-Functional Requirements | proposed |
| S-07 | browse-entry-history | Użytkownik po otwarciu aplikacji widzi wpisy z trzech ostatnich wcześniejszych dni zawierających wpisy, osobno dzisiaj i możliwość dodania wpisu od zera. Może doładować starszy dzień i przeglądać historię bez limitu retencji; upływ czasu ani aktualizacja aplikacji nie usuwa historii. Przy braku wpisów widzi pusty stan i możliwość dodania nowego. | S-01 | FR-002, FR-006, FR-011; Non-Functional Requirements | proposed |
| S-08 | copy-earlier-day-entries | Użytkownik może skopiować wybrane wpisy z trzech ostatnich wcześniejszych dni zawierających wpisy i doładować kolejny starszy dzień; może bezpośrednio skopiować pojedynczy wpis na dzisiaj lub wybrać inny dzień docelowy. Kopie są niezależne od oryginału i można je edytować przed podsumowaniem. | S-07 | FR-006 | proposed |
| S-02 | prepare-dual-system-summaries | Użytkownik może otworzyć osobne podsumowania kompletnego dnia i kopiować teksty do Jiry i MyWork; dodatkowe 48 min otrzymuje najdłuższy pojedynczy wpis, przy remisie pierwszy zapisany. | S-08 | US-01, FR-007, FR-008 | proposed |
| S-03 | track-outstanding-days | Użytkownik może zobaczyć zaległe dni robocze od najstarszego, osobno dzisiaj, wybrać dzień do uzupełnienia i oznaczyć dzień rozliczony wcześniej bez odtwarzania wpisów; starsze nierozliczone dni pozostają widoczne. | S-01 | FR-001, FR-002 | proposed |
| S-04 | confirm-day-transfers | Użytkownik może osobno potwierdzić lub cofnąć przeniesienie do każdego systemu; dzień znika z zaległości po wymaganych potwierdzeniach i wraca po ich cofnięciu, a edycja lub usunięcie wpisu resetuje oba potwierdzenia dnia. | S-02, S-03 | FR-005, FR-009, FR-010 | proposed |
| S-05 | settle-holiday-and-leave | Użytkownik może zmienić typ pojedynczego dnia na święto lub urlop, uzyskać 7 h 12 min na specjalnym tasku Jiry i stan „nie dotyczy” dla MyWork; zastąpienie wpisów urlopem wymaga ostrzeżenia i jawnego potwierdzenia. | S-04 | FR-003, FR-009, FR-010 | proposed |
| S-06 | reuse-entry-templates | Użytkownik może tworzyć, edytować i usuwać zapisane szablony oraz dodawać z nich wpisy; późniejsza zmiana szablonu nie zmienia istniejących wpisów. | S-01 | FR-004 | proposed |

## Baseline

Stan zbadany w kodzie i dokumentach, potwierdzony przez użytkownika; zapisany 2026-11-01.

- **Frontend:** present dla szkieletu, absent dla funkcji produktu — istnieją nawigacja i strony Home, Counter, Weather w `src/TimesheetAssistant/Components/`. Wybór stosu jest zgodny z tech-stack.md.
- **Backend / API:** absent, osobny serwer nie jest wymagany; logika domenowa jeszcze nie istnieje.
- **Data:** absent — brak trwałego zapisu i historii; `src/TimesheetAssistant/MauiProgram.cs` rejestruje tylko usługi startera.
- **Auth:** absent, świadomie poza zakresem jednego lokalnego użytkownika.
- **Deploy / infra:** present — `.github/workflows/windows.yml` oraz `context/deployment/deploy-plan.md` potwierdzają opublikowany i sprawdzony na komputerze autora podgląd startera. Test czystego urządzenia i zgodność przyszłych danych po aktualizacji nie zostały potwierdzone.
- **Observability:** partial — logowanie debugowe w `MauiProgram.cs` i logi procesu publikacji; brak diagnostyki domenowej. Nie wybrano telemetrii chmurowej.
- **Tests:** partial — pusty projekt xUnit; `src/TimesheetAssistant.Tests/README.md` potwierdza brak testów funkcjonalnych i automatyzacji WebView2. Sukces wykrywania testów nie potwierdza działania produktu.
- **Additional inputs:** brak lessons.md oraz sekcji Forward: technical-roadmap w shape-notes.md. Istniejące decyzje o dystrybucji nie wymagają odtworzenia fundamentu wdrożenia.

## Foundations

### F-01: Sprawdzalna ścieżka uruchomienia aplikacji

- **Outcome:** (foundation) Umożliwić automatyczne uruchomienie aplikacji i interakcję z jej interfejsem na odizolowanych danych w CI.
- **Change ID:** verify-desktop-ui-path
- **PRD refs:** US-01; Non-Functional Requirements
- **Unlocks:** S-01 i S-02: ścieżka weryfikacji rzeczywistego interfejsu; S-01 nadal implementuje i sprawdza trwałość wpisów.
- **Prerequisites:** —
- **Parallel with:** —
- **Blockers:** —
- **Unknowns:** Czy wybrana automatyzacja interfejsu działa na runnerze Windows? — Owner: autor z agentem. Block: no. To wynik tego elementu, a nie warunek rozpoczęcia jego planowania.
- **Risk:** Sprawdzamy tylko uruchomienie i interakcję; zapis oraz zachowanie po restarcie zostaną zweryfikowane z rzeczywistą funkcją w S-01.
- **Status:** planning

## Slices

### S-01: Zapisywanie pracy i kontrola sumy dnia

- **Outcome:** Użytkownik może wybrać datę, dodawać, edytować i usuwać osobne wpisy pracy oraz widzieć sumę, brakujący czas lub przekroczenie 7 h 12 min; wpisy pozostają po restarcie.
- **Change ID:** record-persistent-workday
- **PRD refs:** FR-002, FR-005, FR-007; Non-Functional Requirements
- **Prerequisites:** F-01
- **Parallel with:** —
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Trwały zapis poza folderem aplikacji i weryfikacja po restarcie chronią pierwszy użyteczny przepływ; obsługa potwierdzeń po edycji dochodzi w S-04.
- **Status:** proposed

### S-07: Przeglądanie historii

- **Outcome:** Użytkownik po otwarciu aplikacji widzi wpisy z trzech ostatnich wcześniejszych dni zawierających wpisy, osobno dzisiaj i możliwość dodania wpisu od zera. Może doładować starszy dzień i przeglądać historię bez limitu retencji; upływ czasu ani aktualizacja aplikacji nie usuwa historii. Przy braku wpisów widzi pusty stan i możliwość dodania nowego.
- **Change ID:** browse-entry-history
- **PRD refs:** FR-002, FR-006, FR-011; Non-Functional Requirements
- **Prerequisites:** S-01
- **Parallel with:** S-03, S-04, S-05, S-06
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Widok ostatnich wpisów jest ekranem początkowym i nie wymaga listy zaległości. Trzy dni oznaczają dni zawierające wpisy zgodnie z FR-006. Historia obejmuje również dni rozliczone; sprawdzenie zachowania danych po zmianie folderu aplikacji rozwija kontrolę trwałości z S-01.
- **Status:** proposed

### S-08: Kopiowanie wcześniejszych wpisów

- **Outcome:** Użytkownik może skopiować wybrane wpisy z trzech ostatnich wcześniejszych dni zawierających wpisy i doładować kolejny starszy dzień; może bezpośrednio skopiować pojedynczy wpis na dzisiaj lub wybrać inny dzień docelowy. Kopie są niezależne od oryginału i można je edytować przed podsumowaniem.
- **Change ID:** copy-earlier-day-entries
- **PRD refs:** FR-006
- **Prerequisites:** S-07
- **Parallel with:** S-03, S-04, S-05, S-06
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Wybór źródła pomija puste dni; kopia nie modyfikuje oryginału i korzysta z zasad kontroli sumy oraz potwierdzeń obowiązujących w dniu docelowym.
- **Status:** proposed

### S-02: Podsumowania Jiry i MyWork

- **Outcome:** Użytkownik może otworzyć osobne podsumowania kompletnego dnia i kopiować teksty do Jiry i MyWork; dodatkowe 48 min otrzymuje najdłuższy pojedynczy wpis, przy remisie pierwszy zapisany.
- **Change ID:** prepare-dual-system-summaries
- **PRD refs:** US-01, FR-007, FR-008
- **Prerequisites:** S-08
- **Parallel with:** S-03, S-06
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Po dodawaniu od zera i kopiowaniu na dzisiaj sprawdzamy US-01, rozdzielenie wpisów tego samego ticketa i blokadę podsumowania przy sumie innej niż 7 h 12 min.
- **Status:** proposed

### S-03: Wybór zaległego dnia

- **Outcome:** Użytkownik może zobaczyć zaległe dni robocze od najstarszego, osobno dzisiaj, wybrać dzień do uzupełnienia i oznaczyć dzień rozliczony wcześniej bez odtwarzania wpisów; starsze nierozliczone dni pozostają widoczne.
- **Change ID:** track-outstanding-days
- **PRD refs:** FR-001, FR-002
- **Prerequisites:** S-01
- **Parallel with:** S-02, S-06, S-07, S-08
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Przesunięcie okna poprzedniego i aktualnego tygodnia nie może usuwać starszych zaległości; samo zapisanie pełnego dnia nie zamyka zaległości.
- **Status:** proposed

### S-04: Potwierdzanie przeniesienia i korekty

- **Outcome:** Użytkownik może osobno potwierdzić lub cofnąć przeniesienie do każdego systemu; dzień znika z zaległości po wymaganych potwierdzeniach i wraca po ich cofnięciu, a edycja lub usunięcie wpisu resetuje oba potwierdzenia dnia.
- **Change ID:** confirm-day-transfers
- **PRD refs:** FR-005, FR-009, FR-010
- **Prerequisites:** S-02, S-03
- **Parallel with:** S-06, S-07, S-08
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Potwierdzenia dotyczą aktualnej treści całego dnia, dlatego korekta nie może pozostawić pozornego rozliczenia; stany zachowują się po restarcie.
- **Status:** proposed

### S-05: Rozliczenie święta lub urlopu

- **Outcome:** Użytkownik może zmienić typ pojedynczego dnia na święto lub urlop, uzyskać 7 h 12 min na specjalnym tasku Jiry i stan „nie dotyczy” dla MyWork; zastąpienie wpisów urlopem wymaga ostrzeżenia i jawnego potwierdzenia.
- **Change ID:** settle-holiday-and-leave
- **PRD refs:** FR-003, FR-009, FR-010
- **Prerequisites:** S-04
- **Parallel with:** S-06, S-07, S-08
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Anulowanie zastąpienia zachowuje istniejące wpisy; wymagane potwierdzenia muszą odpowiadać typowi dnia i przetrwać restart.
- **Status:** proposed

### S-06: Ponowne używanie szablonów

- **Outcome:** Użytkownik może tworzyć, edytować i usuwać zapisane szablony oraz dodawać z nich wpisy; późniejsza zmiana szablonu nie zmienia istniejących wpisów.
- **Change ID:** reuse-entry-templates
- **PRD refs:** FR-004
- **Prerequisites:** S-01
- **Parallel with:** S-02, S-03, S-04, S-05, S-07, S-08
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Wprowadzamy szablony po ręcznym zapisie; zapisany wpis musi zachować własną treść niezależną od dalszego życia szablonu.
- **Status:** proposed

## Backlog Handoff

| Roadmap ID | Change ID | Suggested issue title | Ready for `/10x-plan` | Notes |
| --- | --- | --- | --- | --- |
| F-01 | verify-desktop-ui-path | Sprawdzalna ścieżka uruchomienia aplikacji | yes | Odblokowuje S-01 → S-07 → S-08 → S-02. |
| S-01 | record-persistent-workday | Zapisywanie pracy i kontrola sumy dnia | no | Po ukończeniu: F-01 |
| S-07 | browse-entry-history | Przeglądanie historii | no | Po ukończeniu: S-01 |
| S-08 | copy-earlier-day-entries | Kopiowanie wcześniejszych wpisów | no | Po ukończeniu: S-07 |
| S-02 | prepare-dual-system-summaries | Podsumowania Jiry i MyWork | no | Po ukończeniu: S-08 |
| S-03 | track-outstanding-days | Wybór zaległego dnia | no | Po ukończeniu: S-01 |
| S-04 | confirm-day-transfers | Potwierdzanie przeniesienia i korekty | no | Po ukończeniu: S-02, S-03 |
| S-05 | settle-holiday-and-leave | Rozliczenie święta lub urlopu | no | Po ukończeniu: S-04 |
| S-06 | reuse-entry-templates | Ponowne używanie szablonów | no | Po ukończeniu: S-01 |

## Open Roadmap Questions

Brak otwartych pytań produktowych. PRD: „Brak otwartych pytań. Intensywność użycia i wolumen danych doprecyzowano 2026-10-18 w target_scale.”
Wykonalność automatyzacji interfejsu jest pytaniem badawczym F-01, możliwym do rozstrzygnięcia w ramach tej zmiany. Jeżeli wynik podważy wybrany sposób testowania, należy jawnie rozstrzygnąć sposób weryfikacji przed uznaniem S-01 za gotowe do odbioru.

## Parked

- **Sugestie ticketów z Jiry (FR-012)** — opcjonalne rozszerzenie poza MVP; ręczny zapis działa lokalnie.
- **Automatyczne logowanie czasu do Jiry/Clockwork Pro i MyWork** — poza zakresem PRD; podsumowania służą ręcznemu przeniesieniu.
- **Konta aplikacji, role i współdzielenie danych** — MVP służy autorowi, bez logowania.
- **Chmurowa baza oraz synchronizacja urządzeń** — dane pozostają lokalne.
- **Inne systemy operacyjne** — MVP obsługuje Windows 11.
- **Automatyczny aktualizator, zmiana kanału dystrybucji i rozbudowana infrastruktura** — brak potrzeby dla tego przepływu; wykorzystujemy istniejącą publikację.
- **Test czystego urządzenia jako bramka tego kamienia milowego** — zatwierdzony podgląd sprawdzono na komputerze autora; zgodność czystego urządzenia pozostaje niepotwierdzona. Zachowanie lokalnych danych po aktualizacji jest wymagane i weryfikowane w S-01/S-07.

## Milestone History

## Done

