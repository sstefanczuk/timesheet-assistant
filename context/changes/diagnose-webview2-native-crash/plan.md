# Diagnoza natywnej awarii WebView2 — Implementation Plan

## Overview

Ustalić miejsce i możliwą przyczynę zakończenia własnych procesów WebView2 kodem `0x80000003` na `windows-2025`. Wynikiem jest raport z dowodami oraz uzasadniony dalszy krok dla F-01; diagnoza sama nie zamyka bramki desktopowego UI.

## Current State Analysis

Lokalny test rzeczywistego Release EXE przechodzi Home → Counter → 0 → 1. Aktualna publikacja na runnerze przechodzi build i pakowanie, lecz procesy WebView2 kończą się przed cleanupem, a host pozostaje uruchomiony. Ostatnie pełne próby: [normalna 42478901525](https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42478901525), snapshot `f0eee0653f9ce0d63bfb0696341444029ade6e30`, i [negatywna 42478904280](https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42478904280), snapshot `68ce060343c65766067fc92d8500b6b237355d5d`. Żadna nie dotarła do asercji Counter. F-01 pozostaje `implementing`, Progress 3.1–3.3 niezaznaczone.

Wykluczono badany brak dostępu standardowego tokenu do własnych katalogów i różnicę pulpitu hosta/child. Bezpośrednie discovery bez proxy potwierdziło utratę listenera. Usunięcie dodatkowego DISABLE_MAX_PRIVILEGE nie usunęło awarii. Nie znamy modułu, offsetu ani stosu; kod wyjścia nie dowodzi konkretnego Chromium CHECK. Nie powtarzać tych prób jako nowych hipotez bez nowych dowodów.

## Desired End State

Powstaje powiązany z dokładnym SHA i wersją obrazu raport określający: potwierdzoną przyczynę, zawężone miejsce awarii albo jawny wynik nierozstrzygający z brakującym dowodem. Każdy wniosek wskazuje własny PID i czas utworzenia, naturalne zakończenie przed cleanupem oraz pochodzenie zdarzenia lub stosu. Wyeksportowany raport nie zawiera surowej pamięci, argumentów funkcji ani danych innych procesów.

### Key Discoveries:

- `src/TimesheetAssistant.UiTests/OwnedProcessJob.cs:199`: otwierane są tylko zweryfikowane procesy prywatnego joba; zachowane uchwyty zapobiegają pomyleniu ponownie użytego PID.
- `src/TimesheetAssistant.UiTests/OwnedProcessJob.cs:219`: obecna obserwacja końca procesu zawiera kod, ale nie rzeczywisty czas utworzenia/wyjścia ani moduł awarii.
- `src/TimesheetAssistant.UiTests/DesktopAppFixture.cs:211`: diagnostykę trzeba zabezpieczyć przed usunięciem własnego profilu.
- `.github/workflows/windows.yml:87`: obecny upload całego katalogu desktop-ui nie nadaje się do przechowywania dumpów; diagnostyka natywna wymaga osobnej listy eksportowanych plików.
- ProcDump domyślnie ignoruje debug breakpoints; first-chance i breakpoint podłączenia debuggera nie są dowodem nieobsłużonej awarii.

## What We're NOT Doing

- Naprawy tokenu, CDP, aplikacji ani wydania na podstawie samej hipotezy; dalsze poprawki wymagają osobnego uzgodnienia.
- Zmiany main, release, tagów, globalnego WER/rejestru, ACL, polityk bezpieczeństwa, sandboxa Chromium, GPU lub istniejących timeoutów testu.
- Własny runner, przeglądarkowy zamiennik testu, nowe platformy, IntegrationTests, trwałość danych i test czystego urządzenia.
- Eksport dumpów, pełnych logów debuggera/zdarzeń, środowiska, command lines, pulpitu lub inwentarza obcych procesów.

## Implementation Approach

Kolejność: pasywne metadane → analiza istniejących raportów z własnego profilu → ograniczone przechwycenie konkretnego własnego PID tylko przy braku wystarczających dowodów. Osobny workflow uruchamiany ręcznie ma `contents: read`, `persist-credentials: false`, runner `windows-2025` i nie zawiera joba release. Buduje aktualny Release EXE z badanego SHA; stary publiczny pakiet nie stanowi dowodu odbioru.

