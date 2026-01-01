# Schemat bazy danych HabitFlow – SQL Server 2022

## 1. Tabele

### 1.1. AspNetUsers (rozszerzona)
Rozszerzenie standardowej tabeli ASP.NET Core Identity o domenowe pola użytkownika.

| Kolumna | Typ | Ograniczenia | Opis |
|---------|-----|--------------|------|
| Id | nvarchar(450) | PK | Unikalny identyfikator użytkownika |
| UserName | nvarchar(256) | NOT NULL, UNIQUE | Nazwa użytkownika |
| Email | nvarchar(256) | NOT NULL | Adres e-mail |
| EmailConfirmed | bit | NOT NULL, DEFAULT 0 | Potwierdzenie e-mail |
| PasswordHash | nvarchar(MAX) | NULL | Hash hasła |
| SecurityStamp | nvarchar(MAX) | NULL | Znacznik bezpieczeństwa |
| ConcurrencyStamp | nvarchar(MAX) | NULL | Znacznik współbieżności |
| **TimeZoneId** | nvarchar(64) | NOT NULL | Strefa czasowa użytkownika (IANA) |
| **CreatedAtUtc** | datetime2 | NOT NULL, DEFAULT GETUTCDATE() | Moment utworzenia konta (UTC) |

**Rozszerzenia domenowe**: TimeZoneId, CreatedAtUtc

---

### 1.2. Habits
Główna tabela przechowująca nawyki użytkowników.

| Kolumna | Typ            | Ograniczenia | Opis |
|---------|----------------|--------------|------|
| Id | int            | PK, IDENTITY(1,1) | Unikalny identyfikator nawyku |
| UserId | nvarchar(450)  | NOT NULL, FK → AspNetUsers(Id) ON DELETE CASCADE | Właściciel nawyku |
| Title | nvarchar(80)   | NOT NULL | Tytuł nawyku |
| Description | nvarchar(1000) | NULL | Opis nawyku |
| Type | tinyint        | NOT NULL | Typ: 1 = Start (zacząć), 2 = Stop (przestać) |
| CompletionMode | tinyint | NOT NULL | Sposób rozliczania: 1 = Binary (0/1), 2 = Quantitative (ilościowy) |
| DaysOfWeekMask | tinyint        | NOT NULL, CHECK (DaysOfWeekMask BETWEEN 1 AND 127) | Maska dni tygodnia (bit 0=Pon, ..., bit 6=Niedz) |
| TargetValue | smallint | NOT NULL, DEFAULT 1, CHECK (TargetValue BETWEEN 1 AND 1000) | Wartość docelowa na dzień (np. liczba stron/posiłków/zadań) |
| TargetUnit | nvarchar(32) | NULL | Jednostka celu (np. 'pages', 'meals', 'tasks') – pole opisowe dla UI |
| DeadlineDate | date           | NULL | Opcjonalny termin zakończenia nawyku |
| CreatedAtUtc | datetime2      | NOT NULL, DEFAULT GETUTCDATE() | Moment utworzenia (UTC) |

**Indeksy**:
- `IX_Habits_UserId_CreatedAtUtc` (UserId, CreatedAtUtc DESC) INCLUDE (Title, Type, DaysOfWeekMask, TargetValue, CompletionMode, DeadlineDate)

---

### 1.3. Checkins
Tabela przechowująca dzienne check-iny dla nawyków.

