# Plan Testów – HabitFlow

## 1. Wprowadzenie i cele testowania

### 1.1 Cel dokumentu
Niniejszy dokument definiuje strategię i zakres testowania aplikacji HabitFlow – webowego trackera nawyków zbudowanego na platformie .NET 9 z wykorzystaniem Blazor Server i SQL Server. Plan testów ma na celu zapewnienie wysokiej jakości produktu MVP, weryfikację kluczowych funkcjonalności biznesowych oraz zapewnienie stabilności i bezpieczeństwa aplikacji.

### 1.2 Cele testowania
- Weryfikacja poprawności implementacji wszystkich wymagań funkcjonalnych ze specyfikacji PRD
- Zapewnienie poprawności logiki biznesowej, szczególnie obliczeń `success_rate` i `daily_score`
- Walidacja architektury Clean Architecture z separacją Command/Query (CQS)
- Potwierdzenie działania mechanizmów autoryzacji i uwierzytelniania (ASP.NET Core Identity)
- Weryfikacja integracji z bazą danych SQL Server przez Entity Framework Core
- Sprawdzenie wydajności kluczowych operacji (ekran "Dziś" ≤ 500ms przy 20 nawykach)
- Potwierdzenie działania ścieżki krytycznej end-to-end zgodnie z PRD

## 2. Zakres testów

### 2.1 W zakresie testowania (MUST)

#### Backend (HabitFlow.Api, HabitFlow.Core)
- **Autoryzacja i uwierzytelnianie**: rejestracja, weryfikacja e-mail, logowanie, reset hasła, wylogowanie, usuwanie konta
- **Zarządzanie nawykami (CRUD)**: tworzenie, odczyt, aktualizacja, usuwanie nawyków typu "start" i "stop"
- **Check-in dzienny**: jednorazowy wpis per nawyk/dzień, walidacja zakresu wartości, blokada duplikatów
- **Uzupełnianie historii**: wpisy wstecz do 7 dni
- **Obliczenia biznesowe**: `daily_score`, `success_rate`, próg 75%, logika dla `CompletionMode` (Binary/Quantitative)
- **Kalendarz readonly**: generowanie statusów dni (plan/wykonane/niewykonane)
- **Wykres postępu**: rolling success rate 7/30 dni
- **Powiadomienia AI**: generowanie przy "miss due", fallback na stałe szablony
- **Zarządzanie profilem**: strefa czasowa użytkownika
- **Walidacje i limity**: max 20 nawyków, limity pól, walidacja danych wejściowych
- **Dispatcher pattern**: `ICommandDispatcher`, `IQueryDispatcher` z rejestracją handlerów
- **Result pattern**: mapowanie `Result<T>` na odpowiedzi HTTP i `ProblemDetails` (RFC 7807)

#### Frontend (HabitFlow.Blazor)
- **Komponenty UI**: renderowanie komponentów MudBlazor, walidacja formularzy
- **Ekran "Dziś"**: wyświetlanie dzisiejszych zadań, szybki check-in
- **Interakcje użytkownika**: formularze z `EditForm` i `DataAnnotationsValidator`
- **Autoryzacja UI**: `[Authorize]`, `AuthorizeView`, `AuthenticationStateProvider`
- **Obsługa błędów**: `ErrorBoundary`, mapowanie `ProblemDetails` na komunikaty UI
- **Wydajność**: czas ładowania, minimalizacja re-renderów

#### Baza danych (HabitFlow.Data)
- **Migracje EF Core**: poprawność schematu, relacje między encjami
- **Konfiguracje encji**: `HabitConfiguration`, `CheckinConfiguration`, `NotificationConfiguration`
- **Zapytania**: `AsNoTracking`, projekcje do DTO, zapobieganie N+1
- **Transakcje**: `SaveChangesAsync`, spójność danych

### 2.2 Poza zakresem testowania MVP
- Testy obciążeniowe i stressowe (planowane po MVP)
- Testy bezpieczeństwa penetracyjne (OWASP)
- Testy dostępności (WCAG 2.1) – podstawowa zgodność weryfikowana manualnie
- Testy międzynarodowe (i18n/l10n) poza podstawową obsługą strefy czasowej
- Testy natywnych aplikacji mobilnych (nie ma w MVP)
- Integracje zewnętrzne (Google Calendar, eksport CSV)
- Gamifikacja i osiągnięcia
- Rate-limiting powiadomień pozytywnych (COULD dla S2)

## 3. Typy testów do przeprowadzenia

### 3.1 Testy jednostkowe (Unit Tests)
**Framework**: XUnit
**Biblioteka mock**: NSubstitute
**Lokalizacja**: `HabitFlow.Tests/UnitTests/`

#### Zakres testów jednostkowych:

##### Walidatory
Wszystkie walidatory powinny być przetestowane pod kątem:
- **Happy path**: poprawne dane przechodzą walidację
- **Przypadki błędów**: niepoprawne dane zwracają odpowiednie komunikaty błędów
- Przykładowe obszary walidacji:
  - Formaty e-mail, długości hasła (≥8 znaków)
  - Limity pól tekstowych (tytuł ≤80, opis ≤280)
  - Zakresy wartości liczbowych (powtórzenia 1-100)
  - Wymagane pola (dni tygodnia, typy nawyków)
  - Formaty dat i stref czasowych
  - Zakresy dat (np. uzupełnianie wstecz do 7 dni)

