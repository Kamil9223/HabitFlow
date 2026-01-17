# API Endpoint Implementation Plan: GET /api/v1/habits/{habitId}/progress/rolling

## 1. Przegląd punktu końcowego

Endpoint zwraca serię kroczącego wskaźnika sukcesu (rolling success rate) dla wybranego nawyku w oknie czasowym 7 lub 30 dni, kończącym się w określonej dacie (włącznie). Dla każdego dnia w zakresie oblicza:
- Liczbę zaplanowanych dni w oknie wstecz (`plannedDays`)
- Sumę dziennych wyników (`sumDailyScore`) z check-inów
- Wskaźnik sukcesu (`successRate = sumDailyScore / plannedDays`)

Endpoint umożliwia wizualizację postępu nawyku w czasie w formie wykresu liniowego pokazującego trendy w realizacji celów.

## 2. Szczegóły żądania

- **Metoda HTTP**: GET
- **Struktura URL**: `/api/v1/habits/{habitId}/progress/rolling`
- **Parametry**:
  - **Wymagane**:
    - `habitId` (route, int): Identyfikator nawyku
    - `windowDays` (query, int): Szerokość okna czasowego (7 lub 30)
  - **Opcjonalne**:
    - `until` (query, string): Data końcowa w formacie `YYYY-MM-DD` (domyślnie: dzisiejsza data w lokalnej strefie czasowej użytkownika)
- **Request Body**: Brak (metoda GET)
- **Nagłówki**:
  - `Authorization: Bearer <token>` (wymagane)

**Przykład żądania**:
```
GET /api/v1/habits/101/progress/rolling?windowDays=7&until=2025-12-07
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## 3. Wykorzystywane typy

### Query Model (Application/Queries/Progress/)
```csharp
public record GetProgressRollingQuery(
    int HabitId,
    int WindowDays,
    DateOnly? Until
) : IQuery<Result<ProgressRollingResponse>>;
```

### Response DTO (Contracts/Progress/)
Istniejący typ `ProgressRollingResponse`:
```csharp
public record ProgressRollingResponse(
    int HabitId,
    int WindowDays,
    string Until,
    ProgressRollingPoint[] Points
);