| Kolumna | Typ | Ograniczenia | Opis |
|---------|-----|--------------|------|
| Id | bigint | PK, IDENTITY(1,1) | Unikalny identyfikator check-inu |
| HabitId | int | NOT NULL, FK → Habits(Id) ON DELETE CASCADE | Powiązany nawyk |
| UserId | nvarchar(450) | NOT NULL, FK → AspNetUsers(Id) ON DELETE NO ACTION | Denormalizacja dla RLS/zapytań |
| LocalDate | date | NOT NULL | Data lokalna check-inu (wg strefy czasu użytkownika) |
| ActualValue | int | NOT NULL | Rzeczywista wartość z dnia (np. przeczytane strony, liczba posiłków, liczba naruszeń) |
| TargetValueSnapshot | smallint | NOT NULL | Snapshot Habits.TargetValue z momentu check-inu |
| CompletionModeSnapshot | tinyint | NOT NULL | Snapshot CompletionMode (1=Binary, 2=Quantitative) |
| HabitTypeSnapshot | tinyint | NOT NULL | Snapshot Type z momentu check-inu (1=Start, 2=Stop) |
| IsPlanned | bit | NOT NULL | Czy dzień był zaplanowany (wg DaysOfWeekMask) |
| CreatedAtUtc | datetime2 | NOT NULL, DEFAULT GETUTCDATE() | Moment utworzenia check-inu (UTC) |

**Ograniczenia**:
- `UQ_Checkins_HabitId_LocalDate` UNIQUE (HabitId, LocalDate)
  (Zakres ActualValue w relacji do TargetValueSnapshot jest walidowany aplikacyjnie: typowo 0 <= ActualValue <= TargetValueSnapshot, z ewentualnym „clampowaniem” do TargetValueSnapshot po stronie domeny.)

**Indeksy**:
- `IX_Checkins_UserId_LocalDate_HabitId` CLUSTERED (UserId, LocalDate, HabitId)
- `IX_Checkins_HabitId_LocalDate` NONCLUSTERED (HabitId, LocalDate) INCLUDE (ActualValue, TargetValueSnapshot, CompletionModeSnapshot, HabitTypeSnapshot, IsPlanned)

---

### 1.4. Notifications
Tabela przechowująca powiadomienia generowane przez system.

| Kolumna | Typ | Ograniczenia | Opis |
|---------|-----|--------------|------|
| Id | bigint | PK, IDENTITY(1,1) | Unikalny identyfikator powiadomienia |
| UserId | nvarchar(450) | NOT NULL, FK → AspNetUsers(Id) ON DELETE CASCADE | Odbiorca powiadomienia |
| HabitId | int | NOT NULL, FK → Habits(Id) ON DELETE CASCADE | Powiązany nawyk |
| LocalDate | date | NOT NULL | Data lokalna dotycząca powiadomienia |
| Type | tinyint | NOT NULL | Typ: 1 = MissDue |
| Content | nvarchar(1024) | NOT NULL | Treść powiadomienia |
| AiStatus | tinyint | NULL | Status generowania AI: 1=Success, 2=Fallback, 3=Error |
| AiError | nvarchar(512) | NULL | Opis błędu AI (do celów diagnostycznych) |
| CreatedAtUtc | datetime2 | NOT NULL, DEFAULT GETUTCDATE() | Moment utworzenia (UTC) |

**Ograniczenia**:
- `UQ_Notifications_HabitId_LocalDate_Type` UNIQUE (HabitId, LocalDate, Type)

**Indeksy**:
- `IX_Notifications_UserId_CreatedAtUtc` (UserId, CreatedAtUtc DESC) INCLUDE (Content, Type, HabitId, LocalDate)

---

### 1.5. Standardowe tabele ASP.NET Core Identity
Pozostałe tabele Identity niezbędne dla pełnego schematu uwierzytelniania:

- **AspNetRoles** (role użytkowników – opcjonalne w MVP, domyślnie wszyscy to "user")
- **AspNetUserRoles** (przypisania ról użytkownikom)
- **AspNetUserClaims** (dodatkowe claims użytkowników)
- **AspNetUserLogins** (zewnętrzne logowania)
- **AspNetUserTokens** (tokeny weryfikacyjne/resetujące)
- **AspNetRoleClaims** (claims dla ról)

**Uwaga**: Struktura zgodna ze standardem ASP.NET Core Identity, tworzona automatycznie przez EF Core.

---

## 2. Relacje między tabelami

