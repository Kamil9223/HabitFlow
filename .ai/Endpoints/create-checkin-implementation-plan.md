# API Endpoint Implementation Plan: Create Habit Checkin

## 1. Przegląd punktu końcowego

Endpoint `POST /api/v1/habits/{habitId}/checkins` umożliwia użytkownikowi utworzenie dziennego check-inu dla określonego nawyku w konkretnej dacie lokalnej. Check-in rejestruje rzeczywistą wartość osiągniętą przez użytkownika (np. liczba przeczytanych stron, wykonanych treningów) wraz ze snapshotami konfiguracji nawyku z momentu utworzenia.

### Kluczowe ograniczenia biznesowe:
- Jeden check-in na nawyk na datę (unique constraint)
- Backfill ograniczony do maksymalnie 7 dni wstecz od bieżącej daty lokalnej użytkownika
- Brak możliwości edycji po utworzeniu (immutable)
- Check-in możliwy tylko dla dni zaplanowanych w nawykach (DaysOfWeekMask)
- Wartość actualValue jest automatycznie clampowana do TargetValueSnapshot

## 2. Szczegóły żądania

### Metoda HTTP
`POST`

### Struktura URL
```
POST /api/v1/habits/{habitId}/checkins
```

### Parametry

#### Route Parameters (wymagane):
- `habitId` (int) - Identyfikator nawyku, dla którego tworzony jest check-in

#### Request Body (JSON):
```json
{
  "localDate": "2025-12-07",
  "actualValue": 7
}
```

**Pola:**
- `localDate` (string, wymagane) - Data lokalna w formacie ISO 8601 (YYYY-MM-DD)
- `actualValue` (int, wymagane) - Rzeczywista wartość osiągnięta w danym dniu (≥ 0)

### Headers:
- `Authorization: Bearer {jwt_token}` - Wymagane do uwierzytelnienia użytkownika
- `Content-Type: application/json`

## 3. Wykorzystywane typy

### DTOs (już istniejące):
- **`CreateCheckinRequest`** (`HabitFlow.Api/Contracts/Checkins/CreateCheckinRequest.cs`)
  - `LocalDate` (string)
  - `ActualValue` (int)

- **`CheckinResponse`** (`HabitFlow.Api/Contracts/Checkins/CheckinResponse.cs`)
  - `Id` (long)
  - `HabitId` (int)
  - `UserId` (string)
  - `LocalDate` (DateOnly lub string)
  - `ActualValue` (int)
  - `TargetValueSnapshot` (short)
  - `CompletionModeSnapshot` (byte)
  - `HabitTypeSnapshot` (byte)
  - `IsPlanned` (bool)
  - `CreatedAtUtc` (DateTime)

### Command Model (do utworzenia):
- **`CreateCheckinCommand`** (`HabitFlow.Api/Features/Checkins/Commands/CreateCheckinCommand.cs`)
  ```csharp
  public record CreateCheckinCommand(
      int HabitId,
      string UserId,
      DateOnly LocalDate,
      int ActualValue
  ) : IRequest<Result<CheckinResponse>>;
  ```

### Handler (do utworzenia):
- **`CreateCheckinCommandHandler`** (`HabitFlow.Api/Features/Checkins/Commands/CreateCheckinCommandHandler.cs`)
  - Implementuje `IRequestHandler<CreateCheckinCommand, Result<CheckinResponse>>`

### Validator (do utworzenia):
- **`CreateCheckinCommandValidator`** (`HabitFlow.Api/Features/Checkins/Commands/CreateCheckinCommandValidator.cs`)
  - Rozszerza `AbstractValidator<CreateCheckinCommand>`

### Entity (istniejący):
- **`Checkin`** (entity model w `HabitFlow.Api/Data/Entities/`)
  - Mapowanie 1:1 ze strukturą tabeli `Checkins`

## 4. Szczegóły odpowiedzi

### Success Response - 201 Created

```json
{
  "id": 9876,
  "habitId": 101,
  "userId": "auth0|abc123",
  "localDate": "2025-12-07",
  "actualValue": 7,
  "targetValueSnapshot": 10,
  "completionModeSnapshot": 2,
  "habitTypeSnapshot": 1,
  "isPlanned": true,
  "createdAtUtc": "2025-12-07T22:01:00Z"
}
```