##### Command Handlers
Wszystkie handlery poleceń powinny być przetestowane pod kątem:
- **Happy path**: pomyślne wykonanie polecenia z poprawnymi danymi
- **Przypadki błędów**: obsługa błędów biznesowych i walidacyjnych
- Kluczowe scenariusze do pokrycia:
  - Autoryzacja: tworzenie/modyfikacja/usuwanie tylko własnych zasobów
  - Walidacje biznesowe (np. limit 20 nawyków, blokada duplikatów check-inów)
  - Obsługa błędów zewnętrznych (np. niedostępność e-mail service)
  - Efekty uboczne (np. wysyłka e-maila, generowanie powiadomień)
  - Usuwanie kaskadowe powiązanych danych
  - Obliczenia biznesowe (`daily_score` dla różnych trybów completion)

##### Query Handlers
Wszystkie handlery zapytań powinny być przetestowane pod kątem:
- **Happy path**: zwracanie poprawnych danych dla prawidłowych zapytań
- **Przypadki brzegowe**: puste wyniki, filtry bez dopasowań
- Kluczowe scenariusze do pokrycia:
  - Filtrowanie i sortowanie danych
  - Paginacja (page, pageSize)
  - Autoryzacja: dostęp tylko do własnych zasobów
  - Projekcje do DTO (zwracanie tylko potrzebnych kolumn)
  - Obliczenia w zapytaniach (np. success_rate, statusy kalendarza)
  - Uwzględnianie strefy czasowej użytkownika
  - Wykluczanie/włączanie powiązanych danych (np. wykonane check-iny)

##### Logika biznesowa (success_rate, daily_score)
Kluczowe algorytmy biznesowe powinny być szczegółowo przetestowane:
- **Obliczanie `daily_score`**:
  - Binary mode: 1.0 gdy `ActualValue > 0`, inaczej 0.0
  - Quantitative mode dla "start": `ActualValue / TargetValue` (clamped 0-1)
  - Quantitative mode dla "stop": `1 - (ActualValue / TargetValue)` (clamped 0-1)
- **Obliczanie `success_rate`**:
  - Suma `daily_score` / liczba dni zaplanowanych w oknie
  - success_rate = 0 gdy brak zaplanowanych dni
  - Rolling windows (7 i 30 dni)
  - Próg sukcesu 75% do deadline'u

##### Infrastructure
Komponenty infrastruktury powinny być przetestowane pod kątem:
- **Dispatchers**: rozwiązywanie handlerów z DI, obsługa błędów, propagacja cancellation tokens
- **Result mappers**: poprawne mapowanie `Result<T>` na odpowiedzi HTTP/IResult
- **Context services**: pobieranie danych użytkownika z claims, obsługa brakujących claims

**Pokrycie docelowe**: ≥80% dla warstwy Core (logika biznesowa)

### 3.2 Testy integracyjne (Integration Tests)
**Framework**: XUnit
**Konteneryzacja**: TestContainers (SQL Server)
**Klient HTTP**: wygenerowany przez NSwag z `openapi.json`
**Lokalizacja**: `HabitFlow.Tests/IntegrationTests/`

#### Infrastruktura testowa:
- `IntegrationTestFactory`: `WebApplicationFactory<Program>` z konfiguracją TestContainers
- `IntegrationTestFixture`: współdzielony kontener SQL Server dla wszystkich testów
- **Strategia bazy**: jedna baza danych dla wszystkich testów, uruchamianie równoległe, izolacja przez unikalne dane użytkowników

#### Zakres testów integracyjnych:

##### AuthEndpoints
- POST `/api/v1/auth/register`: sukces 201, duplikat e-mail 409
- POST `/api/v1/auth/confirm-email`: sukces 200, niepoprawny token 400
- POST `/api/v1/auth/login`: sukces 200 z cookie, błędne dane 401, niezweryfikowany e-mail 403
- POST `/api/v1/auth/forgot-password`: sukces 200, nieistniejący e-mail nadal 200 (bezpieczeństwo)
- POST `/api/v1/auth/reset-password`: sukces 200, niepoprawny token 400
- POST `/api/v1/auth/logout`: sukces 200, wyczyszczenie sesji
- DELETE `/api/v1/auth/account`: sukces 204, usunięcie kaskadowe

##### ProfileEndpoints
- GET `/api/v1/profile`: sukces 200, wymagana autoryzacja 401
- PUT `/api/v1/profile/timezone`: sukces 200, walidacja strefy czasowej 400

##### HabitEndpoints
- POST `/api/v1/habits`: sukces 201, walidacja pól 400, limit 20 nawyków 409
- GET `/api/v1/habits`: sukces 200 z paginacją, sortowanie, filtrowanie
- GET `/api/v1/habits/{id}`: sukces 200, nie znaleziono 404, cudzy nawyk 403/404
- PUT `/api/v1/habits/{id}`: sukces 200, walidacja 400, cudzy nawyk 403
- DELETE `/api/v1/habits/{id}`: sukces 204, cudzy nawyk 403
- GET `/api/v1/habits/{id}/calendar`: sukces 200, poprawne statusy dni