### 2.1. AspNetUsers ↔ Habits
- **Relacja**: 1:N (jeden użytkownik może mieć wiele nawyków)
- **FK**: Habits.UserId → AspNetUsers.Id
- **Kaskada**: ON DELETE CASCADE (usunięcie użytkownika usuwa wszystkie jego nawyki)

### 2.2. Habits ↔ Checkins
- **Relacja**: 1:N (jeden nawyk może mieć wiele check-inów)
- **FK**: Checkins.HabitId → Habits.Id
- **Kaskada**: ON DELETE CASCADE (usunięcie nawyku usuwa wszystkie jego check-iny)
- **Denormalizacja**: Checkins.UserId dla RLS i wydajności zapytań

### 2.3. AspNetUsers ↔ Checkins
- **Relacja**: 1:N (pomocnicza, dla RLS i zapytań)
- **FK**: Checkins.UserId → AspNetUsers.Id
- **Kaskada**: ON DELETE NO ACTION (usuwanie przez kaskadę z Habits)

### 2.4. Habits ↔ Notifications
- **Relacja**: 1:N (jeden nawyk może mieć wiele powiadomień)
- **FK**: Notifications.HabitId → Habits.Id
- **Kaskada**: ON DELETE CASCADE (usunięcie nawyku usuwa wszystkie jego powiadomienia)

### 2.5. AspNetUsers ↔ Notifications
- **Relacja**: 1:N (jeden użytkownik może mieć wiele powiadomień)
- **FK**: Notifications.UserId → AspNetUsers.Id
- **Kaskada**: ON DELETE CASCADE (usunięcie użytkownika usuwa wszystkie jego powiadomienia)

---

## 3. Indeksy

### 3.1. Indeksy domenowe
Zoptymalizowane pod kątem najczęstszych zapytań aplikacji.

#### Habits
- **IX_Habits_UserId_CreatedAtUtc**: (UserId, CreatedAtUtc DESC) INCLUDE (Title, Type, DaysOfWeekMask, TargetValue, CompletionMode, DeadlineDate)
  - Obsługa listy nawyków użytkownika sortowanej chronologicznie
  - INCLUDE pokrywa wszystkie kolumny potrzebne do wyświetlenia listy + danych o celu

#### Checkins
- **IX_Checkins_UserId_LocalDate_HabitId**: CLUSTERED (UserId, LocalDate, HabitId)
  - Główny indeks klastrowy dla widoku "Dziś" i zapytań po dacie lokalnej
  - Optymalizacja zapytań rolling 7/30 dni

- **IX_Checkins_HabitId_LocalDate**: NONCLUSTERED (HabitId, LocalDate) INCLUDE (ActualValue, TargetValueSnapshot, HabitTypeSnapshot, CompletionModeSnapshot, IsPlanned)
  - Obsługa kalendarza pojedynczego nawyku
  - INCLUDE pokrywa dane do obliczenia success_rate oraz częściowego wykonania

#### Notifications
- **IX_Notifications_UserId_CreatedAtUtc**: (UserId, CreatedAtUtc DESC) INCLUDE (Content, Type, HabitId, LocalDate)
  - Obsługa listy powiadomień użytkownika w kolejności chronologicznej
  - INCLUDE pokrywa wszystkie dane do wyświetlenia listy

### 3.2. Indeksy Identity
Standardowe indeksy tworzone automatycznie przez ASP.NET Core Identity na:
- AspNetUsers.NormalizedUserName
- AspNetUsers.NormalizedEmail
- AspNetUserRoles, AspNetUserClaims, AspNetUserLogins (FK i composite)

---

## 4. Row-Level Security (RLS)

### 4.1. Polityki bezpieczeństwa
Wszystkie domenowe tabele (Habits, Checkins, Notifications) implementują RLS oparte na `SESSION_CONTEXT('user_id')`.