**Headers:**
- `Location: /api/v1/habits/101/checkins/9876` (opcjonalnie)
- `Content-Type: application/json`

### Error Responses

| Kod | Scenariusz | Problem Details |
|-----|------------|-----------------|
| 400 | Nieprawidłowy format danych | `{ "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1", "title": "Bad Request", "status": 400, "errors": { "LocalDate": ["Invalid date format"], "ActualValue": ["Must be >= 0"] } }` |
| 401 | Brak/nieprawidłowy token JWT | `{ "type": "https://tools.ietf.org/html/rfc7235#section-3.1", "title": "Unauthorized", "status": 401 }` |
| 403 | Użytkownik nie jest właścicielem nawyku | `{ "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3", "title": "Forbidden", "status": 403, "detail": "You do not have permission to create checkins for this habit" }` |
| 404 | Nawyk nie istnieje | `{ "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4", "title": "Not Found", "status": 404, "detail": "Habit with ID 101 not found" }` |
| 409 | Duplikat check-inu dla daty | `{ "type": "https://tools.ietf.org/html/rfc7231#section-6.5.8", "title": "Conflict", "status": 409, "detail": "A checkin for this date already exists" }` |
| 422 | Naruszenie reguł biznesowych | `{ "type": "https://tools.ietf.org/html/rfc4918#section-11.2", "title": "Unprocessable Entity", "status": 422, "detail": "Checkin date is more than 7 days in the past" }` |

#### Szczegółowe przypadki 422:
- Data > 7 dni wstecz: `"detail": "Checkin date must be within the last 7 days"`
- Dzień nie zaplanowany: `"detail": "Checkin is not allowed for this day (not in planned days)"`
- Po deadline: `"detail": "Checkin deadline has passed for this date"`

## 5. Przepływ danych

### Diagram przepływu:

```
1. HTTP Request → Endpoint
   ↓
2. CreateCheckinRequest (DTO) → Walidacja FluentValidation
   ↓
3. Mapowanie → CreateCheckinCommand
   ↓
4. MediatR Pipeline → CreateCheckinCommandHandler
   ↓
5. Handler Logic:
   a. Parsowanie localDate (string → DateOnly)
   b. Pobranie Habit z EF Core (Include userId)
   c. Sprawdzenie właściciela (UserId)
   d. Walidacja biznesowa:
      - Sprawdzenie duplikatu (HabitId + LocalDate)
      - Walidacja zakresu dat (max 7 dni wstecz, nie przyszłość)
      - Sprawdzenie IsPlanned (DaysOfWeekMask & dzień tygodnia)
      - Walidacja deadline (jeśli zaimplementowane)
   e. Utworzenie snapshots:
      - TargetValueSnapshot ← Habit.TargetValue
      - CompletionModeSnapshot ← Habit.CompletionMode
      - HabitTypeSnapshot ← Habit.Type
   f. Clampowanie actualValue:
      - actualValue = Math.Min(actualValue, TargetValueSnapshot)
   g. Utworzenie entity Checkin
   h. Zapis do bazy danych (SaveChangesAsync)
   ↓
6. Mapowanie Checkin → CheckinResponse
   ↓
7. Result<CheckinResponse> → HTTP 201
```

### Interakcje z bazą danych:

1. **Query 1: Pobranie Habit**
   ```sql
   SELECT Id, UserId, TargetValue, CompletionMode, Type, DaysOfWeekMask
   FROM Habits
   WHERE Id = @habitId
   ```

2. **Query 2: Sprawdzenie duplikatu** (opcjonalnie, jeśli nie polegamy tylko na unique constraint)
   ```sql
   SELECT COUNT(1)
   FROM Checkins
   WHERE HabitId = @habitId AND LocalDate = @localDate
   ```