public record ProgressRollingPoint(
    string Date,
    int PlannedDays,
    decimal SumDailyScore,
    decimal SuccessRate
);
```

### Validator (Application/Queries/Progress/Validators/)
```csharp
public class GetProgressRollingQueryValidator : AbstractValidator<GetProgressRollingQuery>
{
    // Walidacja:
    // - HabitId > 0
    // - WindowDays == 7 || WindowDays == 30
    // - Until: jeśli podane, format YYYY-MM-DD + nie przyszłość (względem lokalnej daty użytkownika)
}
```

### Handler (Application/Queries/Progress/Handlers/)
```csharp
public class GetProgressRollingQueryHandler
    : IQueryHandler<GetProgressRollingQuery, Result<ProgressRollingResponse>>
{
    // Zależności: DbContext (lub IProgressReadStore), ICurrentUserService, TimeProvider
}
```

## 4. Szczegóły odpowiedzi

### Sukces (200 OK)
```json
{
  "habitId": 101,
  "windowDays": 7,
  "until": "2025-12-07",
  "points": [
    {
      "date": "2025-12-01",
      "plannedDays": 3,
      "sumDailyScore": 2.1,
      "successRate": 0.7
    },
    {
      "date": "2025-12-02",
      "plannedDays": 4,
      "sumDailyScore": 3.5,
      "successRate": 0.875
    }
  ]
}
```

### Błędy
- **400 Bad Request**: Nieprawidłowe parametry wejściowe
  ```json
  {
    "type": "https://tools.ietf.org/html/rfc7807#section-6.5.1",
    "title": "Validation Error",
    "status": 400,
    "errors": {
      "WindowDays": ["WindowDays must be 7 or 30."],
      "Until": ["Until date cannot be in the future."]
    }
  }
  ```

- **401 Unauthorized**: Brak lub nieprawidłowe uwierzytelnienie
  ```json
  {
    "type": "https://tools.ietf.org/html/rfc7807#section-6.5.1",
    "title": "Unauthorized",
    "status": 401,
    "detail": "Authentication required."
  }
  ```

- **404 Not Found**: Habit nie istnieje lub nie należy do użytkownika
  ```json
  {
    "type": "https://tools.ietf.org/html/rfc7807#section-6.5.4",
    "title": "Not Found",
    "status": 404,
    "detail": "Habit with ID 101 not found."
  }
  ```

- **500 Internal Server Error**: Nieoczekiwany błąd serwera
  ```json
  {
    "type": "https://tools.ietf.org/html/rfc7807#section-6.6.1",
    "title": "Internal Server Error",
    "status": 500,
    "detail": "An unexpected error occurred."
  }
  ```

## 5. Przepływ danych

### Krok 1: Walidacja i autoryzacja
1. Endpoint odbiera żądanie i konstruuje `GetProgressRollingQuery`
2. Walidator sprawdza poprawność parametrów (przed wykonaniem handlera)
3. Handler pobiera `userId` z `ICurrentUserService` (uwierzytelnienie)

### Krok 2: Weryfikacja własności nawyku
1. Handler sprawdza, czy habit o `habitId` istnieje i należy do `userId`
2. Jeśli nie: zwraca `Result.Failure` z kodem `HabitNotFound` (404)
3. Pobiera strefę czasową użytkownika z tabeli `users` (`timezone_id`)

### Krok 3: Określenie zakresu dat
1. Ustalenie daty końcowej:
   - Jeśli `until` podane: użyj tej daty
   - Jeśli brak: użyj dzisiejszej daty w lokalnej strefie czasowej użytkownika (`TimeProvider.GetLocalNow(timeZone).Date`)
2. Obliczenie daty początkowej: `startDate = until - windowDays + 1`
3. Wygenerowanie listy wszystkich dat w zakresie `[startDate, until]`

### Krok 4: Obliczenia metryk dla każdej daty
Dla każdej daty `d` w zakresie:

1. **Obliczenie `plannedDays`** (liczba dni zaplanowanych w oknie `[d - windowDays + 1, d]`):
   - Zapytanie do `habit_schedules` z uwzględnieniem:
     - `start_date <= d` i (`end_date >= d - windowDays + 1` lub `end_date IS NULL`)
     - Dni tygodnia z `days_of_week` (bitmask)
     - Wykluczenia z `schedule_exceptions` dla tego okresu
   - Zsumowanie dni spełniających warunki

2. **Obliczenie `sumDailyScore`** (suma wyników z check-inów w oknie):
   - Zapytanie do `checkins` WHERE `habit_id = habitId` AND `local_date` BETWEEN `[d - windowDays + 1, d]`
   - Suma kolumny `daily_score`

3. **Obliczenie `successRate`**:
   - Jeśli `plannedDays > 0`: `successRate = sumDailyScore / plannedDays`
   - Jeśli `plannedDays == 0`: `successRate = 0` (lub `null`, zależnie od wymagań biznesowych)

### Krok 5: Budowa odpowiedzi
1. Konstruowanie tablicy `ProgressRollingPoint[]` dla wszystkich dat
2. Zwrócenie `Result.Success(ProgressRollingResponse)`

### Krok 6: Mapowanie Result → HTTP
Endpoint konwertuje `Result<ProgressRollingResponse>` na odpowiedź HTTP:
- Sukces → `200 OK` z JSON body
- Błąd → odpowiedni kod statusu HTTP + `ProblemDetails`

### Diagram przepływu
```
Request → Endpoint → Validator
                  ↓
            Query Dispatcher
                  ↓
         GetProgressRollingQueryHandler
                  ↓
    ┌─────────────────────────────┐
    │ 1. Get userId (auth)        │
    │ 2. Verify habit ownership   │
    │ 3. Get user timezone        │
    │ 4. Calculate date range     │
    │ 5. Query DB:                │
    │    - habit_schedules        │
    │    - schedule_exceptions    │
    │    - checkins               │
    │ 6. Compute metrics per date │
    │ 7. Build response DTO       │
    └─────────────────────────────┘
                  ↓
         Result<ProgressRollingResponse>
                  ↓
        Endpoint (Result → HTTP)
                  ↓
            HTTP Response
