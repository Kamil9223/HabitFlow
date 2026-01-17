# API Endpoint Implementation Plan: GET /api/v1/habits/{habitId}/checkins

## 1. Przegląd punktu końcowego

Endpoint służy do pobierania listy check-inów dla konkretnego nawyku w określonym zakresie dat. Dane są wykorzystywane do wyświetlania wykresów postępów i historii wykonywania nawyku. Endpoint zwraca szczegółowe informacje o każdym check-inie, włączając snapshoty wartości docelowych i konfiguracji nawyku z momentu wykonania check-inu.

**Główne funkcje:**
- Filtrowanie check-inów po zakresie dat (from-to)
- Zwracanie snapshotów konfiguracji nawyku dla każdego check-inu
- Weryfikacja ownership nawyku (security)
- Optymalizacja zapytań poprzez wykorzystanie indeksów

## 2. Szczegóły żądania

- **Metoda HTTP:** GET
- **Struktura URL:** `/api/v1/habits/{habitId}/checkins`
- **Parametry:**
  - **Wymagane:**
    - `habitId` (route parameter, bigint) - identyfikator nawyku
    - `from` (query parameter, string) - data początkowa w formacie YYYY-MM-DD
    - `to` (query parameter, string) - data końcowa w formacie YYYY-MM-DD
  - **Opcjonalne:** brak
- **Request Body:** brak
- **Autoryzacja:** JWT Bearer token (wymagany)

**Przykładowe żądanie:**
```
GET /api/v1/habits/101/checkins?from=2025-11-01&to=2025-11-30
Authorization: Bearer {token}
```

## 3. Wykorzystywane typy

### DTOs (Contracts)

**CheckinListResponse** (już istnieje w `Contracts/Checkins/CheckinListResponse.cs`):
```csharp
public record CheckinListResponse(
    int HabitId,
    string From,
    string To,
    List<CheckinItemDto> Items
);

public record CheckinItemDto(
    long Id,
    string LocalDate,
    int ActualValue,
    short TargetValueSnapshot,
    byte CompletionModeSnapshot,
    byte HabitTypeSnapshot,
    bool IsPlanned
);
```

### Command Modele (CQRS)

**GetCheckinsQuery** (nowy plik: `Features/Checkins/GetCheckins/GetCheckinsQuery.cs`):
```csharp
public record GetCheckinsQuery(
    int HabitId,
    DateOnly From,
    DateOnly To,
    string UserId
) : IRequest<Result<CheckinListResponse>>;
```

### Validator (FluentValidation)

**GetCheckinsQueryValidator** (nowy plik: `Features/Checkins/GetCheckins/GetCheckinsQueryValidator.cs`):
- Walidacja habitId > 0
- Walidacja from <= to
- Walidacja maksymalnego zakresu dat (np. 365 dni)

### Handler (MediatR)

**GetCheckinsQueryHandler** (nowy plik: `Features/Checkins/GetCheckins/GetCheckinsQueryHandler.cs`):
- Weryfikacja ownership nawyku
- Pobranie check-inów z zakresu dat
- Mapowanie na DTOs

## 4. Szczegóły odpowiedzi

### Sukces (200 OK)
```json
{
  "habitId": 101,
  "from": "2025-11-01",
  "to": "2025-11-30",
  "items": [
    {
      "id": 1,
      "localDate": "2025-11-02",
      "actualValue": 1,
      "targetValueSnapshot": 1,
      "completionModeSnapshot": 1,
      "habitTypeSnapshot": 1,
      "isPlanned": true
    }
  ]
}
```

### Błąd walidacji (400 Bad Request)
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "From": ["Date 'from' cannot be after 'to'"],
    "To": ["Date range cannot exceed 365 days"]
  }
}
```

### Brak autoryzacji (401 Unauthorized)
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401
}
```

### Nawyk nie znaleziony (404 Not Found)
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Habit not found",
  "status": 404,
  "detail": "Habit with ID 101 was not found"
}
```

## 5. Przepływ danych

```
1. HTTP Request → Minimal API Endpoint
   ↓
2. Model Binding (habitId, from, to z query string)
   ↓
3. Ekstrakcja UserId z ClaimsPrincipal (JWT)
   ↓
4. Parsowanie dat string → DateOnly
   ↓
5. Utworzenie GetCheckinsQuery(habitId, from, to, userId)
   ↓
6. Walidacja przez FluentValidation
   ├─ Błąd → 400 Bad Request
   └─ OK → dalej
   ↓