##### CheckinEndpoints
- POST `/api/v1/checkins`:
  - Sukces 201
  - Duplikat (ten sam dzień) 409
  - Wartość poza zakresem 400
  - Data starsze niż 7 dni 400
  - Cudzy nawyk 403
- GET `/api/v1/checkins`: sukces 200, filtrowanie po nawyku i dacie
- GET `/api/v1/checkins/by-date`: sukces 200, grupowanie po dniu

##### TodayEndpoints
- GET `/api/v1/today`:
  - Sukces 200
  - Poprawne filtrowanie według dnia tygodnia
  - Uwzględnienie strefy czasowej
  - Wydajność ≤ 500ms przy 20 nawykach (test performance)

##### ProgressEndpoints
- GET `/api/v1/progress/{habitId}/rolling?window=7`: sukces 200, poprawne obliczenia
- GET `/api/v1/progress/{habitId}/rolling?window=30`: sukces 200, poprawne obliczenia

##### NotificationEndpoints
- GET `/api/v1/notifications`: sukces 200, paginacja
- Weryfikacja generowania powiadomień przy "miss due" (test z background jobem)

**Pokrycie docelowe**: 100% endpointów API z kluczowymi scenariuszami (happy path + error cases)

### 3.3 Testy komponentów UI (bUnit)
**Framework**: bUnit
**Lokalizacja**: `HabitFlow.Tests/ComponentTests/` (do utworzenia)

#### Zakres testów komponentów:

##### Habits
- `HabitList.razor`: renderowanie listy, paginacja, brak danych (empty state)
- `HabitItem.razor`: wyświetlanie szczegółów nawyku
- `HabitForm.razor`:
  - Walidacja formularza (`EditForm` + `DataAnnotationsValidator`)
  - Komunikaty błędów walidacji
  - Submit sukces i błąd (mocking API)

##### Today
- `TodayChecklist.razor`:
  - Renderowanie dzisiejszych zadań
  - Filtrowanie według dnia tygodnia
  - Szybki check-in (optymistyczny update)
  - Blokada podwójnego wysłania

##### Calendar
- `CalendarView.razor`:
  - Renderowanie kalendarza
  - Kolorowanie dni (zielony/czerwony/neutralny)
  - Readonly (brak możliwości edycji)

##### Charts
- `RollingSuccessChart.razor`:
  - Renderowanie wykresu (może wymagać mock JS interop)
  - Przełącznik 7/30 dni
  - Tooltip z danymi

##### Shared
- `Notifications.razor`: renderowanie listy powiadomień
- `ErrorBoundary.razor`: obsługa błędów, przyjazne komunikaty

**Pokrycie docelowe**: ≥70% dla kluczowych komponentów UI

### 3.4 Testy end-to-end (E2E)
**Framework**: Playwright
**Lokalizacja**: `HabitFlow.Tests/E2ETests/` (do utworzenia)

#### Ścieżka krytyczna (zgodnie z PRD US-026):
1. **Rejestracja użytkownika**
   - Przejście na `/register`
   - Wypełnienie formularza (e-mail, hasło)
   - Weryfikacja e-maila (symulacja kliknięcia linku)
   - Przekierowanie na `/login`

2. **Logowanie**
   - Wypełnienie formularza logowania
   - Przekierowanie na ekran "Dziś"

3. **Utworzenie nawyku**
   - Przejście na `/habits/new`
   - Utworzenie nawyku typu "start" (np. "Czytanie", 10 stron/dzień, pn-pt)
   - Weryfikacja pojawienia się na liście

4. **Check-in dzienny**
   - Przejście na ekran "Dziś"
   - Wykonanie check-in (np. 7/10 stron)
   - Weryfikacja sukcesu

5. **Widok kalendarza**
   - Przejście na `/habits/{id}/calendar`
   - Weryfikacja kolorowania dnia (częściowo wykonany = żółty/pomarańczowy lub zielony w zależności od progu)
   - Weryfikacja planu na przyszłe dni

6. **Widok wykresu**
   - Przejście na `/habits/{id}/progress`
   - Weryfikacja renderowania wykresu rolling 7 dni
   - Przełączenie na 30 dni

7. **Generowanie powiadomienia "miss due"**
   - Symulacja następnego dnia bez check-in
   - Trigger background job (ręcznie lub przez przesunięcie czasu systemowego)
   - Przejście na `/notifications`
   - Weryfikacja pojawienia się powiadomienia motywacyjnego

8. **Wylogowanie**
   - Kliknięcie "Wyloguj"
   - Przekierowanie na stronę logowania

**Dodatkowe scenariusze E2E**:
- Nawyk typu "stop" (naruszenia)
- Uzupełnianie historii (7 dni wstecz)
- Edycja i usuwanie nawyku
- Zmiana strefy czasowej
- Usunięcie konta

**Pokrycie docelowe**: 100% ścieżki krytycznej + kluczowe scenariusze alternatywne