```

## 6. Względy bezpieczeństwa

### Uwierzytelnianie
- **Wymagane**: Endpoint wymaga aktywnej sesji użytkownika (token JWT lub cookie sesji)
- **401 Unauthorized**: Zwracany gdy użytkownik nie jest uwierzytelniony
- Implementacja przez middleware uwierzytelniania ASP.NET Core

### Autoryzacja
- **IDOR Prevention**: Handler MUSI zweryfikować, że habit o `habitId` należy do zalogowanego użytkownika (`userId`)
- **404 Not Found**: Zwracany zarówno gdy habit nie istnieje, jak i gdy nie należy do użytkownika (nie ujawniamy, że habit istnieje)
- Zapytanie: `WHERE habit_id = @habitId AND user_id = @userId`

### Walidacja danych wejściowych
- **habitId**: Typ int, musi być > 0 (walidacja)
- **windowDays**: Tylko wartości 7 lub 30 dozwolone (whitelist, prevent DoS przez nadmierne obliczenia)
- **until**:
  - Walidacja formatu daty (YYYY-MM-DD)
  - Prevent injection: używaj `DateOnly.TryParseExact`
  - Nie może być w przyszłości (prevent abuse)

### SQL Injection
- **Bezpieczne**: EF Core automatycznie parametryzuje zapytania
- Używaj LINQ queries, unikaj `FromSqlRaw` z niesparametryzowanymi danymi

### Rate Limiting
- Rozważ ograniczenie liczby żądań na użytkownika (np. 100 req/min)
- Szczególnie dla okna 30-dniowego (więcej obliczeń)

### Leak informacji
- Nie zwracaj szczegółów wewnętrznych błędów w production (stack traces)
- Ujednolicone `ProblemDetails` dla błędów

## 7. Obsługa błędów

| Scenariusz | Kod HTTP | Error Type | Detail Message |
|------------|----------|------------|----------------|
| `windowDays` nie jest 7 ani 30 | 400 | ValidationError | "WindowDays must be 7 or 30." |
| `until` nieprawidłowy format | 400 | ValidationError | "Until must be in format YYYY-MM-DD." |
| `until` w przyszłości | 400 | ValidationError | "Until date cannot be in the future." |
| `habitId <= 0` | 400 | ValidationError | "HabitId must be greater than 0." |
| Brak uwierzytelnienia | 401 | Unauthorized | "Authentication required." |
| Habit nie istnieje lub nie należy do użytkownika | 404 | NotFound | "Habit with ID {habitId} not found." |
| Błąd połączenia z bazą danych | 500 | InternalServerError | "An unexpected error occurred." |
| Nieobsłużony wyjątek | 500 | InternalServerError | "An unexpected error occurred." |

### Strategia obsługi błędów w handlerze
```csharp
// Przykład wzorca Result
if (!habitExists || !habitBelongsToUser)
    return Result.Failure<ProgressRollingResponse>(Error.NotFound("HabitNotFound", $"Habit with ID {habitId} not found."));

// Wyjątki dla błędów krytycznych (db connection, etc.) są przechwytywane przez globalny handler
```

### Globalny exception handler
- Middleware przechwytuje nieobsłużone wyjątki
- Loguje szczegóły do ILogger
- Zwraca ogólny `500 ProblemDetails` bez ujawniania szczegółów wewnętrznych

## 8. Rozważania dotyczące wydajności

### Zapytania do bazy danych
- **AsNoTracking**: Wszystkie zapytania read-only muszą używać `.AsNoTracking()`
- **Projekcja**: Używaj `.Select()` do pobierania tylko niezbędnych kolumn
- **Batch queries**: Zapytaj o dane dla wszystkich dat w zakresie jednym lub kilkoma zapytaniami, zamiast N osobnych zapytań
  ```csharp
  // Dobre: jedno zapytanie dla wszystkich dat
  var checkins = await context.Checkins
      .Where(c => c.HabitId == habitId && c.LocalDate >= startDate && c.LocalDate <= endDate)
      .Select(c => new { c.LocalDate, c.DailyScore })
      .ToListAsync();

  // Złe: N zapytań (po jednym dla każdej daty)
  foreach (var date in dates) {
      var score = await context.Checkins.Where(...).SumAsync(); // N+1!
  }
  ```

### Indeksy bazy danych
Upewnij się, że istnieją indeksy na:
- `checkins(habit_id, local_date)` — composite index dla filtrowania check-inów
- `habits(user_id, habit_id)` — dla weryfikacji własności
- `habit_schedules(habit_id, start_date, end_date)` — dla zapytań o harmonogram
- `schedule_exceptions(habit_schedule_id, exception_date)` — dla wykluczeń

### Skompilowane zapytania (Compiled Queries)
Jeśli endpoint będzie często używany, rozważ użycie EF Core compiled queries dla gorącej ścieżki:
```csharp
private static readonly Func<AppDbContext, int, DateOnly, DateOnly, Task<List<CheckinData>>> GetCheckinsCompiled =
    EF.CompileAsyncQuery((AppDbContext ctx, int habitId, DateOnly start, DateOnly end) =>
        ctx.Checkins
            .Where(c => c.HabitId == habitId && c.LocalDate >= start && c.LocalDate <= end)
            .Select(c => new CheckinData(c.LocalDate, c.DailyScore))
            .ToList());
