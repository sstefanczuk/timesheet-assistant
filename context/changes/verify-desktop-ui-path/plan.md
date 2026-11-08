# Sprawdzalna ścieżka uruchomienia aplikacji — Implementation Plan

## Overview

F-01 umożliwia automatyczne uruchomienie rzeczywistej aplikacji Windows i interakcję z jej WebView2 na odizolowanym profilu lokalnie oraz w CI. Odblokowuje weryfikację S-01 i dalszych funkcji MVP.

## Current State Analysis

Aplikacja jest starterem MAUI Blazor Hybrid, Windows-only, unpackaged. Projekt xUnit jest pusty; nie ma Playwright, fixture uruchamiającej aplikację ani izolacji profilu. Workflow buduje Release win-x64, wykonuje puste discovery, audytuje zależności i publikuje samowystarczalny pakiet. Pakiet był ręcznie sprawdzony na komputerze autora, lecz uruchomienie UI na runnerze nie zostało potwierdzone.

Roadmapa i tech-stack rozstrzygają użycie Playwright .NET z rzeczywistym WebView2. Użytkownik zaakceptował tymczasowy test Counter, zastępowany w S-01 testem zapisywania pracy. W razie niepowodzenia na windows-2025 po diagnozie i rozsądnych poprawkach zatrzymujemy odbiór F-01 i uzgadniamy dalszy krok; nie przechodzimy samodzielnie na własny runner.

## Desired End State

Jeden powtarzalny test uruchamia opublikowany EXE Release, rozpoznaje Home, przechodzi przez nawigację do Counter i sprawdza zmianę 0 → 1. Każde uruchomienie ma własny profil WebView2, ograniczony czas oczekiwania i cleanup własnych procesów. Ten sam test działa na windows-2025; błąd blokuje wydanie, a raporty pozwalają rozróżnić błąd startu, połączenia i interakcji. F-01 pozostaje nieukończone bez dowodu CI.

### Key Discoveries:

- `src/TimesheetAssistant/TimesheetAssistant.csproj:4`: zachować TargetFrameworks i Windows-only; `:33` określa unpackaged.
- `src/TimesheetAssistant/MauiProgram.cs:19`: developer tools są tylko w DEBUG; nie zakładać, że jest to warunek ani gotowe rozwiązanie CDP dla Release.
- `src/TimesheetAssistant/Components/Pages/Counter.razor:5`: status licznika i przycisk pozwalają sprawdzić rzeczywistą interakcję z C#.
- `src/TimesheetAssistant/Components/Layout/NavMenu.razor.css:84`: wąska szerokość ukrywa nawigację; test musi otworzyć menu, jeśli link Counter jest ukryty.
- `scripts/Package-Windows.ps1:16`: istnieje folder publish; wykorzystać jego EXE zamiast drugiego publikowania dla testu.
- `.github/workflows/windows.yml:65`: release zależy od build; błąd smoke w tym jobie może zablokować wydanie.

## What We're NOT Doing

- Funkcje domenowe, baza danych i test trwałości po restarcie — to S-01.
- Utrzymywanie testów stron startera jako docelowego pokrycia; Weather nie wchodzi w zakres.
- Osobny serwer, osobna przeglądarka zamiast aplikacji, nowe platformy lub nowy kanał dystrybucji.
- Własny runner, czyste urządzenie, automatyczny updater i zdalny dostęp do debugowania poza loopback.

## Implementation Approach

W fazie 1 przemianować pusty TimesheetAssistant.Tests na TimesheetAssistant.UnitTests, dodać TimesheetAssistant.UiTests i zaktualizować solucję. Tylko UiTests otrzymuje Microsoft.Playwright i fixture zarządzającą procesem aplikacji. Nie tworzyć IntegrationTests. Ścieżkę EXE przekazywać przez `TIMESHEET_ASSISTANT_TEST_APP`; brak konfiguracji lub pliku jest błędem, nie pominięciem. Fixture ustawia wyłącznie środowisko procesu potomnego: unikalny `WEBVIEW2_USER_DATA_FOLDER` i argument CDP w `WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS`. Łączy się przez `Chromium.ConnectOverCDPAsync` z lokalnym endpointem uruchomionej instancji. Nie instaluje osobnego Chromium do tego scenariusza.

