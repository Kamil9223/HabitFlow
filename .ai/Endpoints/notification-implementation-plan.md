# API Endpoint Implementation Plan: GET /api/v1/notifications/{id}

## 1. Przegląd punktu końcowego

Endpoint służy do pobierania pojedynczego powiadomienia na podstawie jego identyfikatora. Powiadomienie musi należeć do aktualnie zalogowanego użytkownika. Jest to operacja tylko do odczytu (read-only) zgodnie z założeniami MVP, gdzie generowanie powiadomień odbywa się w tle przez background job.

## 2. Szczegóły żądania

- **Metoda HTTP**: GET
- **Struktura URL**: `/api/v1/notifications/{id}`
- **Parametry**:
  - **Wymagane**:
    - `id` (bigint, route parameter) — identyfikator powiadomienia
  - **Opcjonalne**: brak
- **Request Body**: brak (GET)
- **Autoryzacja**: wymagana (bearer token JWT)

## 3. Wykorzystywane typy

### Query Model i DTO
**Plik**: `HabitFlow.Core/Features/Notifications/GetNotificationByIdQuery.cs`

```csharp
namespace HabitFlow.Core.Features.Notifications;

/// <summary>
/// Query to retrieve a single notification by ID for the current user.
/// </summary>
public record GetNotificationByIdQuery(long Id) : IQuery<Result<NotificationDetailDto>>;

/// <summary>
/// Data transfer object for a single notification with habit details.
/// </summary>
public record NotificationDetailDto(
    long Id,
    int HabitId,
    string HabitName,
    DateOnly LocalDate,
    NotificationType Type,
    string Content,
    AiGenerationStatus? AiStatus,
    DateTime CreatedAtUtc
);
```

### Query Handler
**Plik**: `HabitFlow.Core/Features/Notifications/GetNotificationByIdQuery.cs` (ten sam plik)

```csharp
/// <summary>
/// Handler for retrieving a single notification by ID with ownership validation.
/// </summary>
public class GetNotificationByIdQueryHandler(
    HabitFlowDbContext context,
    ILoggedUserContext loggedUserContext)
    : IQueryHandler<GetNotificationByIdQuery, Result<NotificationDetailDto>>
{
    public async Task<Result<NotificationDetailDto>> Handle(
        GetNotificationByIdQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Id <= 0)
            return Result.NotFound<NotificationDetailDto>(
                "Notification not found",
                "NOTIFICATION_NOT_FOUND");

        var user = loggedUserContext.GetUser();

        var notification = await context.Notifications
            .AsNoTracking()
            .Where(n => n.Id == query.Id && n.UserId == user.UserId)
            .Select(n => new NotificationDetailDto(
                n.Id,
                n.HabitId,
                n.Habit.Name,
                n.LocalDate,
                n.Type,
                n.Content,
                n.AiStatus,
                n.CreatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

        return notification is not null
            ? Result.Success(notification)
            : Result.NotFound<NotificationDetailDto>(
                "Notification not found",
                "NOTIFICATION_NOT_FOUND");
    }
}
```

## 4. Szczegóły odpowiedzi

### Sukces (200 OK)
```json
{
  "id": 123,
  "habitId": 45,
  "habitName": "Morning Exercise",
  "localDate": "2026-01-20",
  "type": 1,
  "content": "You missed your habit 'Morning Exercise' yesterday. Don't let one miss become a pattern!",
  "aiStatus": 1,
  "createdAtUtc": "2026-01-20T06:00:00Z"
}
```

### Błędy

#### 401 Unauthorized
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Authentication required"
}
```

#### 404 Not Found
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Notification not found",
  "errorCode": "NOTIFICATION_NOT_FOUND"
}
```