```

### Caching
- **Opcjonalnie**: Cache wyników na poziomie aplikacji (Redis, in-memory) z TTL 5-15 minut
- Klucz cache: `progress_rolling:{userId}:{habitId}:{windowDays}:{until}`
- Invalidacja: przy nowym check-inie dla tego nawyku
- Może być overkill dla MVP, ale wartościowe dla skali

### Optymalizacja obliczeń
- Pre-compute `plannedDays` dla całego zakresu jednym zapytaniem (jeśli możliwe)
- Rozważ denormalizację (materialized view) dla często używanych agregacji
- Dla bardzo dużych zakresów: chunking po 10-14 dni

### Limity
- Maksymalny `windowDays` to 30 (już w specyfikacji) — zapobiega nadmiernemu obciążeniu
- Rozważ paginację dla bardzo długich serii (np. ostatnie 365 dni) w przyszłych wersjach

## 9. Etapy wdrożenia

### Krok 1: Utworzenie Query Model i Validator
**Lokalizacja**: `HabitFlow.Api/Application/Queries/Progress/`
1. Dodaj rekord `GetProgressRollingQuery.cs`:
   ```csharp
   public record GetProgressRollingQuery(
       int HabitId,
       int WindowDays,
       DateOnly? Until
   ) : IQuery<Result<ProgressRollingResponse>>;
   ```
2. Utwórz `Validators/GetProgressRollingQueryValidator.cs`:
   - Reguła: `HabitId > 0`
   - Reguła: `WindowDays == 7 || WindowDays == 30`
   - Reguła: Jeśli `Until` podane, musi być valid date i `<= Today` (użyj `ICurrentUserService` dla timezone)
3. Zarejestruj validator w DI (`Program.cs` lub extension method)

### Krok 2: Implementacja Query Handler
**Lokalizacja**: `HabitFlow.Api/Application/Queries/Progress/Handlers/GetProgressRollingQueryHandler.cs`
1. Zaimplementuj `IQueryHandler<GetProgressRollingQuery, Result<ProgressRollingResponse>>`
2. Wstrzyknij zależności:
   - `AppDbContext` (lub `IProgressReadStore`)
   - `ICurrentUserService` (dla `userId` i timezone)
   - `TimeProvider` (dla `GetLocalNow`)
3. Implementacja logiki:
   - Pobierz `userId` i zweryfikuj własność habit
   - Pobierz timezone użytkownika
   - Oblicz zakres dat (`startDate`, `endDate`)
   - Zapytaj DB:
     - Pobierz wszystkie check-iny w zakresie (batch query)
     - Pobierz harmonogramy i wykluczenia
   - Dla każdej daty w zakresie:
     - Oblicz `plannedDays` (helper method)
     - Oblicz `sumDailyScore` (z pobranych check-inów)
     - Oblicz `successRate`
   - Zbuduj `ProgressRollingResponse` z tablicą punktów
   - Zwróć `Result.Success(response)`

### Krok 3: Helper Methods dla obliczeń
W handlerze lub osobnej klasie serwisowej:
1. `CalculatePlannedDays(habitSchedules, exceptions, startDate, endDate, timezone)`:
   - Iteruj przez każdy dzień w zakresie
   - Sprawdź czy dzień jest zaplanowany (days_of_week bitmask)
   - Sprawdź czy dzień nie jest wykluczony (schedule_exceptions)
   - Zlicz
2. `CalculateSuccessRate(sumDailyScore, plannedDays)`:
   - Jeśli `plannedDays == 0` → return 0
   - Else → return `sumDailyScore / plannedDays`

### Krok 4: Rejestracja handlera w DI
**Lokalizacja**: `HabitFlow.Api/Program.cs` lub extension method
```csharp
services.AddScoped<IQueryHandler<GetProgressRollingQuery, Result<ProgressRollingResponse>>,
                   GetProgressRollingQueryHandler>();