7. GetCheckinsQueryHandler.Handle():
   a) Query do Habits: WHERE Id = habitId AND UserId = userId
      ├─ Not found → Result.Failure (404)
      └─ Found → dalej

   b) Query do Checkins:
      SELECT Id, HabitId, LocalDate, ActualValue,
             TargetValueSnapshot, CompletionModeSnapshot,
             HabitTypeSnapshot, IsPlanned
      FROM Checkins WITH (INDEX(IX_Checkins_HabitId_LocalDate))
      WHERE HabitId = @habitId
        AND LocalDate >= @from
        AND LocalDate <= @to
      ORDER BY LocalDate ASC

   c) Mapowanie List<Checkin> → List<CheckinItemDto>

   d) Utworzenie CheckinListResponse

   e) Return Result.Success(response)
   ↓
8. Endpoint zwraca odpowiedź:
   ├─ Result.IsSuccess → 200 OK + JSON
   └─ Result.IsFailure → odpowiedni kod błędu
```

**Interakcje z bazą danych:**
- **Query 1 (Weryfikacja Habit):**
  ```sql
  SELECT Id FROM Habits
  WHERE Id = @habitId AND UserId = @userId
  ```
  - Wykorzystuje PK index na Habits

- **Query 2 (Pobranie Checkins):**
  ```sql
  SELECT Id, HabitId, LocalDate, ActualValue,
         TargetValueSnapshot, CompletionModeSnapshot,
         HabitTypeSnapshot, IsPlanned
  FROM Checkins
  WHERE HabitId = @habitId
    AND LocalDate >= @from
    AND LocalDate <= @to
  ORDER BY LocalDate ASC
  ```
  - Wykorzystuje index: `IX_Checkins_HabitId_LocalDate` z INCLUDE covering index
  - AsNoTracking() dla read-only query

## 6. Względy bezpieczeństwa

### Autoryzacja i uwierzytelnianie
- **JWT Bearer Token:** Wymagany przez `[Authorize]` / `.RequireAuthorization()`
- **UserId Extraction:** Pobranie z `ClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)`
- **Ownership Verification:** Handler weryfikuje czy `Habit.UserId == aktualny UserId`

### Zabezpieczenie przed atakami

**IDOR (Insecure Direct Object Reference):**
- **Zagrożenie:** Użytkownik może manipulować `habitId` aby uzyskać dostęp do cudzych check-inów
- **Mitygacja:**
  - Weryfikacja w handlerze: `WHERE Habit.Id = habitId AND Habit.UserId = userId`
  - Zwracanie 404 zamiast 403 dla nieistniejących/cudzych zasobów (nie ujawniamy istnienia zasobu)

**SQL Injection:**
- **Mitygacja:** EF Core używa parametryzowanych zapytań automatycznie

**Excessive Data Exposure:**
- **Zagrożenie:** Zbyt szeroki zakres dat może spowodować przeciążenie serwera/klienta
- **Mitygacja:**
  - Validator ogranicza maksymalny zakres dat do 365 dni
  - Potencjalnie paginacja w przyszłości przy większych zbiorach danych

### Walidacja danych wejściowych

**FluentValidation rules:**
```csharp
RuleFor(x => x.HabitId)
    .GreaterThan(0)
    .WithMessage("Habit ID must be greater than 0");

RuleFor(x => x.From)
    .LessThanOrEqualTo(x => x.To)
    .WithMessage("Date 'from' cannot be after 'to'");

RuleFor(x => x)
    .Must(x => (x.To.DayNumber - x.From.DayNumber) <= 365)
    .WithMessage("Date range cannot exceed 365 days");
```

## 7. Obsługa błędów

### Katalog błędów

| Kod | Scenariusz | Response | Error Code (opcjonalnie) |
|-----|-----------|----------|--------------------------|
| 400 | Nieprawidłowy format daty | Bad Request + validation errors | `InvalidDateFormat` |
| 400 | from > to | Bad Request + validation errors | `InvalidDateRange` |
| 400 | Zakres dat > 365 dni | Bad Request + validation errors | `DateRangeExceeded` |
| 400 | habitId <= 0 | Bad Request + validation errors | `InvalidHabitId` |
| 401 | Brak/nieprawidłowy JWT token | Unauthorized | - |
| 404 | Nawyk nie istnieje | Not Found + detail message | `HabitNotFound` |
| 404 | Nawyk należy do innego użytkownika | Not Found + detail message | `HabitNotFound` |
| 500 | Błąd bazy danych | Internal Server Error | `DatabaseError` |

### Implementacja w handlerze

**Pattern Result<T>:**
```csharp
// Weryfikacja habit ownership
var habit = await _context.Habits
    .AsNoTracking()
    .FirstOrDefaultAsync(h => h.Id == query.HabitId && h.UserId == query.UserId);