Przy przyszłym uruchomieniu implementacji trzeba uzgodnić techniczne commity/pushe i uruchomienia tej diagnozy: wcześniejsze zgody i limity dotyczyły zakończonych eksperymentów. Ten plan nie przyznaje nowego budżetu poprawek. Przewiduje najwyżej jeden baseline metadanych i jeden run aktywnego przechwycenia, bez automatycznych ponowień awarii. Błąd przygotowania jest raportowany i zatrzymuje próbę.

## Critical Implementation Details

Najpierw zabezpieczyć raporty własnego profilu, dopiero potem usuwać profil. Nie przedłużać życia aplikacji poza dotychczasowy startup 60 s i cleanup 15 s; analiza offline może działać po zakończeniu testu.

Debugger może zmienić zachowanie procesu oraz wygenerować początkowy breakpoint. Przechwycony first-chance `0x80000003` wymaga korelacji z naturalnym zakończeniem i baseline; samo pierwsze trafienie ani systemowy moduł faulting nie dowodzą przyczyny.

## Phase 1: Identyfikacja procesów i metadane awarii

### Overview

Uzyskać identyfikację instancji oraz pasywne metadane bez aktywnego debuggera.

### Changes Required:

#### 1. Tożsamość i metadane

**Files**: `src/TimesheetAssistant.UiTests/OwnedProcessJob.cs`, `src/TimesheetAssistant.UiTests/DesktopAppFixture.cs`, nowy `scripts/diagnostics/Collect-WebView2CrashMetadata.ps1`.

**Intent**: Skorelować wyjście własnego procesu z raportem Windows i zabezpieczyć istniejące raporty WebView2 przed cleanupem.

**Contract**: Opcjonalny tryb diagnostyczny emituje `own-processes.json` z session ID, PID, basename, creation/exit FILETIME i UTC, czasem obserwacji, kodem wyjścia oraz flagą cleanup. GetProcessTimes korzysta z zachowanego uchwytu; obserwacja nie udaje czasu utworzenia. Zdarzenia Application 1000/1001 są ograniczone czasem sesji, a korelacja używa pól faulting process, nie System/Execution/ProcessID. Brak jednoznacznego PID/czasu utworzenia lub połączenia przez report ID oznacza unmatched; nie eksportować takiego zdarzenia. Lista pól raportu: klucz korelacji, czas, event/report ID, exception code, module basename/version, fault offset. Nie eksportować pełnego XML ani ścieżek użytkownika. Raporty crash z unikalnego własnego profilu zabezpieczyć wyłącznie w prywatnym katalogu runnera; brak pliku jest wynikiem diagnostycznym, nie sukcesem zbierania.

#### 2. Izolowane uruchomienie

**File**: nowy `.github/workflows/webview2-native-diagnostics.yml`.

**Intent**: Odtworzyć awarię z aktualnej publikacji bez ścieżki wydania.

**Contract**: Tylko workflow_dispatch na uzgodnionej gałęzi diagnostycznej; preflight potwierdza SHA, image version, WebView2 version oraz wersje i podpisy narzędzi. Nie używać sekretów podczas uruchamiania aplikacji. Katalog prywatnych danych poza upload root. Eksport jawnie nazwanych JSON/raportu/TRX i wybranych logów sesji, retencja 3 dni; żadnych wildcardów obejmujących dumpy. Job ma limit 45 min. Windows preview workflow pozostaje bez zmian.

### Success Criteria:

#### Automated Verification:

- Release build i lokalny desktop UI przechodzą; manifest odróżnia własne procesy i naturalne wyjścia od cleanupu.
- Kontrolowane dane sprawdzają odrzucenie obcego PID, ponownie użytego PID, brakujących pól i zdarzenia spoza sesji oraz poprawną korelację; parser nie eksportuje pełnych zdarzeń.
- Jeden baseline windows-2025 zapisuje SHA, wersje, manifest oraz dopasowane metadane lub jawny brak; upload zawiera wyłącznie dozwolone pliki, a cleanup kończy własne procesy i usuwa profil.

## Phase 2: Ograniczona analiza natywna

### Overview

Uzyskać moduł/offset i dostępny stos, jeśli pasywne metadane nie wyjaśniają awarii.

### Changes Required:

#### 1. Analiza i opcjonalne przechwycenie

**Files**: nowy `scripts/diagnostics/Analyze-WebView2Crash.ps1`, workflow diagnostyczny, opcjonalne podłączenie w fixture/job.

**Intent**: Najpierw analizować istniejący dump własnej sesji, a dopiero przy jego braku lub niewystarczającej treści przechwycić dokładną instancję procesu.