```

### Krok 5: Mapowanie endpointu w Minimal API
**Lokalizacja**: `HabitFlow.Api/Endpoints/ProgressEndpoints.cs`
1. Dodaj endpoint w metodzie `MapProgressEndpoints`:
   ```csharp
   group.MapGet("/{habitId:int}/progress/rolling", GetProgressRolling)
        .WithName("GetProgressRolling")
        .WithOpenApi()
        .Produces<ProgressRollingResponse>(200)
        .Produces<ProblemDetails>(400)
        .Produces<ProblemDetails>(401)
        .Produces<ProblemDetails>(404);

   static async Task<IResult> GetProgressRolling(
       int habitId,
       [FromQuery] int windowDays,
       [FromQuery] string? until,
       IQueryDispatcher queryDispatcher,
       CancellationToken ct)
   {
       DateOnly? untilDate = null;
       if (!string.IsNullOrEmpty(until) &&
           !DateOnly.TryParseExact(until, "yyyy-MM-dd", out var parsed))
       {
           return Results.BadRequest(new ProblemDetails
           {
               Title = "Invalid Date Format",
               Detail = "Until must be in format YYYY-MM-DD."
           });
       }
       if (!string.IsNullOrEmpty(until))
           untilDate = DateOnly.ParseExact(until, "yyyy-MM-dd");

       var query = new GetProgressRollingQuery(habitId, windowDays, untilDate);
       var result = await queryDispatcher.DispatchAsync(query, ct);

       return result.IsSuccess
           ? Results.Ok(result.Value)
           : result.ToHttpResult(); // helper method konwertujący Result → IResult
   }
   ```

### Krok 6: Testy jednostkowe
**Lokalizacja**: `HabitFlow.Tests/UnitTests/Queries/Progress/`
1. `GetProgressRollingQueryValidatorTests.cs`:
   - Test: `WindowDays_WhenNot7Or30_ShouldHaveValidationError`
   - Test: `Until_WhenInFuture_ShouldHaveValidationError`
   - Test: `Until_WhenInvalidFormat_ShouldHaveValidationError`
   - Test: `HabitId_WhenZeroOrNegative_ShouldHaveValidationError`
   - Test: `ValidQuery_ShouldNotHaveValidationError`

2. `GetProgressRollingQueryHandlerTests.cs`:
   - Mock: `DbContext`, `ICurrentUserService`, `TimeProvider`
   - Test: `Handle_HabitNotFound_ReturnsNotFoundError`
   - Test: `Handle_HabitDoesNotBelongToUser_ReturnsNotFoundError`
   - Test: `Handle_ValidRequest_ReturnsCorrectPoints`
   - Test: `Handle_WindowDays7_CalculatesCorrectPlannedDays`
   - Test: `Handle_WithExceptions_ExcludesExceptionDays`
   - Test: `Handle_NoCheckIns_ReturnsZeroScores`
   - Test: `Handle_ZeroPlannedDays_ReturnsZeroSuccessRate`

### Krok 7: Testy integracyjne
**Lokalizacja**: `HabitFlow.Tests/IntegrationTests/Endpoints/`
1. `ProgressEndpointsTests.cs`:
   - Setup: TestContainers SQL Server, seed user + habit + schedules + check-ins
   - Test: `GetProgressRolling_ValidRequest_Returns200WithCorrectData`
   - Test: `GetProgressRolling_InvalidWindowDays_Returns400`
   - Test: `GetProgressRolling_FutureUntilDate_Returns400`
   - Test: `GetProgressRolling_HabitNotFound_Returns404`
   - Test: `GetProgressRolling_HabitBelongsToOtherUser_Returns404`
   - Test: `GetProgressRolling_Unauthorized_Returns401`
   - Test: `GetProgressRolling_DefaultUntil_UsesToday`
   - Test: `GetProgressRolling_WithExceptions_CorrectlyExcludesDays`

### Krok 8: Optymalizacja bazy danych
1. Sprawdź istniejące indeksy w migracjach:
   - `checkins(habit_id, local_date)` — dodaj jeśli nie istnieje
   - `habits(user_id, habit_id)` — dodaj jeśli nie istnieje
2. Jeśli brak, utwórz nową migrację:
   ```bash
   dotnet ef migrations add AddProgressRollingIndexes --project HabitFlow.Api
   ```
3. Zastosuj migrację:
   ```bash
   dotnet ef database update --project HabitFlow.Api
   ```

### Krok 9: Dokumentacja API (OpenAPI)
1. Endpoint automatycznie dodany do Swagger przez `.WithOpenApi()`
2. Sprawdź dokumentację w trybie Development: `https://localhost:5001/swagger`
3. Opcjonalnie dodaj przykłady XML comments dla lepszej dokumentacji:
   ```csharp
   /// <summary>
   /// Get rolling success rate series for a habit
   /// </summary>
   /// <param name="habitId">Habit identifier</param>
   /// <param name="windowDays">Window size (7 or 30 days)</param>
   /// <param name="until">End date (YYYY-MM-DD), defaults to today</param>
   ```