#### 4.1.1. Security Predicate Function
```sql
CREATE FUNCTION dbo.fn_SecurityPredicate(@UserId nvarchar(450))
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN SELECT 1 AS fn_SecurityPredicate_result
WHERE @UserId = CAST(SESSION_CONTEXT(N'user_id') AS nvarchar(450))
    OR CAST(SESSION_CONTEXT(N'user_id') AS nvarchar(450)) IS NULL;
GO
```

#### 4.1.2. Polityki dla tabel

**Habits**:
```sql
CREATE SECURITY POLICY dbo.HabitsSecurityPolicy
ADD FILTER PREDICATE dbo.fn_SecurityPredicate(UserId)
    ON dbo.Habits,
ADD BLOCK PREDICATE dbo.fn_SecurityPredicate(UserId)
    ON dbo.Habits AFTER INSERT,
ADD BLOCK PREDICATE dbo.fn_SecurityPredicate(UserId)
    ON dbo.Habits AFTER UPDATE
WITH (STATE = ON);
```

**Checkins**:
```sql
CREATE SECURITY POLICY dbo.CheckinsSecurityPolicy
ADD FILTER PREDICATE dbo.fn_SecurityPredicate(UserId)
    ON dbo.Checkins,
ADD BLOCK PREDICATE dbo.fn_SecurityPredicate(UserId)
    ON dbo.Checkins AFTER INSERT,
ADD BLOCK PREDICATE dbo.fn_SecurityPredicate(UserId)
    ON dbo.Checkins AFTER UPDATE
WITH (STATE = ON);
```

**Notifications**:
```sql
CREATE SECURITY POLICY dbo.NotificationsSecurityPolicy
ADD FILTER PREDICATE dbo.fn_SecurityPredicate(UserId)
    ON dbo.Notifications,
ADD BLOCK PREDICATE dbo.fn_SecurityPredicate(UserId)
    ON dbo.Notifications AFTER INSERT,
ADD BLOCK PREDICATE dbo.fn_SecurityPredicate(UserId)
    ON dbo.Notifications AFTER UPDATE
WITH (STATE = ON);
```

### 4.2. Ustawianie kontekstu sesji
Po uwierzytelnieniu użytkownika aplikacja ustawia SESSION_CONTEXT:
```sql
EXEC sp_set_session_context @key = N'user_id', @value = @authenticatedUserId, @read_only = 1;
```

### 4.3. Bypass dla zadań systemowych
Operacje systemowe (np. background jobs generujące powiadomienia) powinny wykonywać się z kontekstem administratora lub z wyłączoną polityką RLS dla danej sesji.

---

## 5. Zasady biznesowe egzekwowane aplikacyjnie

Następujące reguły **NIE są** implementowane na poziomie bazy danych (CHECK constraints), lecz walidowane w logice aplikacji:

### 5.1. Walidacje przed zapisem
- **Check-in tylko dla zaplanowanych dni**: aplikacja weryfikuje, czy `LocalDate` odpowiada bitowi w `DaysOfWeekMask`
- **Zakaz check-in po DeadlineDate**: walidacja, czy `LocalDate <= DeadlineDate`
- **Limit 20 nawyków per user**: przy tworzeniu nawyku w transakcji sprawdzenie `COUNT(*) < 20`
- **Zakres wartości check-inu**: `0 <= ActualValue <= TargetValueSnapshot`
- **Okno uzupełniania check-in**: maksymalnie 7 dni wstecz od `LocalDate` w strefie czasowej użytkownika

### 5.2. Snapshoty w Checkins
- `TargetValueSnapshot`, `CompletionModeSnapshot` i `HabitTypeSnapshot` zapisywane z bieżących wartości z Habits w momencie check-inu
- Brak edycji check-inów po zapisie (egzekwowane przez brak endpointu UPDATE)

### 5.3. Obliczenia success_rate
- **Dzienny wkład (daily_score)** liczony jest w oparciu o:
  - ActualValue (rzeczywisty wynik dnia),
  - TargetValueSnapshot (wartość docelowa z momentu check-inu),
  - HabitTypeSnapshot (1 = Start, 2 = Stop),
  - CompletionModeSnapshot (1 = Binary, 2 = Quantitative).