### 3.5 Testy wydajnościowe
**Narzędzie**: BenchmarkDotNet lub k6
**Lokalizacja**: `HabitFlow.Tests/PerformanceTests/` (opcjonalnie)

#### Kluczowe metryki:
- GET `/api/v1/today`: ≤ 500ms przy 20 nawykach (zgodnie z PRD)
- GET `/api/v1/habits`: ≤ 200ms przy paginacji 20 wyników
- POST `/api/v1/checkins`: ≤ 100ms
- GET `/api/v1/habits/{id}/calendar`: ≤ 300ms dla 30 dni

**Priorytet**: COULD (nice-to-have w MVP, MUST po MVP)

### 3.6 Testy bezpieczeństwa (manualne)
#### Zakres weryfikacji manualnej:
- SQL injection: parametryzacja zapytań EF Core (weryfikacja przez code review)
- XSS: sanityzacja inputów w Blazor (domyślna ochrona)
- CSRF: tokeny anty-CSRF w formularzach (domyślnie w Blazor Server)
- Autoryzacja: brak dostępu do cudzych zasobów (covered przez testy integracyjne)
- Weryfikacja HTTPS w production
- Brak sekretów w repozytorium (code review)

## 4. Scenariusze testowe dla kluczowych funkcjonalności

### 4.1 Autoryzacja i uwierzytelnianie

| ID | Scenariusz | Kroki | Oczekiwany rezultat | Typ testu | Priorytet |
|----|-----------|-------|---------------------|-----------|-----------|
| AUTH-001 | Rejestracja z poprawnym e-mailem i hasłem | 1. POST `/api/v1/auth/register` z e-mail i hasłem ≥8 znaków | 201 Created, e-mail weryfikacyjny wysłany | Integration | MUST |
| AUTH-002 | Rejestracja z duplikatem e-maila | 1. Zarejestruj użytkownika<br>2. Ponów z tym samym e-mailem | 409 Conflict, komunikat o duplikacie | Integration | MUST |
| AUTH-003 | Rejestracja z za krótkim hasłem | 1. POST z hasłem <8 znaków | 400 Bad Request, komunikat walidacji | Unit (validator) | MUST |
| AUTH-004 | Potwierdzenie e-maila | 1. POST `/api/v1/auth/confirm-email` z poprawnym tokenem | 200 OK, konto aktywne | Integration | MUST |
| AUTH-005 | Logowanie z poprawnymi danymi | 1. POST `/api/v1/auth/login` | 200 OK, cookie sesyjne | Integration | MUST |
| AUTH-006 | Logowanie przed weryfikacją e-maila | 1. Zarejestruj bez potwierdzenia<br>2. Próba logowania | 403 Forbidden | Integration | MUST |
| AUTH-007 | Reset hasła | 1. POST `/api/v1/auth/forgot-password`<br>2. POST `/api/v1/auth/reset-password` z tokenem | 200 OK, możliwość logowania z nowym hasłem | Integration | MUST |
| AUTH-008 | Usunięcie konta | 1. DELETE `/api/v1/auth/account` jako zalogowany użytkownik | 204 No Content, dane usunięte kaskadowo | Integration | MUST |

### 4.2 Zarządzanie nawykami (CRUD)

| ID | Scenariusz | Kroki | Oczekiwany rezultat | Typ testu | Priorytet |
|----|-----------|-------|---------------------|-----------|-----------|
| HABIT-001 | Utworzenie nawyku "start" | 1. POST `/api/v1/habits` z typem=Start, dni=[Mon,Tue,Wed], powtórzenia=10 | 201 Created, nawyk w bazie | Integration | MUST |
| HABIT-002 | Utworzenie nawyku "stop" | 1. POST z typem=Stop | 201 Created, logika naruszeń aktywna | Integration | MUST |
| HABIT-003 | Walidacja limitu 20 nawyków | 1. Utwórz 20 nawyków<br>2. Próba utworzenia 21. | 409 Conflict, komunikat o limicie | Integration | MUST |
| HABIT-004 | Walidacja tytułu >80 znaków | 1. POST z tytułem 81 znaków | 400 Bad Request | Unit (validator) | MUST |
| HABIT-005 | Odczyt listy nawyków z paginacją | 1. GET `/api/v1/habits?page=1&pageSize=10` | 200 OK, max 10 wyników | Integration | MUST |
| HABIT-006 | Odczyt pojedynczego nawyku | 1. GET `/api/v1/habits/{id}` | 200 OK, szczegóły nawyku | Integration | MUST |
| HABIT-007 | Dostęp do cudzego nawyku | 1. Użytkownik A próbuje GET nawyku użytkownika B | 403 Forbidden lub 404 Not Found | Integration | MUST |
| HABIT-008 | Aktualizacja nawyku | 1. PUT `/api/v1/habits/{id}` z nowymi danymi | 200 OK, dane zaktualizowane | Integration | MUST |
| HABIT-009 | Usunięcie nawyku | 1. DELETE `/api/v1/habits/{id}` | 204 No Content, check-iny usunięte kaskadowo | Integration | MUST |

### 4.3 Check-in dzienny