### Krok 10: Manualne testowanie
1. Uruchom API: `dotnet run --project HabitFlow.Api`
2. Utwórz użytkownika i nawyk przez frontend lub Postman
3. Dodaj kilka check-inów w różnych datach
4. Testuj endpoint:
   ```bash
   # Happy path
   curl -X GET "https://localhost:5001/api/v1/habits/1/progress/rolling?windowDays=7" \
        -H "Authorization: Bearer <token>"

   # Invalid windowDays
   curl -X GET "https://localhost:5001/api/v1/habits/1/progress/rolling?windowDays=15" \
        -H "Authorization: Bearer <token>"

   # Future date
   curl -X GET "https://localhost:5001/api/v1/habits/1/progress/rolling?windowDays=7&until=2099-01-01" \
        -H "Authorization: Bearer <token>"
   ```

### Krok 11: Code review i refactoring
1. Sprawdź zgodność z Clean Architecture (separacja warstw)
2. Upewnij się, że handler używa `AsNoTracking()` i projekcji
3. Sprawdź, czy wszystkie błędy są poprawnie mapowane na kody HTTP
4. Przejrzyj testy pod kątem pokrycia edge cases
5. Uruchom `dotnet format` przed commitem

### Krok 12: Commit i PR
1. Commit changes:
   ```bash
   git add .
   git commit -m "feat(api): implement GET /api/v1/habits/{habitId}/progress/rolling endpoint"
   ```
2. Push do repozytorium
3. Utwórz Pull Request z opisem:
   - Cel: implementacja endpointu rolling success rate
   - Testy: jednostkowe + integracyjne (lista testów)
   - Breaking changes: brak
   - Related issue: Closes #XXX

---

## Podsumowanie

Ten plan zapewnia kompletną implementację endpointu `GET /api/v1/habits/{habitId}/progress/rolling` zgodnie z:
- **Specyfikacją API**: parametry, response, error codes
- **Clean Architecture**: separacja Query/Handler/Validator
- **Backend rules**: CQS, Result pattern, EF Core best practices
- **Bezpieczeństwo**: autoryzacja, walidacja, IDOR prevention
- **Wydajność**: AsNoTracking, batch queries, indeksy
- **Testowanie**: jednostkowe + integracyjne zgodnie z test-plan.md

Handler oblicza kroczący wskaźnik sukcesu dla każdego dnia w zakresie, uwzględniając harmonogramy, wykluczenia i check-iny użytkownika, zwracając precyzyjne dane do wizualizacji postępu nawyku w czasie.