UnitTests sprawdza czystą logikę bez Playwright, WebView2, EXE i rzeczywistego magazynu. UiTests sprawdza pełne przepływy aplikacji; od S-01 także jawne scenariusze zapisu, restartu i odczytu. CI uruchamia projekty osobno: UnitTests przed pakowaniem, UiTests po pakowaniu, bez filtrów kategorii. Raport musi wykazać wykonanie przynajmniej jednego testu desktopowego; puste discovery nie jest sukcesem F-01.

## Critical Implementation Details

Zmienne profilu i CDP muszą być przekazane przed startem WebView2. Nie modyfikować środowiska użytkownika ani całego test runnera. Wybór wolnego portu ma wyścig ze startem; obsłużyć kolizję jako ograniczony błąd startu i nie podłączać się do obcego procesu. Dostęp CDP pozostaje na loopback.

Profil WebView2 nie jest katalogiem danych domenowych. S-01 musi dodać izolację przyszłego magazynu przed testami zapisującymi wpisy. Cleanup kończy tylko proces aplikacji i należące do niej procesy potomne; usunięcie katalogu profilu następuje po ich wyjściu, wyłącznie we własnym katalogu testowym.

## Phase 1: Uruchomienie i połączenie

### Overview

Przygotować powtarzalny start Release i połączenie z rzeczywistym WebView2.

### Changes Required:

#### 1. Zależność i fixture

**Files**: `TimesheetAssistant.sln`, przemianowanie `src/TimesheetAssistant.Tests/` i jego csproj na `src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj`, nowy `src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj` oraz fixture, `.github/workflows/windows.yml` (aktualizacja starej ścieżki testów).

**Intent**: Dodać aktualną stabilną wersję Microsoft.Playwright zgodną z projektem i zarządzanie cyklem życia aplikacji bez referencji do projektu MAUI.

**Contract**: `TIMESHEET_ASSISTANT_TEST_APP` wskazuje EXE; osobny projekt UiTests; unikalny profil na sesję; brak równoległego współdzielenia instancji. Startup ma limit 60 s, operacje UI 15 s, cleanup 15 s; limity konfigurowalne w jednym miejscu fixture. Brak EXE, wyjście procesu, kolizja portu lub timeout powodują czytelny błąd. Połączenie dotyczy wyłącznie testowanej instancji. Normalne uruchomienie aplikacji nie wymaga trybu debugowania.

#### 2. Lokalna publikacja

**File**: `src/TimesheetAssistant.UiTests/README.md`.

**Intent**: Udokumentować przygotowanie EXE zgodnego z konfiguracją pakietu i konfigurację test runnera.

**Contract**: Release, win-x64, SelfContained i WindowsAppSDKSelfContained; unikalny ignorowany katalog wyjściowy oraz ścieżka EXE przekazywana do testu. Wymagany Windows i WebView2 Evergreen. Nie zmieniać skryptu wydania tylko po to, by lokalny eksperyment tworzył release.

### Success Criteria:

#### Automated Verification:

- `dotnet build TimesheetAssistant.sln` przechodzi z zachowanym TargetFrameworks Windows-only.
- Uruchomienie fixture na lokalnym EXE Release uzyskuje stronę rzeczywistego WebView2 w limicie 60 s i kończy własną instancję.
- Błędna ścieżka EXE i niedostępny endpoint kończą się jednoznacznym błędem w ograniczonym czasie, bez osieroconej instancji.

## Phase 2: Test interakcji i diagnostyka

### Overview

Potwierdzić działanie mechanizmu na tymczasowym scenariuszu startera.

### Changes Required:

#### 1. Smoke i dowody błędu

**Files**: `src/TimesheetAssistant.UiTests/`, `src/TimesheetAssistant.UiTests/README.md`.

**Intent**: Sprawdzić renderowanie i interakcję, a przy błędzie zachować wystarczający kontekst do diagnozy.