| ID | Scenariusz | Kroki | Oczekiwany rezultat | Typ testu | Priorytet |
|----|-----------|-------|---------------------|-----------|-----------|
| CHECKIN-001 | Check-in z wartością w zakresie | 1. POST `/api/v1/checkins` z actualValue=7, targetValue=10 | 201 Created, daily_score=0.7 dla "start" | Integration | MUST |
| CHECKIN-002 | Check-in typu "stop" | 1. POST z actualValue=1 (naruszenie), targetValue=3 | 201 Created, daily_score=0.67 (1-1/3) | Unit + Integration | MUST |
| CHECKIN-003 | Duplikat check-in tego samego dnia | 1. POST check-in dla dnia X<br>2. Ponów dla dnia X | 409 Conflict, komunikat o duplikacie | Integration | MUST |
| CHECKIN-004 | Check-in z wartością >targetValue | 1. POST z actualValue=15, targetValue=10 | 201 Created, wartość obcięta do 10 lub daily_score=1.0 | Unit | MUST |
| CHECKIN-005 | Uzupełnienie wstecz (7 dni) | 1. POST z dateOffset=-5 dni | 201 Created | Integration | MUST |
| CHECKIN-006 | Uzupełnienie wstecz >7 dni | 1. POST z dateOffset=-8 dni | 400 Bad Request | Integration | MUST |
| CHECKIN-007 | Binary completion mode | 1. Nawyk z CompletionMode=Binary<br>2. POST z actualValue=1 | daily_score=1.0 | Unit | MUST |
| CHECKIN-008 | Binary z actualValue=0 | 1. POST z actualValue=0 | daily_score=0.0 | Unit | MUST |

### 4.4 Obliczenia biznesowe (success_rate)

| ID | Scenariusz | Kroki | Oczekiwany rezultat | Typ testu | Priorytet |
|----|-----------|-------|---------------------|-----------|-----------|
| CALC-001 | success_rate z pełnym wykonaniem | 1. Nawyk zaplanowany 7 dni<br>2. Check-in 7/7 z targetValue osiągniętym | success_rate=1.0 (100%) | Unit | MUST |
| CALC-002 | success_rate z częściowym wykonaniem | 1. Check-in 5/7 dni | success_rate=5/7≈0.714 (71.4%) | Unit | MUST |
| CALC-003 | success_rate z Quantitative (start) | 1. 3 dni: 10/10, 7/10, 5/10<br>2. daily_scores=[1.0, 0.7, 0.5] | success_rate=(1.0+0.7+0.5)/3≈0.733 (73.3%) | Unit | MUST |
| CALC-004 | success_rate z Quantitative (stop) | 1. 3 dni naruszeń: 0/3, 1/3, 2/3<br>2. daily_scores=[1.0, 0.67, 0.33] | success_rate≈0.667 (66.7%) | Unit | MUST |
| CALC-005 | success_rate=0 gdy brak zaplanowanych dni | 1. Nawyk bez dni w oknie | success_rate=0.0 | Unit | MUST |
| CALC-006 | Próg sukcesu 75% do deadline | 1. Nawyk z deadline za 30 dni<br>2. success_rate=0.76 | Status: sukces (≥75%) | Unit | MUST |
| CALC-007 | Rolling window 7 dni | 1. Check-iny przez 10 dni<br>2. GET rolling?window=7 | Zwraca success_rate z ostatnich 7 dni | Integration | MUST |
| CALC-008 | Rolling window 30 dni | 1. GET rolling?window=30 | Zwraca success_rate z ostatnich 30 dni | Integration | MUST |

### 4.5 Ekran "Dziś" i wydajność

| ID | Scenariusz | Kroki | Oczekiwany rezultat | Typ testu | Priorytet |
|----|-----------|-------|---------------------|-----------|-----------|
| TODAY-001 | Lista dzisiejszych zadań | 1. Utwórz nawyki na różne dni tygodnia<br>2. GET `/api/v1/today` w poniedziałek | Tylko nawyki z poniedziałkiem w harmonogramie | Integration | MUST |
| TODAY-002 | Strefa czasowa użytkownika | 1. Ustaw UTC+2<br>2. GET `/api/v1/today` | "Dziś" wg UTC+2 | Integration | MUST |
| TODAY-003 | Wydajność przy 20 nawykach | 1. Utwórz 20 nawyków<br>2. GET `/api/v1/today` | ≤500ms | Performance | MUST |
| TODAY-004 | Brak wykonanych check-inów | 1. Check-in dla nawyku X dzisiaj<br>2. GET `/api/v1/today` | Nawyk X nie pojawia się na liście | Integration | MUST |

### 4.6 Kalendarz i wizualizacje

| ID | Scenariusz | Kroki | Oczekiwany rezultat | Typ testu | Priorytet |
|----|-----------|-------|---------------------|-----------|-----------|
| CAL-001 | Statusy dni w kalendarzu | 1. GET `/api/v1/habits/{id}/calendar` | Przyszłe=plan, przeszłe=done/miss | Integration | MUST |
| CAL-002 | Kolor zielony dla wykonanego dnia | 1. Check-in z targetValue osiągniętym | Dzień oznaczony jako "done" (zielony) | Integration | MUST |
| CAL-003 | Kolor czerwony dla niewykonanego | 1. Brak check-in w zaplanowanym dniu | Dzień oznaczony jako "miss" (czerwony) | Integration | MUST |
| CAL-004 | Readonly - brak edycji | 1. Próba PUT/PATCH na kalendarz | Brak endpointu (tylko przez CRUD i check-in) | Manual | MUST |