#### 500 Internal Server Error
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred"
}
```

## 5. Przepływ danych

1. **Request → Endpoint**
   - Minimal API endpoint odbiera request z `id` z route parameter
   - Middleware autoryzacji weryfikuje token JWT i ekstrahuje `ClaimsPrincipal`
   - Endpoint nie musi ekstrahować `UserId` — jest to obsługiwane przez `ILoggedUserContext`

2. **Endpoint → Query Dispatcher**
   - Tworzy `GetNotificationByIdQuery(id)`
   - Wywołuje `IQueryDispatcher.Dispatch<Result<NotificationDetailDto>>(query, cancellationToken)`

3. **Query Handler → Database**
   - Handler pobiera `UserId` z `ILoggedUserContext.GetUser()`
   - Wykonuje zapytanie EF Core:
     ```csharp
     var notification = await context.Notifications
         .AsNoTracking()
         .Where(n => n.Id == query.Id && n.UserId == user.UserId)
         .Select(n => new NotificationDetailDto(
             n.Id,
             n.HabitId,
             n.Habit.Name,
             n.LocalDate,
             n.Type,
             n.Content,
             n.AiStatus,
             n.CreatedAtUtc))
         .SingleOrDefaultAsync(cancellationToken);
     ```
   - Wykorzystuje indeks `IX_Notifications_UserId_CreatedAtUtc` (INCLUDE pokrywa większość kolumn)
   - JOIN do `Habits` dla pobrania `n.Habit.Name`

4. **Handler → Result**
   - Jeśli `notification == null`: zwraca `Result.NotFound<NotificationDetailDto>("Notification not found", "NOTIFICATION_NOT_FOUND")`
   - Jeśli znaleziono: zwraca `Result.Success(notification)`

5. **Endpoint → Response**
   - Mapper konwertuje `Result<NotificationDetailDto>` na `IResult`:
     - `Success` → `Results.Ok(dto)`
     - `NotFound` → `Results.Problem(...)` z 404
   - Globalne middleware obsługują wyjątki → 500

## 6. Względy bezpieczeństwa

### Uwierzytelnienie
- Endpoint oznaczony `.RequireAuthorization()`
- Automatyczna weryfikacja tokenu JWT przez middleware ASP.NET Core
- Brak ważnego tokenu → 401 przed wywołaniem handlera

### Autoryzacja na poziomie zasobu
- Walidacja własności zasobu przez `WHERE UserId = @userId` w zapytaniu SQL
- **Ważne**: zwracamy 404, nie 403, gdy powiadomienie należy do innego użytkownika
  - Zapobiega to wyciekowi informacji o istnieniu zasobów
  - Atakujący nie wie, czy ID jest nieprawidłowe, czy należy do kogoś innego

### SQL Injection
- Parametryzowane zapytania EF Core (domyślnie)
- Brak surowego SQL w tym endpoincie

### Nadmierne pobieranie danych
- Projekcja `Select` do DTO pobiera tylko potrzebne kolumny
- Brak serializacji całego entity z wrażliwymi danymi

### Rate Limiting (opcjonalne do rozważenia)
- W przyszłości: limit zapytań per użytkownik
- Ochrona przed enumeration attacks na IDs

## 7. Obsługa błędów

| Scenariusz | Kod HTTP | Error Code | Handling |
|------------|----------|------------|----------|
| Brak tokenu JWT | 401 | - | Automatycznie przez middleware autoryzacji |
| Nieprawidłowy token | 401 | - | Automatycznie przez middleware autoryzacji |
| Id ≤ 0 | 404 | NOTIFICATION_NOT_FOUND | Handler waliduje, zwraca Result.NotFound |
| Powiadomienie nie istnieje | 404 | NOTIFICATION_NOT_FOUND | SingleOrDefaultAsync → null → Result.NotFound |
| Powiadomienie należy do innego użytkownika | 404 | NOTIFICATION_NOT_FOUND | WHERE UserId filtruje → null → Result.NotFound |
| Błąd bazy danych (connection, timeout) | 500 | - | Globalny exception handler → ProblemDetails |
| Nieoczekiwany wyjątek | 500 | - | Globalny exception handler → ProblemDetails |

### Strategia błędów
- **Oczekiwane błędy** (not found): `Result.NotFound` → 404 ProblemDetails
- **Nieoczekiwane błędy** (wyjątki): propagacja → globalny handler → 500
- **Konsystentny format**: wszystkie błędy jako RFC 7807 ProblemDetails

## 8. Rozważania dotyczące wydajności

### Optymalizacje zapytań
- **AsNoTracking**: brak trackingu zmian (read-only query)
- **Projekcja Select**: pobieranie tylko wymaganych kolumn zamiast całego entity
- **SingleOrDefaultAsync**: optymalizacja dla pojedynczego rekordu (vs FirstOrDefault)

### Wykorzystanie indeksów
Istniejący indeks: `IX_Notifications_UserId_CreatedAtUtc (UserId, CreatedAtUtc DESC) INCLUDE (Content, Type, HabitId, LocalDate)`

Zapytanie: `WHERE Id = @id AND UserId = @userId`
- Indeks pokrywa większość kolumn z SELECT (Content, Type, HabitId, LocalDate)
- **Uwaga**: indeks może nie być optymalny dla lookup po `(Id, UserId)` — klauzula `WHERE` filtruje najpierw po `Id` (PK), więc SQL Server użyje clustered index seek na PK
- Dodatkowy filtr `UserId` jest weryfikowany po PK lookup (szybkie, bo to 1 rekord)
- JOIN do `Habits` dla pobrania `Name` — zakładamy, że `Habits` ma PK index
- **Wniosek**: obecny indeks wystarczy; PK jest najbardziej efektywny dla lookup po Id

### Potencjalne wąskie gardła
- **Brak**: to bardzo prosty query (PK lookup + 1 JOIN do Habits)
- **Connection pooling**: domyślnie włączony w EF Core
- **Minimal overhead**: brak repozytoriów, bezpośrednie użycie DbContext

### Możliwe przyszłe optymalizacje
- **Compiled queries**: jeśli ten endpoint będzie bardzo gorący
- **Caching**: rozważyć cache w pamięci dla często odczytywanych powiadomień (z invalidacją)
- **Response compression**: gzip dla redukcji payload (middleware ASP.NET Core)

## 9. Etapy wdrożenia

### Krok 1: Utworzenie Query, DTO i Handler w jednym pliku
**Plik**: `HabitFlow.Core/Features/Notifications/GetNotificationByIdQuery.cs`

Plik powinien zawierać:
- `GetNotificationByIdQuery` record
- `NotificationDetailDto` record (z `HabitName`)
- `GetNotificationByIdQueryHandler` class

Patrz sekcja "3. Wykorzystywane typy" dla pełnego kodu.

**Uwaga**: Używamy `ILoggedUserContext` zamiast przekazywania `UserId` w Query (zgodnie z konwencją projektu).

### Krok 2: Rejestracja Handler w DI
**Plik**: `HabitFlow.Core` (sprawdź, gdzie są rejestrowane handlery)

Jeśli handlers są skanowane automatycznie (przez assembly scan), nic nie trzeba robić.

Jeśli rejestracja manualna w `Program.cs` lub `ServiceCollectionExtensions.cs`:
```csharp
services.AddScoped<IQueryHandler<GetNotificationByIdQuery, Result<NotificationDetailDto>>,
    GetNotificationByIdQueryHandler>();