3. **Insert: Utworzenie Checkin**
   ```sql
   INSERT INTO Checkins (HabitId, UserId, LocalDate, ActualValue,
                        TargetValueSnapshot, CompletionModeSnapshot,
                        HabitTypeSnapshot, IsPlanned, CreatedAtUtc)
   OUTPUT INSERTED.*
   VALUES (@habitId, @userId, @localDate, @actualValue,
           @targetValueSnapshot, @completionModeSnapshot,
           @habitTypeSnapshot, @isPlanned, GETUTCDATE())
   ```

### Wykorzystanie indeksów:
- `IX_Checkins_HabitId_LocalDate` - dla sprawdzenia duplikatu
- `UQ_Checkins_HabitId_LocalDate` - constraint na poziomie bazy
- `IX_Checkins_UserId_LocalDate_HabitId` - clustered index dla wydajności RLS

## 6. Względy bezpieczeństwa

### Uwierzytelnianie
- **Mechanizm**: JWT Bearer Token
- **Implementacja**: `[Authorize]` attribute lub `.RequireAuthorization()` w Minimal API
- **Claims**: UserId z tokenu (`ClaimTypes.NameIdentifier` lub custom claim)

### Autoryzacja
- **Sprawdzenie własności nawyku**:
  ```csharp
  var habit = await dbContext.Habits
      .FirstOrDefaultAsync(h => h.Id == habitId, cancellationToken);

  if (habit == null) return Result.NotFound();
  if (habit.UserId != command.UserId) return Result.Forbidden();
  ```

### Walidacja danych wejściowych
1. **FluentValidation na poziomie Command:**
   - Format daty (regex lub TryParse)
   - Zakres actualValue (>= 0)
   - HabitId > 0

2. **Walidacja biznesowa w Handler:**
   - Sprawdzenie zakresu dat (nie przyszłość, max 7 dni wstecz)
   - Clampowanie actualValue do TargetValueSnapshot
   - Sprawdzenie IsPlanned

### Zapobieganie atakom

| Atak | Zabezpieczenie |
|------|----------------|
| SQL Injection | EF Core z parametryzowanymi queries |
| Mass Assignment | DTO ogranicza mapowane pola |
| IDOR (Insecure Direct Object Reference) | Walidacja UserId przed operacją |
| Date Manipulation | Walidacja zakresu dat + timezone awareness |
| Integer Overflow | Clampowanie + typy smallint/int w bazie |
| Rate Limiting | Middleware (opcjonalnie dla backfill) |

### CORS i HTTPS
- Wymuszenie HTTPS w produkcji
- Konfiguracja CORS dla Blazor frontend

## 7. Obsługa błędów

### Strategia obsługi błędów:
Wykorzystanie **Result pattern** (FluentResults lub własna implementacja) zamiast rzucania wyjątków dla przewidywalnych błędów biznesowych.

### Tabela błędów:

| Typ błędu | Kod HTTP | Miejsce wykrycia | Obsługa |
|-----------|----------|------------------|---------|
| **Walidacja DTO** | 400 | FluentValidation | Automatyczne przez Behavior w MediatR pipeline |
| **Brak tokenu JWT** | 401 | Middleware ASP.NET | Automatyczne przez Authentication middleware |
| **Nawyk nie istnieje** | 404 | Handler | `return Result.NotFound("Habit with ID {habitId} not found")` |
| **Brak uprawnień** | 403 | Handler | `return Result.Forbidden("You do not have permission...")` |
| **Duplikat check-inu** | 409 | Handler / DB Constraint | Catch `DbUpdateException` → sprawdź constraint → 409 |
| **Data > 7 dni wstecz** | 422 | Handler | `return Result.UnprocessableEntity("Date must be within last 7 days")` |
| **Dzień nie zaplanowany** | 422 | Handler | `return Result.UnprocessableEntity("Checkin not allowed for this day")` |
| **Po deadline** | 422 | Handler | `return Result.UnprocessableEntity("Deadline has passed")` |
| **Błąd bazy danych** | 500 | Exception Handler | Global exception handler + ILogger |

### Implementacja w Endpoint:

```csharp
app.MapPost("/api/v1/habits/{habitId}/checkins",
    async (int habitId, CreateCheckinRequest request, ISender sender, ClaimsPrincipal user) =>
{
    var userId = user.GetUserId(); // Extension method
    var command = new CreateCheckinCommand(habitId, userId,
        DateOnly.Parse(request.LocalDate), request.ActualValue);

    var result = await sender.Send(command);

    return result.Match(
        onSuccess: checkin => Results.Created($"/api/v1/habits/{habitId}/checkins/{checkin.Id}", checkin),
        onNotFound: () => Results.NotFound(new ProblemDetails { ... }),
        onForbidden: () => Results.Forbid(),
        onConflict: () => Results.Conflict(new ProblemDetails { ... }),
        onUnprocessableEntity: msg => Results.UnprocessableEntity(new ProblemDetails { ... }),
        onFailure: error => Results.BadRequest(new ProblemDetails { ... })
    );
})
.RequireAuthorization()
.WithOpenApi();
```

### Logowanie:

```csharp
// W Handler:
_logger.LogInformation("Creating checkin for Habit {HabitId} on {LocalDate}",
    command.HabitId, command.LocalDate);

_logger.LogWarning("Duplicate checkin attempt for Habit {HabitId} on {LocalDate}",
    command.HabitId, command.LocalDate);

_logger.LogError(ex, "Database error while creating checkin for Habit {HabitId}",
    command.HabitId);
```

## 8. Rozważania dotyczące wydajności

### Potencjalne wąskie gardła:

1. **Query N+1 Problem**:
   - **Ryzyko**: Brak, jeden query dla Habit
   - **Mitygacja**: Nie dotyczy (single entity fetch)

2. **Database Round-trips**:
   - **Ryzyko**: 2-3 queries (Habit fetch + duplikat check + insert)
   - **Mitygacja**:
     - Unique constraint obsłuży duplikaty (catch exception)
     - Minimalizacja do 2 queries (fetch + insert)

3. **Index Coverage**:
   - **Optymalizacja**: Wykorzystanie `IX_Checkins_HabitId_LocalDate` z INCLUDE
   - **Query plan**: Index seek zamiast scan

4. **Clampowanie actualValue**:
   - **Performance**: O(1) - prosty Math.Min
   - **Bez wpływu**: Operacja w pamięci

5. **Snapshot tworzenie**:
   - **Performance**: O(1) - proste przypisanie wartości
   - **Bez wpływu**: Brak dodatkowych queries

### Strategie optymalizacji:

#### 1. Database:
```csharp
// Minimal query - tylko potrzebne kolumny
var habit = await dbContext.Habits
    .AsNoTracking() // Read-only
    .Select(h => new {
        h.Id,
        h.UserId,
        h.TargetValue,
        h.CompletionMode,
        h.Type,
        h.DaysOfWeekMask
    })
    .FirstOrDefaultAsync(h => h.Id == habitId, cancellationToken);
```

#### 2. Caching (opcjonalnie):
- **Distributed cache** dla Habit metadata (jeśli read-heavy)
- Cache key: `habit:{habitId}:metadata`
- Invalidacja: Przy update Habit

#### 3. Batch Operations:
- Jeśli użytkownik tworzy backfill (7 check-inów), rozważyć bulk insert:
  ```csharp
  await dbContext.Checkins.AddRangeAsync(checkins);
  await dbContext.SaveChangesAsync();
  ```

#### 4. Connection Pooling:
- Domyślnie włączone w EF Core
- Konfiguracja w connection string: `Max Pool Size=100`

### Monitoring i metryki:

- **Czas odpowiedzi**: Target < 200ms dla p95
- **Throughput**: ~100 requests/sec (pojedynczy instance)
- **Database metrics**:
  - Query duration < 50ms
  - Connection pool utilization < 80%

## 9. Etapy wdrożenia

### Krok 1: Przygotowanie struktury plików
```
HabitFlow.Api/
├── Features/
│   └── Checkins/
│       ├── Commands/
│       │   ├── CreateCheckinCommand.cs
│       │   ├── CreateCheckinCommandHandler.cs
│       │   └── CreateCheckinCommandValidator.cs
│       └── Mappings/
│           └── CheckinMappings.cs (opcjonalnie dla AutoMapper)
```

