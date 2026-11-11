# F-01 — stan i powrót do pracy

Stan na 2026-11-11: **praca nad desktopowym CI odłożona, F-01 nieukończone**.
Status zmiany pozostaje `implementing`, a w roadmapie `in-progress`.

## Gdzie są zmiany

| Element | Lokalizacja |
| --- | --- |
| Lokalna gałąź z kandydatem workflow | `wip/f-01-desktop-ui-gate-20261111` |
| Dokładny commit | `8980a915eaa63338eda3060fb3529a36a96593c7` |
| Zakres tego commitu | Wyłącznie `.github/workflows/windows.yml`: test opublikowanego EXE, walidacja wyniku runnera/TRX i wysyłka diagnostyki. |
| Plan i kryteria odbioru | [plan.md](plan.md), faza 3 i Progress 3.1–3.3 |
| Pełna historia prób F-01 | [change.md](change.md) |
| Wynik diagnozy natywnej | [diagnosis.md](../diagnose-webview2-native-crash/diagnosis.md) |
| Warunki wdrożenia i publikacji | [deploy-plan.md](../../deployment/deploy-plan.md) |
| Polecenia lokalnych testów UI | [README projektu UiTests](../../../src/TimesheetAssistant.UiTests/README.md) |

Gałąź i commit checkpointu nie zostały wypchnięte na GitHub. Nie mają jeszcze
działającego odnośnika WWW i nie należy zakładać, że będą dostępne po nowym klonowaniu
repozytorium. Pozostają w tym lokalnym repozytorium; zachować gałąź do powrotu do
diagnozy. Ewentualny push należy uzgodnić osobno. Ta notatka nie uruchamia CI.

## Co działa, co pozostaje otwarte

- Fazy 1–2 mają zapisane dowody lokalnego uruchomienia rzeczywistego Release EXE,
  przejścia Home → Counter → 0 → 1 oraz kontrolowanego błędu z cleanupem.
- Fixture testów i narzędzia diagnostyki natywnej są już na lokalnym `main`;
  checkpoint zachowuje tylko niewłączony kandydat workflow. Wspierające commity
  diagnozy: `29a3054` i `49d90c5`.
- Na `windows-2025` własne procesy WebView2 kończą się naturalnie kodem
  `0x80000003` przed połączeniem ze stroną. Nie ustalono modułu, stosu ani
  przyczyny źródłowej. Pasywny [baseline 42572422837](https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42572422837)
  potwierdził ten objaw bez debuggera.
- Finalna [próba aktywna 42578053182](https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42578053182)
  zakończyła analizę statusem `budget-exhausted`, `Analyses=[]`. Dokładny
  przekroczony limit nie został zapisany. Cleanup potwierdzono; pamięci nie
  eksportowano. Wynik pozostaje nierozstrzygający.
- Nadal brakuje obu dowodów CI: udanej interakcji na opublikowanym EXE oraz
  kontrolowanej awarii asercji blokującej wydanie i zachowującej diagnostykę.
  Progress 3.1–3.3 pozostaje niezaznaczone.
- `main` zachowuje wcześniejszy workflow build/package/release bez bramki UI.
  Zielony build nie potwierdza desktopowego CI. Dotychczasowe ustawienia
  automatycznych technicznych prerelease pozostają niezmienione.
- Testy lokalne pozostają wymagane; trwałość i izolacja danych należą do S-01.
  Ta notatka nie zmienia zależności S-01 od F-01 w roadmapie ani kryteriów odbioru.

## Jak obejrzeć i odzyskać kandydata

Polecenia wykonywać z katalogu głównego repozytorium. Sam podgląd nie zmienia plików:

```powershell
git show --stat 8980a915eaa63338eda3060fb3529a36a96593c7
git diff 8980a91^ 8980a91 -- .github/workflows/windows.yml
```

Przy faktycznym wznowieniu, po sprawdzeniu czystego katalogu roboczego, utworzyć
nową gałąź z aktualnego `main` i przywrócić wyłącznie workflow z checkpointu:

```powershell
git switch main
git switch -c fix/f-01-desktop-ci
git restore --source=8980a915eaa63338eda3060fb3529a36a96593c7 -- .github/workflows/windows.yml
git diff -- .github/workflows/windows.yml
```

Ostatnia operacja przywraca pełną wersję pliku z checkpointu. Porównać ją ze
zmianami workflow wprowadzonymi od tego czasu i zachować potrzebne nowsze ustawienia.
Pozostawienie aktualnego kodu aplikacji i testów pozwala kontynuować po kolejnych
funkcjach; starego checkpointu nie traktować jako gotowej gałęzi do scalenia.

## Pierwszy krok przy wznowieniu

Przeczytać [rekomendację raportu](../diagnose-webview2-native-crash/diagnosis.md#rekomendowany-dalszy-krok):
osobno uzgodnić zmianę zapisującą dokładny warunek przerwania analizy i zmierzone
liczniki, zachowując istniejące limity i sanitizację. Najpierw sprawdzić ją na
danych kontrolowanych; dopiero potem zdecydować o zakresie kolejnej próby CI.
Wcześniejsze jednorazowe zgody na runy i limity korekt zostały wykorzystane.
Nie podnosić limitów ani nie ponawiać eksperymentów na podstawie samej hipotezy.

Artefakty CI mają retencję trzech dni. Trwałe wartości i granice dowodów są zapisane
w raporcie diagnozy; lokalne kopie pod `artifacts/` są ignorowane przez Git.