if (habit is null)
{
    return Result<CheckinListResponse>.Failure(
        "HabitNotFound",
        $"Habit with ID {query.HabitId} was not found"
    );
}
```

**Mapping w endpoincie:**
```csharp
app.MapGet("/api/v1/habits/{habitId}/checkins", async (...) =>
{
    var result = await mediator.Send(query);

    return result.IsSuccess
        ? Results.Ok(result.Value)
        : result.ErrorCode switch
        {
            "HabitNotFound" => Results.NotFound(new {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                title = "Habit not found",
                status = 404,
                detail = result.Error
            }),
            _ => Results.Problem(...)
        };
})
.RequireAuthorization();
```

### Logowanie

**Poziomy logowania:**
- **Information:** Udane wywołanie endpointu
- **Warning:** Próba dostępu do cudzego nawyku (potencjalny atak)
- **Error:** Błędy bazy danych, nieoczekiwane wyjątki

## 8. Rozważania dotyczące wydajności

### Optymalizacje zapytań

**Wykorzystanie indeksów:**
- Query używa covering index: `IX_Checkins_HabitId_LocalDate` INCLUDE (ActualValue, TargetValueSnapshot, CompletionModeSnapshot, HabitTypeSnapshot, IsPlanned)
- Wszystkie potrzebne kolumny są w INCLUDE → brak dodatkowego lookupu

**EF Core optimizations:**
```csharp
var checkins = await _context.Checkins
    .AsNoTracking() // Read-only, brak change trackingu
    .Where(c => c.HabitId == query.HabitId
             && c.LocalDate >= query.From
             && c.LocalDate <= query.To)
    .OrderBy(c => c.LocalDate)
    .Select(c => new CheckinItemDto( // Projekcja bezpośrednio na DTO
        c.Id,
        c.LocalDate.ToString("yyyy-MM-dd"),
        c.ActualValue,
        c.TargetValueSnapshot,
        c.CompletionModeSnapshot,
        c.HabitTypeSnapshot,
        c.IsPlanned
    ))
    .ToListAsync();
```

### Limity i zabezpieczenia

**Maksymalny zakres dat:** 365 dni
- Typowy nawyk dzienny = ~365 rekordów/rok = akceptowalna wielkość response
- Dla większych zakresów w przyszłości: rozważyć paginację

**Potencjalne wąskie gardła:**
- Zbyt duże response przy długim okresie (mitygacja: limit 365 dni)
- Brak cachingu (rozważyć w przyszłości dla często odpytywanych zakresów)

### Monitoring

**Metryki do śledzenia:**
- Czas wykonania zapytania do bazy
- Rozmiar response
- Liczba zwracanych check-inów
- Częstotliwość błędów 404 (potencjalne ataki IDOR)

## 9. Etapy wdrożenia

### Krok 1: Utworzenie struktury folderów i plików
```
Features/Checkins/GetCheckins/
├── GetCheckinsQuery.cs
├── GetCheckinsQueryValidator.cs
└── GetCheckinsQueryHandler.cs
```

### Krok 2: Implementacja Query i Validator

**GetCheckinsQuery.cs:**
```csharp
namespace HabitFlow.Api.Features.Checkins.GetCheckins;

public record GetCheckinsQuery(
    int HabitId,
    DateOnly From,
    DateOnly To,
    string UserId
) : IRequest<Result<CheckinListResponse>>;
```

**GetCheckinsQueryValidator.cs:**
```csharp
public class GetCheckinsQueryValidator : AbstractValidator<GetCheckinsQuery>
{
    public GetCheckinsQueryValidator()
    {
        RuleFor(x => x.HabitId)
            .GreaterThan(0)
            .WithMessage("Habit ID must be greater than 0");

        RuleFor(x => x.From)
            .LessThanOrEqualTo(x => x.To)
            .WithMessage("Date 'from' cannot be after 'to'");

        RuleFor(x => x)
            .Must(x => (x.To.DayNumber - x.From.DayNumber) <= 365)
            .WithMessage("Date range cannot exceed 365 days");
    }
}
```

### Krok 3: Implementacja Handlera

**GetCheckinsQueryHandler.cs:**
```csharp
public class GetCheckinsQueryHandler : IRequestHandler<GetCheckinsQuery, Result<CheckinListResponse>>
{
    private readonly AppDbContext _context;
    private readonly ILogger<GetCheckinsQueryHandler> _logger;