### Krok 2: Implementacja Command Model
```csharp
// CreateCheckinCommand.cs
namespace HabitFlow.Api.Features.Checkins.Commands;

public record CreateCheckinCommand(
    int HabitId,
    string UserId,
    DateOnly LocalDate,
    int ActualValue
) : IRequest<Result<CheckinResponse>>;
```

### Krok 3: Implementacja Validator
```csharp
// CreateCheckinCommandValidator.cs
public class CreateCheckinCommandValidator : AbstractValidator<CreateCheckinCommand>
{
    public CreateCheckinCommandValidator()
    {
        RuleFor(x => x.HabitId)
            .GreaterThan(0).WithMessage("HabitId must be greater than 0");

        RuleFor(x => x.ActualValue)
            .GreaterThanOrEqualTo(0).WithMessage("ActualValue must be non-negative");

        RuleFor(x => x.LocalDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("LocalDate cannot be in the future")
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)))
                .WithMessage("LocalDate cannot be more than 7 days in the past");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");
    }
}
```

### Krok 4: Implementacja Handler - Część 1 (Setup)
```csharp
// CreateCheckinCommandHandler.cs
public class CreateCheckinCommandHandler
    : IRequestHandler<CreateCheckinCommand, Result<CheckinResponse>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<CreateCheckinCommandHandler> _logger;

    public CreateCheckinCommandHandler(
        ApplicationDbContext dbContext,
        ILogger<CreateCheckinCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<CheckinResponse>> Handle(
        CreateCheckinCommand command,
        CancellationToken cancellationToken)
    {
        // Implementacja w kolejnych krokach
    }
}
```

### Krok 5: Implementacja Handler - Część 2 (Walidacja istnienia i własności)
```csharp
// W metodzie Handle:

// 1. Pobranie nawyku
var habit = await _dbContext.Habits
    .AsNoTracking()
    .FirstOrDefaultAsync(h => h.Id == command.HabitId, cancellationToken);

if (habit == null)
{
    _logger.LogWarning("Habit {HabitId} not found", command.HabitId);
    return Result.NotFound($"Habit with ID {command.HabitId} not found");
}

// 2. Sprawdzenie własności
if (habit.UserId != command.UserId)
{
    _logger.LogWarning("User {UserId} attempted to create checkin for Habit {HabitId} owned by {OwnerId}",
        command.UserId, command.HabitId, habit.UserId);
    return Result.Forbidden("You do not have permission to create checkins for this habit");
}
```

### Krok 6: Implementacja Handler - Część 3 (Walidacja biznesowa)
```csharp
// 3. Sprawdzenie czy dzień jest zaplanowany
var dayOfWeek = (int)command.LocalDate.DayOfWeek; // 0 = Sunday
var dayMask = 1 << dayOfWeek;
var isPlanned = (habit.DaysOfWeekMask & dayMask) != 0;

if (!isPlanned)
{
    _logger.LogWarning("Checkin for Habit {HabitId} on {LocalDate} is not allowed (not a planned day)",
        command.HabitId, command.LocalDate);
    return Result.UnprocessableEntity("Checkin is not allowed for this day (not in planned days)");
}

// 4. Walidacja deadline (jeśli zaimplementowane w Habit)
// if (habit.CheckinDeadline.HasValue && DateTime.UtcNow > CalculateDeadline(command.LocalDate, habit))
// {
//     return Result.UnprocessableEntity("Checkin deadline has passed for this date");
// }
```

### Krok 7: Implementacja Handler - Część 4 (Utworzenie snapshots i clampowanie)
```csharp
// 5. Utworzenie snapshots
var targetValueSnapshot = habit.TargetValue;
var completionModeSnapshot = habit.CompletionMode;
var habitTypeSnapshot = habit.Type;

// 6. Clampowanie actualValue
var clampedActualValue = Math.Min(command.ActualValue, targetValueSnapshot);

if (clampedActualValue != command.ActualValue)
{
    _logger.LogInformation("ActualValue {Original} clamped to {Clamped} for Habit {HabitId}",
        command.ActualValue, clampedActualValue, command.HabitId);
}
```