**Contract**: Preflight sprawdza faktyczną dostępność CDB i ProcDump, wersję/help oraz podpis Microsoft; brak narzędzia zatrzymuje etap, bez instalowania globalnego debuggera. Dump bez zgodnej tożsamości procesu nie jest analizowany jako dowód tej awarii. CDB analizuje lokalnie z ograniczonym cache symboli Microsoft. Eksport zawiera provenance, exception code, fault module/version/offset, maksymalnie 32 ramki module!function lub module+offset oraz status symboli; brak prywatnych symboli pozostaje jawny. Nie eksportować parametrów, pamięci, raw debugger output ani automatycznego pełnego !analyze.

Aktywne przechwycenie ma najwyżej dwa wybrane własne PID i jeden dump na PID, łącznie do 256 MiB prywatnych danych. Bezpośrednio przed attach ponownie sprawdzić uchwyt, job, creation time i żywy proces. ProcDump używa PID, ograniczenia liczby oraz breakpoint-aware first-chance filtra `80000003` (zweryfikować `-e 1 -b -f 80000003 -n 1 -mt` w wersji narzędzia). Watchdog kończy monitorowanie najpóźniej z końcem 60-sekundowego startupu; `-at` nie zastępuje tego limitu. Nie używać attach po nazwie, globalnego -i, -wer, -ma ani -k. Początkowy breakpoint debuggera musi być rozpoznany; samodzielnie nie stanowi dowodu. Wyjście przed attach, odmowa dostępu lub zmiana tożsamości oznacza brak przechwycenia, bez poszerzania zakresu.

Pełne logi i dumpy pozostają prywatne; allowlist parser odrzuca nieoczekiwane dane. Raport sanitizacji i `finally` potwierdzają usunięcie danych oraz zakończenie własnych helperów. Awaria diagnostyki nie zastępuje oryginalnego wyniku testu. Gdy dump przekroczy limit, symbole nie wystarczą lub attach zmieni objaw, zakończyć raportem nierozstrzygającym.

### Success Criteria:

#### Automated Verification:

- Kontrolowane przypadki potwierdzają limity PID/czasu/rozmiaru, odmowę attach przy złej tożsamości i brak niedozwolonych pól w raporcie; actionlint przechodzi dla workflow.
- Etap analizuje własny dump lub wykonuje najwyżej jeden uzgodniony run aktywny i zapisuje moduł/stos albo konkretny powód braku dowodu; raw dane nie trafiają do artefaktów, cleanup helperów i plików jest potwierdzony.

## Phase 3: Wniosek i dalszy krok dla F-01

### Overview

Oddzielić obserwację miejsca awarii od udowodnionej przyczyny i przekazać wynik do F-01.

### Changes Required:

#### 1. Raport i powiązanie

**Files**: nowy `context/changes/diagnose-webview2-native-crash/diagnosis.md`, `context/changes/verify-desktop-ui-path/change.md`, `context/deployment/deploy-plan.md`.

**Intent**: Zachować trwały, krótki raport po wygaśnięciu artefaktów i wskazać konkretny następny krok.

**Contract**: Raport zawiera badane SHA/run IDs/wersje, baseline i wpływ instrumentacji, pochodzenie każdego wniosku, sanitizowane moduły/ramki oraz klasyfikację: przyczyna potwierdzona, miejsce zawężone, nierozstrzygnięte. Każda hipoteza jest oznaczona; brak CHECK/symboli nie jest uzupełniany domysłem. Rekomendacja wskazuje jedną uzasadnioną zmianę do osobnego uzgodnienia albo dokładny brakujący dowód i jego zakres. Dopisać link w historii F-01 i sekcji wdrożenia, zachowując wcześniejsze wyniki, status implementing i nieodebrane kryteria 3.1–3.3. Raport diagnostyczny nie jest dowodem przejścia UI/release gate.

### Success Criteria:

#### Automated Verification:

- Raport wskazuje wszystkie wykonane runy i SHA, klasyfikuje wynik oraz uzasadnia dalszy krok; dokumentacja F-01 zachowuje status i niezaznaczone kryteria fazy 3.

#### Manual Verification:

- Autor potwierdza, że raport odróżnia dowody od hipotez i wybiera dalszy krok; żadna naprawa ani nowy eksperyment nie jest uruchamiany na podstawie samego raportu.

## Testing Strategy

### Unit Tests:

Sprawdzić istotne kontrakty korelacji/sanitizacji na syntetycznych danych, bez uruchamiania EXE czy WebView2 w UnitTests. Nie dodawać testów lustrzanych dla P/Invoke. Nie tworzyć IntegrationTests.

### UI Tests:

Zachować istniejący realny Counter test i jego asercję 1, timeouty oraz fail-on-cleanup. Lokalny build/test weryfikuje regresję fixture. Baseline i aktywne przechwycenie służą diagnozie; brak awarii pod debuggerem nie spełnia F-01.

### Manual Testing Steps:

1. Sprawdzić sanitizowany raport, zgodność instancji oraz wpływ podłączenia debuggera.
2. Potwierdzić rekomendację i oddzielnie uzgodnić ewentualne wykonanie naprawy lub poszerzenie zakresu.

## Performance Considerations

Nie zwiększać timeoutów aplikacji. Analiza offline ma limit 5 minut, cache symboli 512 MiB, prywatne dumpy 256 MiB; osiągnięcie limitu daje jawny wynik częściowy. Nie uruchamiać nieograniczonych pętli CI. Limity służą ograniczeniu diagnozy, nie SLA produktu.

## Migration Notes

Brak migracji danych. Zmiany diagnostyczne są opt-in i możliwe do usunięcia bez zmiany pakietu aplikacji. Zachować niezwiązane zmiany AGENTS.md i .agents/, nie stage całego drzewa. Nie edytować context/archive/. Plan zapisany bez commitów i bez uruchamiania CI.

## References

- [Historia F-01](../verify-desktop-ui-path/change.md) i [jego plan](../verify-desktop-ui-path/plan.md).
- [GetProcessTimes](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getprocesstimes): dokładna tożsamość czasowa instancji.
- [Application crash troubleshooting](https://learn.microsoft.com/troubleshoot/windows-server/performance/troubleshoot-application-service-crashing-behavior): metadane faulting module nie muszą oznaczać źródłowej przyczyny.
- [WebView2 process events and dumps](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/process-related-events): raporty we własnym profilu.
- [ProcDump](https://learn.microsoft.com/en-us/sysinternals/downloads/procdump): breakpoint filtering i ograniczenia triage dump.
- [CDB](https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/cdb-command-line-options), [stack commands](https://learn.microsoft.com/en-us/windows-hardware/drivers/debuggercmds/k--kb--kc--kd--kp--kp--kv--display-stack-backtrace-), [exception semantics](https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/controlling-exceptions-and-events), [public symbols](https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/microsoft-public-symbols).

## Progress

> Convention: pending/done tracked only here. Append commit SHA when a criterion lands; preserve titles.

### Phase 1: Identyfikacja procesów i metadane awarii

#### Automated

- [ ] 1.1 Release build i lokalny desktop UI przechodzą; manifest odróżnia własne procesy i naturalne wyjścia od cleanupu.
- [ ] 1.2 Kontrolowane dane sprawdzają odrzucenie obcego PID, ponownie użytego PID, brakujących pól i zdarzenia spoza sesji oraz poprawną korelację; parser nie eksportuje pełnych zdarzeń.
- [ ] 1.3 Jeden baseline windows-2025 zapisuje SHA, wersje, manifest oraz dopasowane metadane lub jawny brak; upload zawiera wyłącznie dozwolone pliki, a cleanup kończy własne procesy i usuwa profil.

### Phase 2: Ograniczona analiza natywna

#### Automated

- [ ] 2.1 Kontrolowane przypadki potwierdzają limity PID/czasu/rozmiaru, odmowę attach przy złej tożsamości i brak niedozwolonych pól w raporcie; actionlint przechodzi dla workflow.
- [ ] 2.2 Etap analizuje własny dump lub wykonuje najwyżej jeden uzgodniony run aktywny i zapisuje moduł/stos albo konkretny powód braku dowodu; raw dane nie trafiają do artefaktów, cleanup helperów i plików jest potwierdzony.

### Phase 3: Wniosek i dalszy krok dla F-01

#### Automated

- [ ] 3.1 Raport wskazuje wszystkie wykonane runy i SHA, klasyfikuje wynik oraz uzasadnia dalszy krok; dokumentacja F-01 zachowuje status i niezaznaczone kryteria fazy 3.

#### Manual

- [ ] 3.2 Autor potwierdza, że raport odróżnia dowody od hipotez i wybiera dalszy krok; żadna naprawa ani nowy eksperyment nie jest uruchamiany na podstawie samego raportu.