### 4.7 Powiadomienia AI

| ID | Scenariusz | Kroki | Oczekiwany rezultat | Typ testu | Priorytet |
|----|-----------|-------|---------------------|-----------|-----------|
| NOTIF-001 | Generowanie przy "miss due" | 1. Zaplanowany dzień bez check-in<br>2. Trigger background job | Powiadomienie w `/api/v1/notifications` | Integration + E2E | MUST |
| NOTIF-002 | Brak powiadomienia gdy dzień wykonany | 1. Check-in zgodnie z planem | Brak nowego powiadomienia | Integration | MUST |
| NOTIF-003 | Fallback na stałe szablony | 1. AI niedostępne (mock timeout)<br>2. Trigger miss due | Powiadomienie ze stałym tekstem | Unit | MUST |
| NOTIF-004 | Jedno powiadomienie per nawyk/dzień | 1. Miss due dla nawyku X dzień Y<br>2. Ponowny trigger | Brak duplikatu powiadomienia | Integration | MUST |

## 5. Środowisko testowe

### 5.1 Środowiska

| Środowisko | Cel | Konfiguracja |
|-----------|-----|--------------|
| **Lokalne (dev)** | Development, testy manualne | SQL Server w Docker, `appsettings.Development.json` |
| **CI/CD (GitHub Actions)** | Automatyczne testy przy PR/merge | TestContainers (SQL Server), in-memory gdzie możliwe |
| **Staging** (opcjonalnie) | Testy akceptacyjne, UAT | Azure SQL lub SQL Server w chmurze |

### 5.2 Infrastruktura testowa

#### Testy jednostkowe
- **Baza danych**: In-memory SQLite lub mockowane `DbContext` przez NSubstitute
- **DI**: Ręczna konfiguracja kontenerów testowych lub `ServiceCollection`
- **Izolacja**: Każdy test niezależny, brak współdzielonego stanu

#### Testy integracyjne
- **Baza danych**: TestContainers z SQL Server 2022
- **WebApplicationFactory**: Konfiguracja testowego API z nadpisanym `DbContext`
- **Strategia bazy**:
  - Jedna instancja kontenera dla wszystkich testów (shared fixture)
  - Każdy test używa unikalnych użytkowników/danych (izolacja przez dane, nie transakcje)
  - Testy uruchamiane równolegle (XUnit collections)
- **Klient HTTP**: Generowany przez NSwag z `openapi.json`

#### Testy E2E
- **Środowisko**: Aplikacja uruchomiona lokalnie lub w kontenerze Docker
- **Przeglądarka**: Chromium (headless) przez Playwright
- **Baza danych**: Dedykowana instancja testowa, czyszczona między testami

### 5.3 Dane testowe
- **Seeding**: Minimalne dane testowe w `IntegrationTestFixture`
- **Fabryki**: Builder pattern dla encji (np. `HabitBuilder`, `UserBuilder`)
- **Resetowanie**: Transakcje wycofywane po testach (unit) lub czyszczenie bazy (E2E)

## 6. Narzędzia do testowania

| Kategoria | Narzędzie | Wersja | Cel |
|-----------|-----------|--------|-----|
| **Framework testowy** | XUnit | Latest | Testy jednostkowe i integracyjne |
| **Mocking** | NSubstitute | Latest | Mockowanie zależności w testach jednostkowych |
| **Konteneryzacja** | TestContainers | Latest | SQL Server w testach integracyjnych |
| **Asercje** | FluentAssertions | Latest (opcjonalnie) | Czytelniejsze asercje |
| **Testy UI** | bUnit | Latest | Testy komponentów Blazor |
| **E2E** | Playwright | Latest | Testy end-to-end UI |
| **Klient HTTP** | NSwag | - | Generowanie typowanego klienta z OpenAPI |
| **Pokrycie kodu** | Coverlet | Latest | Raportowanie pokrycia testów |
| **Wydajność** | BenchmarkDotNet | Latest (opcjonalnie) | Testy wydajnościowe |
| **CI/CD** | GitHub Actions | - | Automatyzacja testów |

### 6.1 Konfiguracja NSwag
- Plik `nswag.json` w `HabitFlow.Api/` i `HabitFlow.Blazor/`
- Generowanie klienta C# z `openapi.json`
- Wykorzystanie w testach integracyjnych dla typowanych wywołań API

### 6.2 Konfiguracja TestContainers
```csharp
// IntegrationTestFixture.cs
public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public async Task InitializeAsync()
    {
        await _msSqlContainer.StartAsync();
        // Migracje EF Core
    }

    public async Task DisposeAsync()
    {
        await _msSqlContainer.DisposeAsync();
    }
}
```

## 7. Harmonogram testów

### 7.1 Fazy testowania w sprintach