### Krok 8: Implementacja Handler - Część 5 (Zapis do bazy)
```csharp
// 7. Utworzenie entity
var checkin = new Checkin
{
    HabitId = command.HabitId,
    UserId = command.UserId,
    LocalDate = command.LocalDate,
    ActualValue = clampedActualValue,
    TargetValueSnapshot = targetValueSnapshot,
    CompletionModeSnapshot = completionModeSnapshot,
    HabitTypeSnapshot = habitTypeSnapshot,
    IsPlanned = isPlanned,
    CreatedAtUtc = DateTime.UtcNow
};

try
{
    _dbContext.Checkins.Add(checkin);
    await _dbContext.SaveChangesAsync(cancellationToken);

    _logger.LogInformation("Checkin {CheckinId} created for Habit {HabitId} on {LocalDate}",
        checkin.Id, command.HabitId, command.LocalDate);
}
catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_Checkins_HabitId_LocalDate") == true)
{
    _logger.LogWarning("Duplicate checkin attempt for Habit {HabitId} on {LocalDate}",
        command.HabitId, command.LocalDate);
    return Result.Conflict("A checkin for this date already exists");
}
```

### Krok 9: Implementacja Handler - Część 6 (Mapowanie response)
```csharp
// 8. Mapowanie do response
var response = new CheckinResponse
{
    Id = checkin.Id,
    HabitId = checkin.HabitId,
    UserId = checkin.UserId,
    LocalDate = checkin.LocalDate,
    ActualValue = checkin.ActualValue,
    TargetValueSnapshot = checkin.TargetValueSnapshot,
    CompletionModeSnapshot = checkin.CompletionModeSnapshot,
    HabitTypeSnapshot = checkin.HabitTypeSnapshot,
    IsPlanned = checkin.IsPlanned,
    CreatedAtUtc = checkin.CreatedAtUtc
};

return Result.Success(response);
```

### Krok 10: Implementacja Endpoint
```csharp
// W CheckinEndpoints.cs
public static class CheckinEndpoints
{
    public static IEndpointRouteBuilder MapCheckinEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/habits/{habitId}/checkins")
            .WithTags("Checkins")
            .RequireAuthorization();

        group.MapPost("", CreateCheckin)
            .Produces<CheckinResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .WithOpenApi();

        return app;
    }

    private static async Task<IResult> CreateCheckin(
        int habitId,
        CreateCheckinRequest request,
        ISender sender,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        // 1. Wyciągnięcie userId z claims
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in claims");

        // 2. Parsowanie localDate
        if (!DateOnly.TryParseExact(request.LocalDate, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var localDate))
        {
            return Results.BadRequest(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Invalid date format. Expected YYYY-MM-DD"
            });
        }

        // 3. Utworzenie command
        var command = new CreateCheckinCommand(habitId, userId, localDate, request.ActualValue);

        // 4. Wysłanie przez MediatR
        var result = await sender.Send(command, cancellationToken);

        // 5. Mapowanie Result na IResult
        return result.Match(
            onSuccess: checkin => Results.Created(
                $"/api/v1/habits/{habitId}/checkins/{checkin.Id}",
                checkin),
            onNotFound: () => Results.NotFound(new ProblemDetails { /* ... */ }),
            onForbidden: () => Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                detail: "You do not have permission to create checkins for this habit"),
            onConflict: () => Results.Conflict(new ProblemDetails { /* ... */ }),
            onUnprocessableEntity: msg => Results.UnprocessableEntity(new ProblemDetails { /* ... */ }),
            onFailure: error => Results.BadRequest(new ProblemDetails { /* ... */ })
        );
    }
}
```

### Krok 11: Rejestracja w Program.cs
```csharp
// W Program.cs (w sekcji z endpoint mappings)
app.MapCheckinEndpoints();
```