```

### Krok 3: Dodanie Endpoint w NotificationEndpoints.cs
**Plik**: `HabitFlow.Api/Endpoints/NotificationEndpoints.cs`

Dodaj metodę mapowania:
```csharp
private static IEndpointRouteBuilder MapGetNotificationById(this IEndpointRouteBuilder builder)
{
    builder.MapGet("/{id:long}", async (
        long id,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken) =>
    {
        var query = new GetNotificationByIdQuery(id);
        var result = await queryDispatcher.Dispatch(query, cancellationToken);

        return result.ToHttpResult();
    })
    .RequireAuthorization()
    .WithName("GetNotificationById")
    .WithTags("Notifications")
    .Produces<NotificationDetailDto>(StatusCodes.Status200OK)
    .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
    .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
    .WithOpenApi(operation => new(operation)
    {
        Summary = "Get notification by ID",
        Description = "Retrieves a single notification owned by the current user"
    });

    return builder;
}
```

Wywołaj w `MapNotifications`:
```csharp
public static IEndpointRouteBuilder MapNotifications(this IEndpointRouteBuilder builder)
{
    var group = builder.MapGroup("/api/v1/notifications")
        .WithTags("Notifications");

    group.MapGetNotifications();
    group.MapGetNotificationById(); // nowa linia

    return builder;
}
```

### Krok 4: Dodanie DTO do Contracts (opcjonalne)
**Plik**: `HabitFlow.Api/Contracts/Notifications/NotificationDetailDto.cs`

Jeśli konwencja projektu wymaga duplikacji DTO w warstwie Contracts dla API, utwórz odpowiedni rekord.
Alternatywnie, możesz reużyć DTO z `HabitFlow.Core` (sprawdź, jak to jest robione w innych endpointach).

### Krok 5: Testy jednostkowe
**Plik**: `HabitFlow.Tests/UnitTests/Notifications/GetNotificationByIdQueryHandlerTests.cs`

Testy do utworzenia:
- `Handle_ValidIdAndOwner_ReturnsNotification` — happy path
- `Handle_InvalidId_ReturnsNotFound` — Id ≤ 0
- `Handle_NotificationNotExists_ReturnsNotFound` — ID nie istnieje w bazie
- `Handle_NotificationBelongsToOtherUser_ReturnsNotFound` — walidacja własności
- `Handle_IncludesHabitName_ReturnsCorrectData` — weryfikacja JOIN do Habits

### Krok 6: Testy integracyjne
**Plik**: `HabitFlow.Tests/IntegrationTests/Notifications/NotificationEndpointsTests.cs`

Dodaj testy do istniejącej klasy (lub utwórz nową):
- `GetNotificationById_Authenticated_ReturnsNotification` — 200 OK
- `GetNotificationById_NotFound_Returns404` — notification nie istnieje
- `GetNotificationById_OtherUsersNotification_Returns404` — próba dostępu do cudzego
- `GetNotificationById_Unauthenticated_Returns401` — brak tokenu
- `GetNotificationById_InvalidId_Returns404` — Id ≤ 0

### Krok 7: Weryfikacja i dokumentacja
1. Uruchom `dotnet test` — wszystkie testy zielone
2. Uruchom `dotnet run --project HabitFlow.Api`
3. Sprawdź Swagger UI (`/swagger`) — endpoint widoczny i opisany
4. Przetestuj manualnie z Postman/curl:
   ```bash
   curl -H "Authorization: Bearer <token>" \
        http://localhost:5000/api/v1/notifications/123
   ```
5. Zweryfikuj logi i metryki wydajności
6. Zaktualizuj dokumentację API (jeśli istnieje poza Swagger)

### Krok 8: Code review i merge
1. Uruchom `dotnet format` — formatowanie kodu
2. Commit zgodnie z Conventional Commits:
   ```
   feat(api): implement GET /api/v1/notifications/{id} endpoint
   ```
3. Utwórz PR z opisem, testami i linkiem do tego planu
4. Po review i aprobacie: merge do master

---

## Dodatkowe uwagi

### Różnice między NotificationDto a NotificationDetailDto
- **NotificationDto** (w `GetNotificationsQuery.cs`): używany dla listy, nie zawiera `HabitName`
- **NotificationDetailDto** (w `GetNotificationByIdQuery.cs`): używany dla pojedynczego rekordu, zawiera `HabitName` (wymaga JOIN)

Ta separacja jest celowa:
- Lista powiadomień nie potrzebuje nazw nawyków (oszczędność JOIN)
- Szczegóły pojedynczego powiadomienia mogą zawierać dodatkowe informacje

### Zależności między plikami
- Query & Handler → `HabitFlow.Core/Features/Notifications/GetNotificationByIdQuery.cs`
- Endpoint → `HabitFlow.Api/Endpoints/NotificationEndpoints.cs`
- Handler zależy od: `HabitFlowDbContext`, `ILoggedUserContext`, `IQuery<T>`, `IQueryHandler<T, R>`, `Result<T>`
- Endpoint zależy od: `IQueryDispatcher`, `NotificationDetailDto`, extension method `ToHttpResult()`

### Szacowany czas implementacji
**2-3 godziny** (włącznie z testami jednostkowymi i integracyjnymi)

- Implementacja Query & Handler: 30 min
- Endpoint: 15 min
- Testy jednostkowe: 45 min
- Testy integracyjne: 45 min
- Weryfikacja i dokumentacja: 15-30 min
