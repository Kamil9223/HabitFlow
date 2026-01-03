# API Endpoint Implementation Plan: Get Today

## 1. Przeglad punktu koncowego
Endpoint GET `/api/v1/today` zwraca liste zaplanowanych nawykow na wskazany dzien (domyslnie dzis w strefie czasowej uzytkownika) wraz z informacja o tym, czy check-in juz istnieje. Dane sa tylko do odczytu i maja sluzyc szybkiemu widokowi "Dzis".

## 2. Szczegoly zadania
- **Metoda HTTP:** GET
- **Struktura URL:** `/api/v1/today`
- **Parametry:**
  - **Wymagane:** brak
  - **Opcjonalne:**
    - `date` (YYYY-MM-DD) - data lokalna, jesli brak, uzyj "today" w strefie czasowej uzytkownika
- **Request Body:** brak

## 3. Wykorzystywane typy
- **DTO:**
  - `TodayResponse` (Date, Items)
  - `TodayItem` (HabitId, Title, Type, CompletionMode, TargetValue, TargetUnit, IsPlanned, HasCheckin)
- **Query model:**
  - `GetTodayQuery : IQuery<TodayResponse>` (parametry: `DateOnly? Date`)
  - `GetTodayQueryHandler : IQueryHandler<GetTodayQuery, TodayResponse>`

## 4. Szczegoly odpowiedzi
- **200 OK:** zwraca `TodayResponse`
- **401 Unauthorized:** brak/niepoprawny token
- **400 Bad Request (opcjonalnie):** niepoprawny format `date` (gdy model binding nie zmapuje parametru)

## 5. Przeplyw danych
1. Endpoint pobiera opcjonalny query param `date`.
2. Z kontekstu auth pobierany jest `UserId` oraz `TimeZoneId` (z profilu uzytkownika).
3. Jesli `date` nie zostal podany, oblicz lokalna date uzytkownika: `UtcNow` -> `TimeZoneInfo` -> `DateOnly`.
4. Handler wykonuje zapytanie read-only:
   - filtr po `Habits.UserId == currentUserId`.
   - filtr po zaplanowaniu na dzien (maska `DaysOfWeekMask`).
   - projekcja do `TodayItem` z `HasCheckin = exists` na `Checkins` dla `LocalDate == targetDate`.
5. Zwracany jest `TodayResponse` z `Date = targetDate` i lista elementow.

## 6. Wzgledy bezpieczenstwa
- Endpoint wymaga autoryzacji (`RequireAuthorization`).
- RLS w SQL Server dodatkowo filtruje dane po `UserId` (SESSION_CONTEXT).
- Brak danych wrazliwych w odpowiedzi.
- Walidacja inputu tylko dla `date` (format i poprawne mapowanie do `DateOnly`).

## 7. Obsluga bledow
- **401 Unauthorized:** brak/niepoprawny token.
- **400 Bad Request:** niepoprawny format `date` (jezeli model binding nie przejdzie).
- **500 Internal Server Error:** nieoczekiwane bledy (globalny handler ProblemDetails).

## 8. Wydajnosc
- `AsNoTracking` i projekcja `Select` tylko do wymaganych pol.
- Jedno zapytanie z joinem/subquery (np. `Any`) na `Checkins` dla danej daty.
- Wspieranie przez indeksy: `IX_Habits_UserId_CreatedAtUtc` i `IX_Checkins_UserId_LocalDate_HabitId`.
- Limit nawykow per user (20) ogranicza rozmiar wyniku.

## 9. Kroki implementacji
1. Dodaj `GetTodayQuery` i `GetTodayQueryHandler` w warstwie Application.
2. W handlerze uzyj `DbContext` (read-only) z `AsNoTracking` i projekcja do `TodayItem`.
3. Zaimplementuj logike wyboru `targetDate` (z query param lub z `TimeZoneId` uzytkownika).
4. Zmapuj handler do `TodayEndpoints` przez `IQueryDispatcher`.
5. Dodaj walidacje `date` (model binding / custom validation) i zwracanie `400` dla zlego formatu.
6. Dodaj testy jednostkowe dla handlera (planowanie, hasCheckin) i testy integracyjne endpointu.
7. Zweryfikuj wydajnosc zapytania (plan zapytania + indeksy) oraz odpowiedzi 200/401.
