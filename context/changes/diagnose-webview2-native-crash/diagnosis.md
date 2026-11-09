# Wynik diagnozy natywnej awarii WebView2

Data: 2026-11-09. **Klasyfikacja: nierozstrzygnięte.** Własne procesy WebView2 kończą się naturalnie z `0x80000003` na `windows-2025`, także bez debuggera. Nie ustalono modułu, offsetu, stosu ani konkretnego CHECK; nie potwierdzono przyczyny źródłowej. Ograniczona próba aktywna zakończyła analizę statusem `budget-exhausted` bez wyników stosu.

Raport opisuje wykonaną diagnozę; nie zamyka bramki UI. F-01 pozostaje `implementing`, a kryteria 3.1–3.3 w [jego planie](../verify-desktop-ui-path/plan.md) pozostają niezaznaczone. Żaden opisany run nie potwierdza przejścia Home → Counter → 0 → 1 w CI ani kontrolowanej awarii asercji Counter. Nie wykonano weryfikacji trwałości danych ani czystego urządzenia.

## Zakres i pochodzenie

Diagnoza następuje po nieudanych pełnych próbach F-01: [normalnej 42478901525](https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42478901525), SHA `f0eee0653f9ce0d63bfb0696341444029ade6e30`, oraz [negatywnej 42478904280](https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42478904280), SHA `68ce060343c65766067fc92d8500b6b237355d5d`. Ich wcześniejszą historię zachowuje [change.md F-01](../verify-desktop-ui-path/change.md).

Poniższa tabela obejmuje wszystkie runy tej zmiany diagnostycznej. Każdy snapshot był osobno zatwierdzony i wysłany wyłącznie na gałąź `ci/webview2-native-*`; nie zmieniano zdalnego main ani wydań. Baseline i finalna próba aktywna badały aktualny Release EXE publikowany z danego snapshotu, nie stary pakiet wydania; pozostałe runy zatrzymały się w preflighcie.

| Run | Dokładny SHA snapshotu | Wynik i granica obserwacji |
| --- | --- | --- |
| [42572422837](https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42572422837) | `f6496537c95d471a425e3ba7e31ac49ff1d18f55` | Pasywny baseline: dwa naturalne wyjścia `0x80000003`, bez zdarzeń Windows i bez raportów Crashpad. |
| [42575638296](https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42575638296) | `71542594daf2ac3c1d9be9b9c761da6c44e027fc` | Błąd walidacji RUNNER_TEMP względem kontowego TEMP; zatrzymanie przed wykrywaniem narzędzi i startem aplikacji. |
| [42576293870](https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42576293870) | `ca63e63601da7ef459807ee7d0011a5074290bbd` | Walidacja katalogu przeszła; `tools-unavailable`, brak ProcDump. Bez startu aplikacji i attach. |
| [42577085978](https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42577085978) | `05f83d1dd9a968e18d709e1750651f57bf39bc24` | Pobranie i weryfikacja przenośnego ProcDump przeszły; walidacja pomocy narzędzia zawiodła. Bez startu aplikacji i attach. |
| [42578053182](https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42578053182) | `9888a9a64427ba9a972097a59fefeed298ad826d` | Preflight przeszedł, próba dla dwóch własnych PID; naturalne wyjścia `0x80000003`, analiza `budget-exhausted`, brak modułu/stosu. |

Źródłem wartości są allowlistowane `preflight.json`, `own-processes.json`, `crash-metadata.json`, `profile-report-status.json`, `baseline-status.json` oraz — dla fazy 2 — `portable-procdump.json`, `native-tool-preflight.json`, `native-analysis.json` i `native-cleanup.json`. Lokalne kopie znajdują się w ignorowanych katalogach `artifacts/native-phase1-ci/baseline-42572422837/` i `artifacts/native-phase2-ci/{active-42575638296,fix1-42576293870,portable-42577085978,help-fix2-42578053182}/`. Artefakty CI mają retencję trzech dni; najważniejsze wartości utrwalono poniżej, aby wnioski nie zależały od ich dostępności.