- Dla CompletionMode = Binary (1)
  - Zakres: ActualValue ∈ {0, 1}
  - Dzienny wkład: 'daily_score = (ActualValue > 0 ? 1.0 : 0.0)'
- CompletionMode = Quantitative (2)
  - Normalizacja:
  - ratio = ActualValue / TargetValueSnapshot
  - ratio_clamped = min(max(ratio, 0), 1)
- Dla typu Start (HabitTypeSnapshot = 1) – „zacząć / robić coś”: daily_score = ratio_clamped np. 7/10 stron → 0.7, 2/3 posiłków → ≈0.66
- Dla typu Stop (HabitTypeSnapshot = 2) – „przestać / ograniczać”: daily_score = 1 - ratio_clamped np. Target = 3 naruszenia, Actual = 1 → 1 - 1/3 ≈ 0.66
- **Success rate** dla okna (rolling 7/30 dni):
  - success_rate = (suma daily_score w oknie) / (liczba_dni_zaplanowanych_w_oknie)
  - Dni nieplanowane są pomijane w mianowniku (liczą się tylko dni, w których nawyk był zaplanowany).
- Wszystkie obliczenia realizowane są w logice aplikacji (EF Core), brak widoków/TVF w MVP.

### 5.4. Powiadomienia
- **Trigger "miss due"**: wykonywany przez background job sprawdzający check-iny po zakończeniu doby lokalnej użytkownika
- **Unikalność**: jedna notyfikacja MissDue per (HabitId, LocalDate) – egzekwowane przez UNIQUE constraint
- **Brak limitu generowania w MVP**: każdy miss generuje powiadomienie

---

## 6. Decyzje projektowe i uzasadnienia

### 6.1. Wybór typów danych
- **datetime2 zamiast datetime**: lepsza precyzja i zakres dat
- **nvarchar dla tekstów**: pełne wsparcie Unicode (międzynarodowe znaki)
- **tinyint dla enum**: oszczędność miejsca dla małych zbiorów wartości (Type, AiStatus)
- **date dla LocalDate**: przechowywanie daty kalendarzowej bez czasu
- **bigint dla ID w Checkins/Notifications**: przygotowanie na duże wolumeny danych

### 6.2. Denormalizacja UserId
- **Checkins.UserId i Notifications.UserId**: denormalizacja dla:
  - RLS (polityki na poziomie wiersza)
  - Wydajność zapytań "Dziś" i listy powiadomień (unikanie JOIN z Habits)
  - Zapewnienie spójności kaskadowych usunięć

### 6.3. Snapshoty w Checkins
- **TargetValueSnapshot, CompletionModeSnapshot, HabitTypeSnapshot**: zachowanie kontekstu check-inu:
  - Nawyk może zmienić TargetValue / CompletionMode w przyszłości
  - Historyczne check-iny zachowują wartości z momentu zapisu
  - Umożliwia poprawne obliczenia success_rate dla starszych okresów

### 6.4. Brak soft delete
- **Hard delete tylko**: zgodnie z wymaganiami MVP
- Kaskady FK zapewniają spójność
- Brak audytu i archiwizacji w MVP

### 6.5. Brak partycjonowania
- **MVP bez partycjonowania**: uproszczenie implementacji
- **Przyszłość**: możliwość partycjonowania Checkins i Notifications po LocalDate przy dużych wolumenach

### 6.6. Indeksy INCLUDE
- **Pokrywające indeksy**: zmniejszenie liczby lookup'ów do tabeli bazowej
- **Trade-off**: większy rozmiar indeksu vs. szybsze zapytania
- Uzasadnione dla najczęstszych zapytań (lista nawyków, widok "Dziś", powiadomienia)