| Sprint | Tydzień | Aktywności testowe | Odpowiedzialny |
|--------|---------|-------------------|----------------|
| **S1** | T1 | - Setup projektu testowego `HabitFlow.Tests`<br>- Konfiguracja XUnit, NSubstitute<br>- Pierwsze testy walidatorów (Auth) | Dev |
| **S1** | T2 | - Testy jednostkowe handlerów Auth<br>- Testy integracyjne AuthEndpoints<br>- Setup TestContainers | Dev |
| **S2** | T3 | - Testy jednostkowe Habits i Checkins<br>- Testy integracyjne HabitEndpoints, CheckinEndpoints<br>- Testy obliczeń success_rate | Dev |
| **S2** | T4 | - Testy Today, Calendar, Progress<br>- Testy E2E ścieżki krytycznej (Playwright)<br>- Testy wydajnościowe (endpoint /today)<br>- Raportowanie pokrycia | Dev |

### 7.2 Continuous Integration
- **Trigger**: Każdy push do PR, merge do `master`
- **Pipeline** (GitHub Actions):
  1. Restore dependencies
  2. Build solution
  3. Run unit tests
  4. Run integration tests (z TestContainers)
  5. Generate coverage report (Coverlet)
  6. Upload artifacts (test results, coverage)
  7. (Opcjonalnie) Run E2E tests na staging

### 7.3 Regression testing
- Pełen zestaw testów uruchamiany przy każdym merge do `master`
- Przed release: manualne testy akceptacyjne kluczowych ścieżek

## 8. Kryteria akceptacji testów

### 8.1 Kryteria przejścia testów (test pass criteria)

| Typ testu | Kryterium | Wartość docelowa |
|-----------|-----------|------------------|
| **Testy jednostkowe** | Pokrycie kodu (Core) | ≥80% |
| **Testy jednostkowe** | Wszystkie przechodzą | 100% pass rate |
| **Testy integracyjne** | Wszystkie endpointy pokryte | 100% pokrycia endpointów |
| **Testy integracyjne** | Wszystkie przechodzą | 100% pass rate |
| **Testy E2E** | Ścieżka krytyczna działa | 100% pass rate |
| **Testy wydajnościowe** | Endpoint `/today` | ≤500ms przy 20 nawykach |
| **Build CI/CD** | Pipeline przechodzi | 100% success rate |

### 8.2 Definition of Done (DoD) dla funkcjonalności
Funkcjonalność uznawana za ukończoną, gdy:
1. ✅ Kod zaimplementowany zgodnie z PRD i zasadami projektu
2. ✅ Testy jednostkowe napisane i przechodzą (coverage ≥80% dla logiki biznesowej)
3. ✅ Testy integracyjne napisane i przechodzą (happy path + error cases)
4. ✅ Kod przeszedł code review
5. ✅ Migracje EF Core zaktualizowane (jeśli dotyczy)
6. ✅ OpenAPI spec zaktualizowany (jeśli dotyczy)
7. ✅ Pipeline CI/CD przechodzi
8. ✅ Brak krytycznych/wysokich błędów w IDE (Rider inspections)

### 8.3 Definition of Done dla MVP
MVP gotowy do release, gdy:
1. ✅ Wszystkie wymagania MUST z PRD zaimplementowane i przetestowane
2. ✅ Ścieżka krytyczna E2E działa (US-026)
3. ✅ Pokrycie testów: Unit ≥80%, Integration 100% endpointów, E2E ścieżka krytyczna
4. ✅ Testy wydajnościowe przechodzą (endpoint `/today` ≤500ms)
5. ✅ Brak znanych błędów krytycznych (severity: critical/high)
6. ✅ Dokumentacja API aktualna (OpenAPI)
7. ✅ Migracje bazy danych działają poprawnie
8. ✅ Weryfikacja bezpieczeństwa (manualna, checklist)
9. ✅ Deployment na środowisko staging/produkcyjne działa

### 8.4 Priorytety defektów

| Severity | Opis | Przykład | SLA naprawy |
|----------|------|----------|-------------|
| **Critical** | Blokuje kluczową funkcjonalność, brak workaround | Nie można zalogować się, crash aplikacji | 24h |
| **High** | Poważny błąd, istnieje workaround | Błędne obliczenia success_rate, duplikaty check-inów | 3 dni |
| **Medium** | Drobny błąd funkcjonalny | Walidacja UI nie synchronizuje się z API | 1 tydzień |
| **Low** | Kosmetyka, UX | Literówka w UI, brak spacji | Backlog |

## 9. Role i odpowiedzialności w procesie testowania

### 9.1 Role

| Rola | Osoba | Odpowiedzialności |
|------|-------|-------------------|
| **Developer & QA** | 1 osoba (solo dev) | - Implementacja kodu<br>- Pisanie testów jednostkowych i integracyjnych<br>- Utrzymanie pipeline CI/CD<br>- Wykonywanie testów manualnych<br>- Naprawa defektów |
| **Code Reviewer** | (opcjonalnie druga osoba lub self-review) | - Review kodu i testów<br>- Weryfikacja pokrycia testów<br>- Zatwierdzanie PR |
| **Product Owner** | (opcjonalnie) | - Definiowanie kryteriów akceptacji<br>- UAT (User Acceptance Testing)<br>- Priorytetyzacja defektów |