**Contract**: Home z nagłówkiem `Hello, world!` → link Counter (otworzyć zwinięte menu, jeśli potrzebne) → nagłówek Counter → status `Current count: 0` → przycisk `Click me` → status `Current count: 1`. Locatory korzystają z ról i dostępnych nazw. Nie stosować sztywnych opóźnień. Wyniki w `artifacts/desktop-ui/`: TRX, log startu/PID/etapu/endpointu i przy błędzie zrzut oraz trace, jeśli połączenie było dostępne. Błąd zapisu diagnostyki nie może ukryć pierwotnego błędu testu.

Dokumentacja oznacza Counter jako tymczasowy; S-01 zastępuje ten scenariusz testem zapisywania pracy, zachowując fixture. Raport nie deklaruje trwałości wpisów.

### Success Criteria:

#### Automated Verification:

- Dwa kolejne uruchomienia `dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj --logger trx` z ustawioną ścieżką EXE przechodzą scenariusz 0 → 1 na różnych profilach.
- Wymuszony błąd asercji zachowuje TRX, diagnostykę startu i zrzut/trace dostępnej strony; cleanup działa także po błędzie.
- Audyt `dotnet list TimesheetAssistant.sln package --vulnerable --include-transitive --source https://api.nuget.org/v3/index.json` jest wykonany, a ewentualne wyniki jawnie ocenione.

#### Manual Verification:

- Autor potwierdza widoczne przejście Home → Counter i zmianę 0 → 1; po teście zwykła aplikacja nadal uruchamia się poprawnie, a niezależne sesje WebView2 pozostają otwarte.

**Implementation Note**: Po automatycznej weryfikacji tej fazy uzyskać potwierdzenie powyższego kryterium ręcznego przed kolejną fazą.

## Phase 3: CI i dokumentacja

### Overview

Włączyć test desktopowy do istniejącego workflow i uzyskać dowód na runnerze.

### Changes Required:

#### 1. Bramka wydania

**File**: `.github/workflows/windows.yml`.

**Intent**: Sprawdzać opublikowaną aplikację przed udostępnieniem artefaktu wydania.

**Contract**: Zachować restore/build/audyt i pakowanie; przed pakowaniem uruchamiać `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj`. Po pakowaniu uruchomić `dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj` na `artifacts/publish-<version>/TimesheetAssistant.exe`; wymagać co najmniej jednego wykonanego testu oraz zera błędów. Sprawdzić dostępność WebView2; brak runtime ma dać czytelną diagnozę, nie skip. Diagnostykę przesyłać osobnym artefaktem także po błędzie, z retencją 3 dni. Job release nadal zależy od sukcesu joba build. Nie włączać debugowania globalnie ani w wydawanym pakiecie.

#### 2. Stan weryfikacji

**Files**: `src/TimesheetAssistant.UnitTests/README.md`, `src/TimesheetAssistant.UiTests/README.md`, `AGENTS.md`, `context/deployment/deploy-plan.md`.

**Intent**: Zastąpić nieaktualne stwierdzenia o braku testów rzeczywistym zakresem i dowodem.

**Contract**: Opisać osobne polecenia obu projektów testowych, wymagane zmienne i artefakty. Dopisać wynik eksperymentu CI z odnośnikiem do runa; zachować historyczne wyniki i granice (brak weryfikacji trwałości i czystego urządzenia). Nie zmieniać plików w archive/.

### Success Criteria:

#### Automated Verification:

- Run na windows-2025 wykonuje co najmniej jeden test DesktopUI na opublikowanym EXE Release i kończy się sukcesem; dostępny jest TRX z wynikiem testu.
- Kontrolowane niepowodzenie testu w CI kończy job build błędem, uniemożliwia job release i zachowuje dostępne artefakty diagnostyczne.
- Dokumentacja opisuje aktualny zakres testów, lokalne polecenia i odnośnik do dowodu CI bez deklarowania trwałości danych.

Jeżeli uruchomienie nie powiedzie się mimo diagnozy i rozsądnych poprawek, zapisać wynik, pozostawić F-01 nieukończone i uzgodnić z użytkownikiem dalszy krok. Własny runner i zastąpienie testu desktopowego testem przeglądarkowym nie są zatwierdzone.

## Testing Strategy

### Unit Tests:

Nie dodawać testów odtwarzających wewnętrzną implementację fixture. Błędy startu i cleanup sprawdzić kontrolowanymi próbami z faz 1–2; przyszłe testy czystej logiki trafiają do UnitTests. Nie dodawać sztucznych testów tylko dla dodatniego discovery.

### UI Tests:

Jeden test rzeczywistego procesu Release i WebView2, dwukrotnie lokalnie i w CI. Wynik runnera z zerem testów nie spełnia odbioru. Powtórzenia nie mogą maskować błędu asercji. Testować brak EXE, timeout, błąd asercji i cleanup. Nie tworzyć osobnego IntegrationTests. Od S-01 UiTests jawnie sprawdza zapis, restart i odczyt; sama obecność zestawu UI nie potwierdza tego pokrycia.

### Manual Testing Steps:

1. Obserwować lokalny test i potwierdzić nawigację oraz kliknięcie.
2. Po zakończeniu uruchomić zwykłą aplikację i sprawdzić, czy inne otwarte sesje WebView2 pozostały dostępne.

## Performance Considerations

Oczekiwania są ograniczone limitami fixture; nie ustalamy SLA produktu. Korzystamy z istniejącej publikacji i jednego scenariusza, bez dodatkowej instalacji Chromium. Nie zwiększać timeoutów bez diagnozy.

## Migration Notes

Brak danych do migracji. Test nie zapisuje wpisów pracy. Wycofanie zmiany usuwa fixture, zależność i krok CI bez zmiany danych użytkownika. W S-01 zastąpić Counter scenariuszem domenowym i dodać izolację magazynu aplikacji przed testami zapisu.

## References

- `context/foundation/roadmap.md`: F-01, S-01 i granice weryfikacji.
- `context/foundation/tech-stack.md`: Playwright .NET z rzeczywistym WebView2.
- `context/deployment/deploy-plan.md`: istniejąca publikacja i ręczny test pakietu.
- [Playwright .NET WebView2](https://playwright.dev/dotnet/docs/webview2).
- [Microsoft WebView2 environment overrides](https://learn.microsoft.com/en-us/microsoft-edge/webview2/reference/win32/webview2-idl).

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles.

### Phase 1: Uruchomienie i połączenie

#### Automated

- [x] 1.1 `dotnet build TimesheetAssistant.sln` przechodzi z zachowanym TargetFrameworks Windows-only. — b77224a
- [x] 1.2 Uruchomienie fixture na lokalnym EXE Release uzyskuje stronę rzeczywistego WebView2 w limicie 60 s i kończy własną instancję. — b77224a
- [x] 1.3 Błędna ścieżka EXE i niedostępny endpoint kończą się jednoznacznym błędem w ograniczonym czasie, bez osieroconej instancji. — b77224a

### Phase 2: Test interakcji i diagnostyka

#### Automated

- [x] 2.1 Dwa kolejne uruchomienia `dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj --logger trx` z ustawioną ścieżką EXE przechodzą scenariusz 0 → 1 na różnych profilach.
- [x] 2.2 Wymuszony błąd asercji zachowuje TRX, diagnostykę startu i zrzut/trace dostępnej strony; cleanup działa także po błędzie.
- [x] 2.3 Audyt `dotnet list TimesheetAssistant.sln package --vulnerable --include-transitive --source https://api.nuget.org/v3/index.json` jest wykonany, a ewentualne wyniki jawnie ocenione.

#### Manual

- [x] 2.4 Autor potwierdza widoczne przejście Home → Counter i zmianę 0 → 1; po teście zwykła aplikacja nadal uruchamia się poprawnie, a niezależne sesje WebView2 pozostają otwarte.

### Phase 3: CI i dokumentacja

#### Automated

- [ ] 3.1 Run na windows-2025 wykonuje co najmniej jeden test DesktopUI na opublikowanym EXE Release i kończy się sukcesem; dostępny jest TRX z wynikiem testu.
- [ ] 3.2 Kontrolowane niepowodzenie testu w CI kończy job build błędem, uniemożliwia job release i zachowuje dostępne artefakty diagnostyczne.
- [ ] 3.3 Dokumentacja opisuje aktualny zakres testów, lokalne polecenia i odnośnik do dowodu CI bez deklarowania trwałości danych.