### 6.7. LocalDate vs UTC
- **LocalDate (date)**: dla operacji kalendarzowych użytkownika (check-in, kalendarz)
- **CreatedAtUtc (datetime2)**: dla audytu i chronologii zdarzeń
- **Konwersje**: aplikacja odpowiada za konwersję między UTC a czasem lokalnym używając TimeZoneId

### 6.8. Typy enum jako tinyint
- **Type (Habits)**: 1=Start, 2=Stop
- **Type (Notifications)**: 1=MissDue (możliwość rozszerzenia)
- **AiStatus**: 1=Success, 2=Fallback, 3=Error
- Wartości dokumentowane w kodzie aplikacji (enum w C#)

---

## 7. Migracje i deployment

### 7.1. Kolejność migracji
1. **Migracja 1**: Tabele Identity (AspNetUsers + rozszerzenia)
2. **Migracja 2**: Tabele domenowe (Habits, Checkins, Notifications)
3. **Migracja 3**: Indeksy pokrywające
4. **Migracja 4**: RLS (funkcje, polityki)

### 7.2. Seed data
- Brak wymaganych danych początkowych w MVP
- Opcjonalnie: dane testowe dla developmentu

### 7.3. Rollback strategy
- Każda migracja z odpowiadającym `Down()` w EF Core
- Testowanie rollbacku w środowisku pre-production

---

## 8. Monitoring i obserwability

### 8.1. Metryki do śledzenia (poza MVP)
- Czas wykonania zapytań na indeksach
- Fragmentacja indeksów (maintenance plan)
- Rozmiar tabel i indeksów
- Statystyki RLS overhead

### 8.2. Logowanie błędów AI
- AiError i AiStatus w Notifications
- Analiza fallbacków dla optymalizacji integracji AI

---

## 9. Nierozstrzygnięte kwestie (do decyzji)

### 9.1. EndedAtUtc/Status w Habits
- **Pytanie**: Czy dodać kolumny dla raportowania zamkniętych nawyków?
- **Opcje**:
  - Dodać `EndedAtUtc datetime2 NULL`, `Status tinyint` (1=Active, 2=Completed, 3=Abandoned)
  - Pozostawić bez zmian; zamknięte nawyki = te z DeadlineDate w przeszłości
- **Rekomendacja**: Pozostawić bez zmian w MVP; status można wyliczyć z DeadlineDate

### 9.2. Semantyka MissDue przy zmianie strefy czasowej
- **Pytanie**: Jak określić moment "cięcia" doby przy zmianie TimeZoneId w trakcie dnia?
- **Rekomendacja**: Zmiana strefy czasu obowiązuje od następnej doby (00:00 nowej strefy)

### 9.3. Maksymalna długość AiError
- **Pytanie**: Czy 512 znaków wystarczy? Czy logować szczegóły poza DB?
- **Rekomendacja**: 512 znaków dla podstawowej diagnozy; pełne logi techniczne w systemie logowania aplikacji (Serilog, Application Insights)

---

## 10. Wersjonowanie schematu

- **Aktualna wersja**: 1.0 (MVP)
- **Historia zmian**:
  - 2025-11-22: Wersja inicjalna (MVP)

---

## 11. Podsumowanie

Schemat bazy danych HabitFlow zapewnia:
- ✅ Pełną obsługę ASP.NET Core Identity
- ✅ Model domenowy dla nawyków typu Start/Stop
- ✅ Harmonogram oparty na masce dni tygodnia
- ✅ Check-iny z immutability i snapshotami
- ✅ Powiadomienia AI z fallbackiem
- ✅ Row-Level Security dla izolacji danych użytkowników
- ✅ Indeksy zoptymalizowane pod widoki "Dziś", kalendarz i wykresy
- ✅ Hard delete z kaskadami
- ✅ Gotowość do EF Core code-first migrations
- ✅ Skalowanie i wydajność dla MVP (do ~1000 użytkowników)

**Schemat jest gotowy do implementacji jako migracje EF Core.**