    public GetCheckinsQueryHandler(
        AppDbContext context,
        ILogger<GetCheckinsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<CheckinListResponse>> Handle(
        GetCheckinsQuery query,
        CancellationToken cancellationToken)
    {
        // 1. Weryfikacja ownership nawyku
        var habitExists = await _context.Habits
            .AsNoTracking()
            .AnyAsync(h => h.Id == query.HabitId && h.UserId == query.UserId,
                     cancellationToken);

        if (!habitExists)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access habit {HabitId} that doesn't exist or doesn't belong to them",
                query.UserId, query.HabitId);

            return Result<CheckinListResponse>.Failure(
                "HabitNotFound",
                $"Habit with ID {query.HabitId} was not found");
        }

        // 2. Pobranie check-inów
        var checkins = await _context.Checkins
            .AsNoTracking()
            .Where(c => c.HabitId == query.HabitId
                     && c.LocalDate >= query.From
                     && c.LocalDate <= query.To)
            .OrderBy(c => c.LocalDate)
            .Select(c => new CheckinItemDto(
                c.Id,
                c.LocalDate.ToString("yyyy-MM-dd"),
                c.ActualValue,
                c.TargetValueSnapshot,
                c.CompletionModeSnapshot,
                c.HabitTypeSnapshot,
                c.IsPlanned
            ))
            .ToListAsync(cancellationToken);

        // 3. Utworzenie response
        var response = new CheckinListResponse(
            query.HabitId,
            query.From.ToString("yyyy-MM-dd"),
            query.To.ToString("yyyy-MM-dd"),
            checkins
        );

        _logger.LogInformation(
            "Retrieved {Count} checkins for habit {HabitId} from {From} to {To}",
            checkins.Count, query.HabitId, query.From, query.To);

        return Result<CheckinListResponse>.Success(response);
    }
}
```

### Krok 4: Rejestracja w CheckinEndpoints.cs

**Dodanie endpointu w `CheckinEndpoints.cs`:**
```csharp
group.MapGet("/{habitId}/checkins", GetCheckinsPerHabit)
    .WithName("GetCheckinsPerHabit")
    .WithDescription("Get checkins for a habit within a date range")
    .Produces<CheckinListResponse>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .RequireAuthorization();

static async Task<IResult> GetCheckinsPerHabit(
    int habitId,
    [FromQuery] string from,
    [FromQuery] string to,
    ClaimsPrincipal user,
    ISender sender,
    CancellationToken cancellationToken)
{
    // Parsowanie dat
    if (!DateOnly.TryParseExact(from, "yyyy-MM-dd", out var fromDate))
    {
        return Results.BadRequest(new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            title = "Invalid date format",
            status = 400,
            errors = new Dictionary<string, string[]>
            {
                ["from"] = new[] { "Date must be in format YYYY-MM-DD" }
            }
        });
    }

    if (!DateOnly.TryParseExact(to, "yyyy-MM-dd", out var toDate))
    {
        return Results.BadRequest(new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            title = "Invalid date format",
            status = 400,
            errors = new Dictionary<string, string[]>
            {
                ["to"] = new[] { "Date must be in format YYYY-MM-DD" }
            }
        });
    }

    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var query = new GetCheckinsQuery(habitId, fromDate, toDate, userId);

    var result = await sender.Send(query, cancellationToken);

    return result.IsSuccess
        ? Results.Ok(result.Value)
        : result.ErrorCode switch
        {
            "HabitNotFound" => Results.NotFound(new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                title = "Habit not found",
                status = 404,
                detail = result.Error
            }),
            _ => Results.Problem(
                title: "An error occurred",
                statusCode: 500,
                detail: result.Error
            )
        };
}
```

### Krok 5: Testy jednostkowe

**Utworzenie `GetCheckinsQueryHandlerTests.cs` w projekcie testowym:**

Scenariusze testowe:
- ✅ Sukces: Zwraca check-iny dla właściciela nawyku
- ✅ Sukces: Zwraca pustą listę gdy brak check-inów w zakresie
- ❌ Błąd: 404 gdy nawyk nie istnieje
- ❌ Błąd: 404 gdy nawyk należy do innego użytkownika
- ✅ Sukces: Sortowanie po LocalDate ASC
- ✅ Sukces: Poprawne mapowanie snapshotów

**Utworzenie `GetCheckinsQueryValidatorTests.cs`:**
- ❌ Błąd walidacji: habitId <= 0
- ❌ Błąd walidacji: from > to
- ❌ Błąd walidacji: zakres > 365 dni
- ✅ Sukces walidacji: poprawne parametry

### Krok 6: Testy integracyjne

**Utworzenie `GetCheckinsEndpointTests.cs` w IntegrationTests:**

Scenariusze:
- ✅ 200 OK: Zwraca check-iny dla poprawnego requestu
- ❌ 400 Bad Request: Nieprawidłowy format daty
- ❌ 400 Bad Request: from > to
- ❌ 400 Bad Request: zakres > 365 dni
- ❌ 401 Unauthorized: Brak tokena JWT
- ❌ 404 Not Found: Nieistniejący nawyk
- ❌ 404 Not Found: Cudzy nawyk (security test)

Setup testów:
```csharp
// Arrange
var user = await CreateTestUser();
var habit = await CreateTestHabit(user.Id);
await CreateTestCheckins(habit.Id, new DateOnly(2025, 11, 1), 30);