### 9.2 Workflow testowania

1. **Development**:
   - Dev implementuje funkcjonalność w feature branch
   - Dev pisze testy jednostkowe równolegle z implementacją (TDD/TLD)
   - Dev uruchamia lokalnie: `dotnet test`

2. **Pull Request**:
   - Dev tworzy PR do `master`
   - GitHub Actions uruchamia pipeline (build + testy)
   - (Opcjonalnie) Code review przez drugą osobę lub self-review
   - Merge tylko gdy pipeline przechodzi ✅

3. **Integration & E2E**:
   - Dev pisze testy integracyjne dla nowych endpointów
   - Dev aktualizuje testy E2E jeśli dotknięta ścieżka krytyczna
   - Uruchomienie pełnego zestawu testów przed merge do `master`

4. **Defect Management**:
   - Zgłaszanie bugów jako GitHub Issues
   - Priorytyzacja przez severity
   - Naprawa w osobnych branches, z testami reprodukującymi bug
   - Weryfikacja naprawy przez ponowne uruchomienie testów

## 10. Procedury raportowania błędów

### 10.1 Szablon zgłoszenia błędu (GitHub Issue)

```markdown
## Opis błędu
[Krótki opis problemu w 1-2 zdaniach]

## Kroki reprodukcji
1. [Krok 1]
2. [Krok 2]
3. [Krok 3]

## Oczekiwane zachowanie
[Co powinno się stać]

## Rzeczywiste zachowanie
[Co się dzieje]

## Środowisko
- **Projekt**: HabitFlow.Api / HabitFlow.Blazor / HabitFlow.Core
- **Commit/Branch**: [hash lub nazwa brancha]
- **Środowisko**: Lokalne / CI/CD / Staging
- **Przeglądarka** (jeśli dotyczy): Chrome 131 / Edge / Firefox

## Logi / Stack trace
[Jeśli dostępne, wklej logi lub stack trace]

## Załączniki
[Screenshoty, wideo, pliki]

## Severity
- [ ] Critical (blokuje kluczową funkcjonalność)
- [ ] High (poważny błąd, workaround istnieje)
- [ ] Medium (drobny błąd)
- [ ] Low (kosmetyka)

## Powiązane
- Related to: #[issue_number]
- Blocks: #[issue_number]
```

### 10.2 Kanały raportowania

| Kanał | Cel | Odpowiedź |
|-------|-----|-----------|
| **GitHub Issues** | Główny kanał dla bugów i feature requests | 24-48h (acknowledgment) |
| **Failed CI/CD build** | Automatyczne powiadomienie na GitHub | Natychmiastowa reakcja |
| **Slack/Email** (opcjonalnie) | Komunikacja w zespole | Według ustaleń |

### 10.3 Workflow bugów

1. **Zgłoszenie**: Issue utworzony według szablonu, label `bug`, severity
2. **Triaging**: Dev ocenia severity i priority
3. **Assignment**: Dev przypisuje do siebie (solo) lub kolejki
4. **Development**:
   - Branch `fix/issue-123-description`
   - Test reprodukujący bug (red)
   - Naprawa (green)
   - Refactoring (jeśli potrzeba)
5. **PR & Review**: PR z linkiem do issue `Fixes #123`
6. **Verification**: Uruchomienie testów, merge
7. **Closure**: Issue zamknięty automatycznie po merge

### 10.4 Metryki jakości

| Metryka | Cel | Źródło danych |
|---------|-----|---------------|
| **Test pass rate** | ≥98% | CI/CD pipeline |
| **Code coverage** | ≥80% (Core) | Coverlet report |
| **Bugs per sprint** | <10 new bugs | GitHub Issues |
| **Critical bugs** | 0 otwartych critical bugs przed release | GitHub Issues (filtered) |
| **Mean time to fix (MTTF)** | <48h dla critical, <1 tydzień dla high | GitHub Issues (time to close) |
| **Flaky tests** | <5% testów | CI/CD logs |

---

## Podsumowanie

Niniejszy plan testów zapewnia kompleksowe pokrycie aplikacji HabitFlow we wszystkich kluczowych obszarach:

✅ **Testy jednostkowe** (XUnit + NSubstitute) dla walidatorów, handlerów i logiki biznesowej
✅ **Testy integracyjne** (TestContainers + NSwag) dla wszystkich endpointów API
✅ **Testy komponentów UI** (bUnit) dla kluczowych komponentów Blazor
✅ **Testy E2E** (Playwright) dla ścieżki krytycznej z PRD
✅ **Testy wydajnościowe** dla kluczowych endpointów (endpoint `/today` ≤500ms)
✅ **CI/CD** (GitHub Actions) z automatycznym uruchamianiem testów

Plan jest dostosowany do architektury Clean Architecture, wzorca CQS, wykorzystania EF Core, Blazor Server, MudBlazor oraz specyfiki projektu HabitFlow jako MVP dla solo developera. Wszystkie scenariusze testowe pokrywają wymagania funkcjonalne z PRD, ze szczególnym naciskiem na kluczowe obszary: autoryzację, logikę obliczeń `success_rate`/`daily_score`, check-iny, kalendarz oraz powiadomienia AI.