### Krok 12: Testy jednostkowe - Handler
```csharp
// HabitFlow.Tests/UnitTests/Features/Checkins/CreateCheckinCommandHandlerTests.cs
public class CreateCheckinCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessResult()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var handler = new CreateCheckinCommandHandler(dbContext, NullLogger<>.Instance);

        var habit = new Habit { /* ... */ };
        dbContext.Habits.Add(habit);
        await dbContext.SaveChangesAsync();

        var command = new CreateCheckinCommand(habit.Id, habit.UserId, DateOnly.Today, 5);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ActualValue.Should().Be(5);
    }

    [Fact]
    public async Task Handle_HabitNotFound_ReturnsNotFoundResult()
    {
        // Arrange & Act & Assert
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbiddenResult()
    {
        // Arrange & Act & Assert
    }

    [Fact]
    public async Task Handle_DuplicateCheckin_ReturnsConflictResult()
    {
        // Arrange & Act & Assert
    }

    [Fact]
    public async Task Handle_NotPlannedDay_ReturnsUnprocessableEntityResult()
    {
        // Arrange & Act & Assert
    }

    [Fact]
    public async Task Handle_ActualValueExceedsTarget_ClampsValue()
    {
        // Arrange & Act & Assert
    }
}
```

### Krok 13: Testy integracyjne - Endpoint
```csharp
// HabitFlow.Tests/IntegrationTests/Endpoints/CheckinEndpointsTests.cs
public class CheckinEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    [Fact]
    public async Task CreateCheckin_ValidRequest_Returns201()
    {
        // Arrange
        var habitId = await CreateTestHabitAsync();
        var request = new CreateCheckinRequest("2025-12-07", 5);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/habits/{habitId}/checkins", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var checkin = await response.Content.ReadFromJsonAsync<CheckinResponse>();
        checkin.Should().NotBeNull();
        checkin!.ActualValue.Should().Be(5);
    }

    [Fact]
    public async Task CreateCheckin_DuplicateDate_Returns409()
    {
        // Arrange & Act & Assert
    }

    [Fact]
    public async Task CreateCheckin_Unauthorized_Returns401()
    {
        // Arrange & Act & Assert
    }

    [Fact]
    public async Task CreateCheckin_NotOwner_Returns403()
    {
        // Arrange & Act & Assert
    }
}
```

### Krok 14: Dokumentacja OpenAPI
- Weryfikacja automatycznie wygenerowanej dokumentacji Swagger
- Dodanie przykładów w `WithOpenApi()`:
  ```csharp
  .WithOpenApi(operation =>
  {
      operation.Summary = "Create a daily checkin for a habit";
      operation.Description = "Creates a checkin entry for a specific date...";
      return operation;
  })
  ```

### Krok 15: Code Review Checklist
- [ ] Wszystkie kody HTTP poprawne (400, 401, 403, 404, 409, 422)
- [ ] Walidacja FluentValidation działa
- [ ] Sprawdzenie własności nawyku zaimplementowane
- [ ] Clampowanie actualValue działa poprawnie
- [ ] Obsługa duplikatów (unique constraint)
- [ ] IsPlanned calculowane poprawnie z DaysOfWeekMask
- [ ] Snapshots tworzone prawidłowo
- [ ] Logowanie na odpowiednich poziomach
- [ ] Testy jednostkowe pokrywają wszystkie scenariusze
- [ ] Testy integracyjne weryfikują pełny flow
- [ ] Dokumentacja OpenAPI kompletna

### Krok 16: Deployment
- Uruchomienie migracji (jeśli dodano nowe kolumny/tabele)
- Weryfikacja na środowisku staging
- Monitoring logów po wdrożeniu
- Sprawdzenie metryk wydajności

---

## Podsumowanie

Ten plan implementacji zapewnia kompleksowe wytyczne do stworzenia endpointu `POST /api/v1/habits/{habitId}/checkins` zgodnie ze specyfikacją API, regułami biznesowymi i najlepszymi praktykami architektury Clean Architecture z CQRS/MediatR. Kluczowe aspekty to:

- **Bezpieczeństwo**: JWT authentication + sprawdzenie własności zasobu
- **Walidacja**: Dwupoziomowa (FluentValidation + biznesowa w handler)
- **Immutability**: Brak endpoint'u do edycji check-inów
- **Snapshots**: Zachowanie stanu nawyku w momencie check-inu
- **Wydajność**: Optymalizacja queries, wykorzystanie indeksów
- **Testy**: Pełne pokrycie unit + integration tests