Implementację pasywną zapisano w commicie `29a3054`, aktywną w `49d90c5`. Kontrakty interpretacji określają [plan](plan.md), [historia zgód i prób](change.md) oraz [analyzer z badanego snapshotu](https://github.com/sstefanczuk/timesheet-assistant/blob/9888a9a64427ba9a972097a59fefeed298ad826d/scripts/diagnostics/Analyze-WebView2Crash.ps1). Snapshoty eksperymentalne nie są dowodem odbioru fazy 3 F-01.

## Środowisko i narzędzia

Wszystkie pięć preflightów zapisało `ImageVersion=20261103.298.1`, `ImageOS=win25-vs2026` i WebView2 `156.0.4314.58` z podpisem `Valid`. Workflow wybierał `windows-2025`; SDK pochodził z repozytoryjnego `global.json` (`10.0.403`, workload set `10.0.402.1`). Preflight zapisał podpisane dotnet (`10,0,1126,37416 @Commit: e2f47b0110ed922f21a1522da67279133ce28f32`) i pwsh (`7.6.9.500`). Wersja pliku dotnet nie jest numerem SDK.

Po potwierdzeniu braku ProcDump użytkownik osobno zatwierdził zmianę pierwotnego ograniczenia do istniejących narzędzi: pobranie przenośnej wersji do prywatnego katalogu runnera, bez globalnej instalacji i zmiany PATH. Źródło: `https://download.sysinternals.com/files/Procdump.zip`; ProcDump `12.02`, podpis Microsoft `Valid`. Przypięte SHA-256:

- Archiwum: `5FE68E81632F13188930AFFFF20A9A9910170BAE654360850D3108CEAD708A2D`.
- `procdump64.exe`: `22C2A2B09ADE73A342A18DAB838354385F73FC98E324B9903FD61FFF949C5124`.

W finalnym runie CDB `10.0.26100.9457 (WinBuild.160101.0800)` i ProcDump przeszły kontrolę podpisów oraz wszystkich wymaganych przełączników (`MissingSwitches=[]`). CDB zwrócił pomoc jako UTF-8 bez BOM, ProcDump jako UTF-16LE bez BOM (16974 bajty stdout). To potwierdza problem dekodowania wcześniejszego preflightu, nie brak funkcji ProcDump. Zapis `Executed=false` w raporcie provisioning oznacza, że downloader nie uruchomił EXE; późniejsze zatwierdzone wywołanie pomocy z akceptacją EULA oraz capture rzeczywiście nastąpiły.

## Pasywny baseline

Sesja `9e81be002e1a4c659da1372a709c2011`, bez aktywnego debuggera. Dane `GetProcessTimes` pochodzą z zachowanych uchwytów procesów własnego joba; PID i rzeczywisty creation FILETIME identyfikują instancję. Poniższe naturalne wyjścia miały `Cleanup=false`:

| PID WebView2 | Creation FILETIME | Exit FILETIME | Wyjście UTC | Kod |
| --- | --- | --- | --- | --- |
| 4112 | `134386792967409933` | `134386792986743560` | `2026-11-09T06:28:18.674356Z` | `0x80000003` |
| 9096 | `134386792986981660` | `134386792990936140` | `2026-11-09T06:28:19.093614Z` | `0x80000003` |

Host PID 6252 (creation FILETIME `134386792947086972`) żył do cleanupu o `06:29:14.7083213Z`, a jego wyjście ma `Cleanup=true`. Pozostałe dwa procesy WebView2 zakończyły się kodem 0. Manifest potwierdza `CleanupComplete=true`; `baseline-status.json` potwierdza `PrivateEvidenceRemoved=true`, `TestExitCode=1` i `DesktopGateAccepted=false`. Test zatrzymał się na niezmienionym 60-sekundowym startupie.

Kolektor zwrócił `QueryStatus=no-events`, `Status=no-unambiguous-match`, `Events=[]`. Zabezpieczanie raportów własnego profilu zwróciło `Status=absent`, `Count=0`, `Bytes=0`. Są to braki dowodów w badanym oknie i profilu; nie dowodzą, że Windows nigdy nie zapisuje takich zdarzeń. Nie ma dopasowanego modułu ani offsetu z Application 1000/1001.

## Próba aktywna i wpływ instrumentacji

Finalna sesja `812c688c6bdd40b6be98462193dc9292` próbowała przechwycenia dokładnie dwóch własnych instancji. Ponownie zanotowano naturalne wyjścia (`Cleanup=false`):

| PID WebView2 | Creation FILETIME | Exit FILETIME | Wyjście UTC | Kod |
| --- | --- | --- | --- | --- |
| 9016 | `134386825975636163` | `134386826001512185` | `2026-11-09T07:23:20.1512185Z` | `0x80000003` |
| 7784 | `134386826001554771` | `134386826005552334` | `2026-11-09T07:23:20.5552334Z` | `0x80000003` |

Host PID 2096 (creation FILETIME `134386825942036596`) żył do cleanupu o `07:24:14.2051848Z`; dwa pozostałe procesy WebView2 zakończyły się kodem 0. `CleanupComplete=true`, `TestExitCode=1`, `DesktopGateAccepted=false`. Pasywne metadane ponownie miały `no-events`; dodatkowych raportów Crashpad z własnego profilu nie znaleziono (`Status=absent`, `Count=0`). Wartość `Bytes=769202` w `profile-report-status.json` obejmuje zapisane w tej sesji prywatne dumpy aktywne, nie znalezione raporty Crashpad.

`native-analysis.json` zachowuje wynik: `Status=budget-exhausted`, `Analyses=[]`, `Capture.Attempted=2`, `Capture.Status=monitoring-first-chance-ambiguous`, `InitialBreakpointExcluded=false`. Nie wyeksportowano żadnego modułu, wersji modułu, offsetu ani ramki; nie znamy stanu symboli dla konkretnego miejsca awarii. Brak wyników nie pozwala stwierdzić, że symbole były przyczyną przerwania.

Symptom wyjścia `0x80000003` występuje również bez debuggera, lecz aktywne dumpy mogły uchwycić first-chance lub początkowy breakpoint debuggera. Raport nie wykluczył tej możliwości. Zgodność kodu końca procesu z baseline nie dowodzi, że przechwycono tę samą instrukcję lub nieobsłużony wyjątek. Nie można na tej podstawie wskazać CHECK ani przyczyny.

Budżety wynosiły: najwyżej dwa PID i jeden dump/PID, prywatne dumpy 256 MiB, offline 300 s, cache symboli 512 MiB; analyzer ogranicza też cały katalog offline do 528 MiB. Startup 60 s i cleanup 15 s pozostały bez zmian. Watchdog rozmiaru może dopuścić chwilowe przekroczenie podczas zapisu. Raport nie zapisał, **który warunek budżetu zadziałał ani jakie wartości zmierzono w tej chwili**. Zapisane 769202 bajty nie wyjaśniają samodzielnie przerwania analizy. Wzrost cache lub katalogu offline pozostaje hipotezą, nie wynikiem.

## Eksport i cleanup

W finalnej próbie `HelpersStopped=true`, `PrivateAnalysisRemoved=true`, a `native-cleanup.json` i `baseline-status.json` potwierdzają `PrivateEvidenceRemoved=true`; `RawExported=false`. Cleanup prywatnych danych potwierdzono także w trzech przerwanych preflightach. Surowe dumpy, logi CDB i pomoc narzędzi zostały usunięte na runnerze, nie są częścią artefaktów i nie można ich odzyskać z tego raportu. Finalny artefakt zawierał dziesięć jawnie nazwanych plików: dziewięć wymienionych JSON i `desktop-ui.trx`. Nie eksportowano pamięci, argumentów funkcji ani informacji o obcych procesach.

Końcowy workflow pozostaje czerwony, ponieważ zachował pierwotne niepowodzenie desktop startupu; poprawne zebranie i posprzątanie diagnostyki nie zmienia wyniku testu.

## Rekomendowany dalszy krok

Do osobnego uzgodnienia proponuje się **jedną zmianę diagnostyczną w analyzerze: zapisać dokładny powód przerwania budżetu wraz z sanitizowanymi licznikami w chwili decyzji**. Zakres: etap analizy, liczba wybranych dumpów, łączne prywatne bajty sesji, czas offline, bajty cache i katalogu offline, odpowiadające im limity oraz nazwa naruszonego warunku. Raport nadal ma wykluczać pamięć, ścieżki użytkownika i raw output; dotychczasowe limity pozostają bez zmian. Ten brak telemetryczny uniemożliwia obecnie uzasadnienie, czy kolejna analiza ma szansę dostarczyć moduł/stos.

Najpierw należy uzgodnić tę zmianę i zweryfikować ją na danych kontrolowanych. Dopiero osobna decyzja użytkownika może dopuścić kolejny snapshot/push/run i jego zakres. Dotychczasowe jednorazowe zgody oraz limit dwóch korekt są wykorzystane; niniejszy raport nie uruchamia eksperymentu i nie przyznaje nowego budżetu. Nie rekomenduje się podnoszenia limitów ani ponawiania próby na podstawie hipotezy o cache. Brakującym docelowym dowodem pozostaje dopasowany do własnej instancji moduł/offset lub stos, z rozstrzygnięciem wpływu początkowego breakpointu; dopiero taki dowód może uzasadnić naprawę F-01.
