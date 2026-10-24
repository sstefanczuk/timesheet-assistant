## Timesheet Assistant - MVP

### Główny problem
Manuale logowanie czasu pracy do dwóch systemów jest czasochłonne. Jeden z nich to Jira (i dodatek Clockwork Pro) w organizacji klienta. Drugi z nich to system wewnętrzny MyWork. Wymagane jest wejście w odpowiedni ticket na Jirze i dodanie czasu pracy. Obejmuje to zarówno zwykłe tickety z bieżącymi zadaniami, jak i dedykowane tickety na Scrum Ceremonies, Internal Communication, Knowledge Sharing, a nawet Holiday. MyWork oczekuje zalogowanych 8h, podczas gdy Jira 90% z tego czasu, czyli 7h 12m.

### Preferowana technologia
Osobista aplikacja desktopowa w .NET MAUI. 
Doświadczenie w .NET / C#: 13 lat
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