// Act
var response = await _client.GetAsync(
    $"/api/v1/habits/{habit.Id}/checkins?from=2025-11-01&to=2025-11-30",
    authenticatedUser: user);

// Assert
response.StatusCode.Should().Be(HttpStatusCode.OK);
var result = await response.Content.ReadFromJsonAsync<CheckinListResponse>();
result.Items.Should().HaveCount(30);
```

### Krok 7: Dokumentacja OpenAPI

Endpoint automatycznie generuje dokumentację OpenAPI w trybie Development.

**Weryfikacja:**
- Uruchom `dotnet run --project HabitFlow.Api`
- Otwórz `https://localhost:7001/swagger`
- Sprawdź czy endpoint jest widoczny z pełną dokumentacją

### Krok 8: Weryfikacja manualna

**Checklist przed merge:**
- [ ] Endpoint zwraca 200 OK dla poprawnego requestu
- [ ] Endpoint zwraca 400 dla błędnych parametrów dat
- [ ] Endpoint zwraca 401 bez tokena JWT
- [ ] Endpoint zwraca 404 dla nieistniejącego nawyku
- [ ] Endpoint zwraca 404 przy próbie dostępu do cudzego nawyku
- [ ] Sortowanie check-inów po LocalDate ASC
- [ ] Wszystkie testy jednostkowe przechodzą
- [ ] Wszystkie testy integracyjne przechodzą
- [ ] `dotnet format` wykonane
- [ ] OpenAPI dokumentacja wygenerowana poprawnie

### Krok 9: Commit i PR

**Commit message:**
```
feat(api): implement GET /api/v1/habits/{habitId}/checkins endpoint

- Add GetCheckinsQuery, Validator, and Handler
- Implement ownership verification for security
- Add date range validation (max 365 days)
- Include unit and integration tests
- Optimize query with covering index
```

**PR Description:**
```markdown
## Summary
Implements endpoint to retrieve checkins for a habit within a date range.

## Changes
- ✅ GetCheckinsQuery with FluentValidation
- ✅ GetCheckinsQueryHandler with ownership check
- ✅ Endpoint mapping in CheckinEndpoints
- ✅ Unit tests for handler and validator
- ✅ Integration tests for all scenarios

## Security
- Verified habit ownership before returning data
- Return 404 instead of 403 to avoid resource enumeration
- Limited date range to 365 days max

## Testing
- [x] Unit tests pass
- [x] Integration tests pass
- [x] Manual testing completed
- [x] OpenAPI docs verified

Closes #[issue-number]
```

---

## Podsumowanie

Plan implementacji obejmuje:
1. ✅ Struktura CQRS (Query/Validator/Handler)
2. ✅ Security (ownership verification, IDOR prevention)
3. ✅ Performance (covering index, AsNoTracking, date range limit)
4. ✅ Validation (FluentValidation z jasnym komunikatem błędów)
5. ✅ Error handling (Result pattern, odpowiednie kody HTTP)
6. ✅ Testy (unit + integration)
7. ✅ Dokumentacja (OpenAPI auto-generated)

**Szacowany czas implementacji:** 2-3 godziny
**Priorytet:** Wysoki (core functionality dla wyświetlania wykresów)
