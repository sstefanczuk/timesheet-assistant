# Diagnoza natywnej awarii WebView2 — Plan Brief

> Full plan: [plan.md](plan.md)

## What & Why

Ustalić moduł, stos lub przyczynę zakończenia WebView2 kodem `0x80000003` na windows-2025. Bez tego kolejne zmiany tokenu lub połączenia byłyby spekulacją, a F-01 nadal nie ma dowodu CI.

## Starting Point

Lokalny test Release EXE przechodzi. Ostatnie pełne runy 42478901525 i 42478904280 potwierdzają naturalne zakończenie własnych procesów WebView2 przed cleanupem; host pozostaje żywy, a asercje UI nie są osiągane. Dwa uzgodnione budżety poprawek zostały wyczerpane.

## Desired End State

Raport pozwala rozróżnić potwierdzoną przyczynę, zawężone miejsce awarii i wynik nierozstrzygający. Każdy wniosek ma źródło, tożsamość instancji i konkretny następny krok; diagnoza nie zamyka bramki F-01.

## Key Decisions Made

| Decision | Choice | Why | Source |
| --- | --- | --- | --- |
| Runner | windows-2025, aktualna publikacja badanego SHA | Zachowuje warunki awarii. | F-01 / Plan |
| Kolejność | Metadane → istniejący własny dump → opcjonalny attach | Ogranicza wpływ debuggera. | Research / Plan |
| Tożsamość | Prywatny job, zachowany uchwyt, PID + creation FILETIME | Chroni przed obcą instancją i ponownym użyciem PID. | Research / Plan |
| Dane | Analiza lokalna na runnerze, eksport allowlist raportu | Dump triage nadal zawiera pamięć. | Użytkownik / Research |
| Zakres wykonania | Jeden baseline i najwyżej jeden run aktywny, osobne uzgodnienie wykonania | Plan nie odnawia budżetu poprawek. | Użytkownik / Plan |
| Odbiór | Wynik nierozstrzygający jest jawny, F-01 pozostaje otwarte | Brak symboli nie uprawnia do wskazania przyczyny. | F-01 / Plan |

## Scope

**In scope:** manifest własnych procesów; korelacja zdarzeń; analiza dumpów własnej sesji; ograniczone przechwycenie PID; sanitizowany raport; powiązanie z historią F-01.

**Out of scope:** naprawy produktu/tokenu/CDP, main/release/tagi, globalny debugger/WER/rejestr, zmiany sandboxa/GPU/timeoutów, raw dump upload, własny runner, persistence i IntegrationTests.

## Architecture / Approach

Osobny ręczny workflow bez release buduje aktualny EXE. Fixture zabezpiecza metadane i istniejące raporty przed cleanupem; skrypty analizują je prywatnie i eksportują jedynie dozwolone pola. Aktywny debugger jest fallbackiem wymagającym korelacji z baseline, ponieważ może sam wygenerować breakpoint.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Identyfikacja procesów i metadane awarii | Manifest, dopasowane zdarzenia lub jawny brak | Chromium może nie dostarczyć WER. |
| 2. Ograniczona analiza natywna | Moduł/offset, do 32 ramek lub powód braku dowodu | Attach zmienia objaw; brak symboli/narzędzi. |
| 3. Wniosek i dalszy krok dla F-01 | Raport i jedna uzasadniona rekomendacja | Miejsce awarii nie musi być przyczyną. |

**Prerequisites:** Uzgodnienie wykonania technicznych commitów/pushy i runów; .NET/MAUI i WebView2; preflight narzędzi na rzeczywistym obrazie. Zatwierdzono obecnie zapis planu.
**Estimated effort:** Około 2–3 sesje diagnostyczne; bez gwarancji identyfikacji prywatnego Chromium CHECK.

## Open Risks & Assumptions

- WER lub własny profil mogą nie zawierać raportu; brak oznacza wynik diagnostyczny.
- CDB/ProcDump i symbole wymagają preflight; niedostępność zatrzymuje etap bez globalnych zmian.
- Limity: startup 60 s, cleanup 15 s; maksymalnie dwa PID i jeden dump na PID, 256 MiB dumpów, 512 MiB cache symboli, analiza 5 min, job 45 min.
- Surowe dumpy/logi pozostają prywatne i są usuwane; publikowane wybrane pliki mają retencję 3 dni.

## Success Criteria (Summary)

- Dowody są powiązane z konkretną własną instancją i SHA, a obce/niejednoznaczne rekordy odrzucone.
- Raport zawiera moduł/stos lub dokładny brak dowodu, bez eksportu pamięci i parametrów funkcji.
- Autor ocenia rekomendację; F-01 zachowuje nieodebraną fazę 3 do osobnej weryfikacji UI w CI.
