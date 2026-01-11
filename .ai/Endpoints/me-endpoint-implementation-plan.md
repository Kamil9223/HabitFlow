# API Endpoint Implementation Plan: GET /api/v1/auth/me

## 1. Przegląd punktu końcowego

Endpoint GET `/api/v1/auth/me` zwraca podstawowe informacje o aktualnie zalogowanym użytkowniku, niezbędne do inicjalizacji powłoki aplikacji (App Shell). Służy do pobrania danych użytkownika takich jak ID, email, status potwierdzenia emaila, strefa czasowa oraz data utworzenia konta.

**Cel**: Dostarczenie frontendu (Blazor Server) kluczowych danych użytkownika po zalogowaniu, umożliwiając personalizację interfejsu (wyświetlanie email, zarządzanie strefą czasową, wyświetlanie informacji o koncie).

**Wymagania funkcjonalne**:
- Dostępny tylko dla uwierzytelnionych użytkowników
- Zwraca dane użytkownika na podstawie sesji/cookie
- Prosty, szybki endpoint wywoływany przy inicjalizacji App Shell

---

## 2. Szczegóły żądania

- **Metoda HTTP**: GET
- **Struktura URL**: `/api/v1/auth/me`
- **Parametry**:
  - **Wymagane**: Brak (użytkownik identyfikowany przez sesję)
  - **Opcjonalne**: Brak
- **Request Body**: Brak (GET)
- **Headers**:
  - `Cookie`: Sesja ASP.NET Core Identity (automatyczna)
- **Uwierzytelnianie**: Cookie-based session (ASP.NET Core Identity)

---

## 3. Wykorzystywane typy

### 3.1. Response DTO (istniejące)

**Lokalizacja**: `HabitFlow.Api.Contracts.Auth.MeResponse`

```csharp
public record MeResponse(
    string UserId,
    string Email,
    bool EmailConfirmed,
    string TimeZoneId,
    DateTimeOffset CreatedAtUtc
);
```

**Uwaga**: Typ już istnieje, nie wymaga zmian.

### 3.2. Query Model (do utworzenia)

**Lokalizacja**: `HabitFlow.Core.Features.Auth.GetMeQuery.cs`

```csharp
public record GetMeQuery() : IQuery<Result<MeResponse>>;
```

**Charakterystyka**:
- Marker query bez parametrów
- Użytkownik identyfikowany przez `ILoggedUserContext`
- Zwraca `Result<MeResponse>` (obsługa błędów przez Result pattern)

### 3.3. Query Handler (do utworzenia)

**Lokalizacja**: `HabitFlow.Core.Features.Auth.GetMeQuery.cs` (w tym samym pliku co query)

```csharp
public class GetMeQueryHandler(
    ILoggedUserContext loggedUserContext,
    HabitFlowDbContext dbContext)
    : IQueryHandler<GetMeQuery, Result<MeResponse>>
{
    public async Task<Result<MeResponse>> Handle(
        GetMeQuery query,
        CancellationToken cancellationToken)
    {
        // Implementation details in section 8
    }
}
```

### 3.4. Entity (istniejące)

**Lokalizacja**: `HabitFlow.Data.Entities.ApplicationUser`

- **Wykorzystywane pola**:
  - `Id` (Guid)
  - `Email` (string)
  - `EmailConfirmed` (bool)
  - `TimeZoneId` (string)
  - `CreatedAtUtc` (DateTime)

---

## 4. Szczegóły odpowiedzi

### 4.1. Success Response (200 OK)

**Status Code**: `200 OK`

**Body**:
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "emailConfirmed": true,
  "timeZoneId": "Europe/Warsaw",
  "createdAtUtc": "2025-12-07T15:50:00Z"
}
```

**Content-Type**: `application/json`

### 4.2. Error Responses

#### 401 Unauthorized

**Kiedy**: Użytkownik nie jest zalogowany (brak lub nieprawidłowa sesja)

**Obsługa**: Automatyczna przez middleware `RequireAuthorization()`

**Body**: Standard ASP.NET Core 401 response

---

#### 404 Not Found (Edge Case)

**Kiedy**: Użytkownik zalogowany (sesja aktywna), ale rekord usunięty z bazy danych

**Body**:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "errors": [
    {
      "code": "User.NotFound",
      "message": "User account not found."
    }
  ]
}
```

---

#### 500 Internal Server Error

**Kiedy**: Nieoczekiwany błąd aplikacji/bazy danych

**Obsługa**: Global exception handler (ASP.NET Core)

---

## 5. Przepływ danych

### 5.1. Diagram przepływu

```
[Client/Browser]
    |
    | GET /api/v1/auth/me + Cookie
    |
    v
[ASP.NET Core Middleware]
    |
    | RequireAuthorization() → ClaimsPrincipal
    |
    v
[AuthEndpoints.MapGet("/me")]
    |
    | Dispatch GetMeQuery
    |
    v
[GetMeQueryHandler]
    |
    ├→ ILoggedUserContext.GetUser() → CurrentUser
    |
    ├→ dbContext.Users.FindAsync(userId)
    |
    ├→ Map ApplicationUser → MeResponse
    |
    └→ Return Result<MeResponse>
    |
    v
[Endpoint]
    |
    | result.ToHttpResult(Results.Ok)
    |
    v
[Client] ← 200 OK + JSON
```

### 5.2. Szczegółowy opis kroków

1. **Klient wysyła żądanie**:
   - GET `/api/v1/auth/me`
   - Cookie z sesją ASP.NET Core Identity

2. **ASP.NET Core Middleware**:
   - `RequireAuthorization()` weryfikuje sesję
   - Jeśli brak autoryzacji → 401 Unauthorized (automatyczny short-circuit)
   - Jeśli OK → wypełnia `HttpContext.User` (ClaimsPrincipal)

3. **Endpoint**:
   - Tworzy `GetMeQuery()`
   - Wywołuje `IQueryDispatcher.Dispatch(query, ct)`

4. **GetMeQueryHandler**:
   - Pobiera `CurrentUser` z `ILoggedUserContext.GetUser()`
     - Wyciąga `UserId`, `TimeZoneId`, `Email` z ClaimsPrincipal
   - Wykonuje query do bazy:
     ```csharp
     var user = await dbContext.Users.FindAsync(
         new object[] { currentUser.UserId },
         cancellationToken);
     ```
   - **Walidacja**: Jeśli `user == null` → zwróć `Result.Failure<MeResponse>(Error.NotFound(...))`
   - **Mapowanie**:
     ```csharp
     var response = new MeResponse(
         user.Id.ToString(),
         user.Email!,
         user.EmailConfirmed,
         user.TimeZoneId,
         new DateTimeOffset(user.CreatedAtUtc, TimeSpan.Zero)
     );
     ```
   - Zwraca `Result.Success(response)`

5. **Endpoint**:
   - Wywołuje `result.ToHttpResult(value => Results.Ok(value))`
   - Jeśli success → 200 OK + JSON
   - Jeśli failure (404) → 404 Not Found + error JSON

6. **Odpowiedź do klienta**:
   - JSON z danymi użytkownika lub błąd

---

## 6. Względy bezpieczeństwa

### 6.1. Uwierzytelnianie

- **Mechanizm**: Cookie-based session (ASP.NET Core Identity)
- **Wymaganie**: Endpoint oznaczony `.RequireAuthorization()`
- **Weryfikacja**: Automatyczna przez middleware
- **Brak dostępu dla niezalogowanych**: 401 Unauthorized

### 6.2. Autoryzacja

- **Zakres danych**: Użytkownik widzi tylko swoje dane
- **Implicit authorization**: ID użytkownika z `ClaimsPrincipal` (HttpContext.User)
- **Brak dodatkowej autoryzacji**: Użytkownik może zawsze zobaczyć swoje własne dane

### 6.3. Row-Level Security (RLS)

- **Nie dotyczy**: Pobieramy pojedynczy rekord `ApplicationUser` po PRIMARY KEY (Id)
- **Brak ryzyka**: Nie ma możliwości dostępu do danych innych użytkowników
  - UserId pochodzi z ClaimsPrincipal (trusted source po autentykacji)

### 6.4. Validacja danych wejściowych

- **Brak parametrów zewnętrznych**: GET bez query params/body
- **Walidacja UserId**: Guid z claims (zwalidowany przez Identity)
- **Walidacja istnienia użytkownika**: Handler sprawdza `user != null`

### 6.5. Wrażliwe dane w odpowiedzi

- **Email**: Dostępny dla zalogowanego użytkownika (bezpieczne)
- **TimeZoneId**: Niepoufne
- **CreatedAtUtc**: Niepoufne
- **Brak haseł/tokenów**: Response nie zawiera wrażliwych danych

### 6.6. HTTPS

- **Wymaganie**: Komunikacja przez HTTPS w produkcji
- **Development**: `dotnet dev-certs https --trust`

### 6.7. CORS

- **Konfiguracja**: Na poziomie aplikacji (Program.cs)
- **Nie specyficzne dla endpointa**: Blazor Server komunikuje się z API przez SignalR (same-origin)

### 6.8. Rate Limiting

- **MVP**: Brak rate limiting
- **Przyszłość**: Rozważyć dla API endpointów (np. AspNetCoreRateLimit)

---

## 7. Obsługa błędów

### 7.1. Błędy uwierzytelniania (401)

**Scenariusz**:
- Użytkownik nie jest zalogowany (brak cookie)
- Sesja wygasła
- Cookie nieprawidłowy/zmanipulowany

**Obsługa**:
- Middleware `RequireAuthorization()` zwraca 401 automatycznie
- Endpoint nie jest wywoływany

**Logowanie**:
- Standardowe ASP.NET Core logging (informacyjne)

**Client action**:
- Redirect do `/login`

---

### 7.2. Użytkownik nie istnieje (404)

**Scenariusz**:
- Użytkownik zalogowany (sesja aktywna)
- Rekord usunięty z bazy (edge case: usunięcie przez admin lub cascade delete bug)

**Obsługa w handlerze**:
```csharp
var user = await dbContext.Users.FindAsync(...);
if (user is null)
{
    return Result.Failure<MeResponse>(
        Error.NotFound("User.NotFound", "User account not found."));
}
```

**Response**:
- 404 Not Found
- Error JSON z kodem `User.NotFound`

**Logowanie**:
- Warning level: "User {UserId} authenticated but not found in database"

**Client action**:
- Wylogowanie użytkownika (SignOut)
- Redirect do `/login`

---

### 7.3. Błędy bazy danych (500)

**Scenariusz**:
- SQL Server down/timeout
- Connection pool exhausted
- Nieoczekiwany exception w EF Core

**Obsługa**:
- Global exception handler (middleware)
- Handler może rzucić exception lub zwrócić `Result.Failure` z internal error

**Response**:
- 500 Internal Server Error
- Generyczny error message (bez szczegółów technicznych)

**Logowanie**:
- Error level: Full exception stack trace
- Include UserId, query details

**Client action**:
- Wyświetlenie komunikatu "Service temporarily unavailable"
- Retry (automatyczny lub manualny)

---

### 7.4. Mapowanie Result → HTTP Status

**Wykorzystanie helper method** `result.ToHttpResult()` z istniejącej infrastruktury:

```csharp
return result.ToHttpResult(value => Results.Ok(value));
```

**Mapowanie Error.Type → Status Code**:
- `NotFound` → 404
- `Unauthorized` → 401 (nie występuje w handlerze, obsłużone w middleware)
- Inny błąd → 500 (lub mapowanie custom errorów w ToHttpResult)

---

## 8. Rozważania dotyczące wydajności

### 8.1. Zapytanie do bazy danych

**Query**:
```csharp
dbContext.Users.FindAsync(userId)
```

**Charakterystyka**:
- SELECT po PRIMARY KEY (Guid Id)
- O(1) - indeks clustered (domyślny PK w Identity)
- Bardzo szybkie (< 5ms dla lokalnego SQL Server)

**Brak JOINów**:
- Nie pobieramy powiązanych encji (Habits, Checkins, Notifications)
- AsNoTracking nie jest potrzebny (read-only endpoint, ale można dodać dla optymalizacji):
  ```csharp
  dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId)
  ```

### 8.2. Częstotliwość wywołań

**Scenariusz**:
- Endpoint wywoływany przy inicjalizacji App Shell
- Zazwyczaj 1 raz na sesję/reload aplikacji
- Niskie obciążenie (nie jest to hot path)

**Load**:
- MVP: < 100 użytkowników → ~1 request/user/session
- Nie wymaga cachingu w MVP

### 8.3. Caching (przyszłość)

**Możliwości po MVP**:
1. **Memory Cache** (IMemoryCache):
   - Cache `MeResponse` per user (klucz: UserId)
   - TTL: 5-15 minut
   - Invalidacja: przy update profilu (zmiana TimeZoneId, Email)

2. **Distributed Cache** (Redis):
   - Dla skali (multiple instances Blazor Server)
   - Shared cache między instancjami

**Uwaga**: W MVP brak cachingu (prostota implementacji, niskie obciążenie).

### 8.4. Potencjalne wąskie gardła

**Baza danych**:
- W MVP: brak ryzyka (proste query, niski load)
- W produkcji: connection pooling SQL Server (domyślne w EF Core)

**SignalR connections** (Blazor Server):
- Każdy SignalR hub connection wywołuje `/me` przy inicjalizacji
- Jeśli wiele reconnections → może generować spike
- Mitigation: Client-side cache w Blazor (przechowywać MeResponse w session storage)

### 8.5. Optymalizacje (opcjonalne)

1. **AsNoTracking**:
   ```csharp
   dbContext.Users.AsNoTracking().FirstOrDefaultAsync(...)
   ```

2. **Projection** (jeśli FindAsync nie wystarcza):
   ```csharp
   dbContext.Users
       .Where(u => u.Id == userId)
       .Select(u => new MeResponse(
           u.Id.ToString(),
           u.Email!,
           u.EmailConfirmed,
           u.TimeZoneId,
           new DateTimeOffset(u.CreatedAtUtc, TimeSpan.Zero)
       ))
       .FirstOrDefaultAsync(cancellationToken);
   ```
   - Korzyść: Brak materializacji pełnej entity
   - W praktyce: minimalna różnica dla 5 pól

---

## 9. Etapy wdrożenia

### Krok 1: Utworzenie Query i Query Handlera

**Plik**: `HabitFlow.Core/Features/Auth/GetMeQuery.cs`

**Zawartość**:
```csharp
using HabitFlow.Api.Contracts.Auth;
using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Abstractions.Services;
using HabitFlow.Core.Common;
using HabitFlow.Data;
using Microsoft.EntityFrameworkCore;

namespace HabitFlow.Core.Features.Auth;

/// <summary>
/// Query to retrieve current authenticated user information.
/// </summary>
public record GetMeQuery() : IQuery<Result<MeResponse>>;

/// <summary>
/// Handler for GetMeQuery.
/// </summary>
public class GetMeQueryHandler(
    ILoggedUserContext loggedUserContext,
    HabitFlowDbContext dbContext)
    : IQueryHandler<GetMeQuery, Result<MeResponse>>
{
    public async Task<Result<MeResponse>> Handle(
        GetMeQuery query,
        CancellationToken cancellationToken)
    {
        // Get current user from claims
        var currentUser = loggedUserContext.GetUser();

        // Fetch user from database
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUser.UserId, cancellationToken);

        // Validate user exists
        if (user is null)
        {
            return Result.Failure<MeResponse>(
                Error.NotFound("User.NotFound", "User account not found."));
        }

        // Map to response
        var response = new MeResponse(
            user.Id.ToString(),
            user.Email!,
            user.EmailConfirmed,
            user.TimeZoneId,
            new DateTimeOffset(user.CreatedAtUtc, TimeSpan.Zero)
        );

        return Result.Success(response);
    }
}
```

**Uwagi**:
- Handler zarejestrowany automatycznie przez reflection (DependencyInjection.cs)
- AsNoTracking dla wydajności (read-only)
- FirstOrDefaultAsync zamiast FindAsync (umożliwia AsNoTracking)

---

### Krok 2: Aktualizacja endpointa w AuthEndpoints.cs

**Lokalizacja**: `HabitFlow.Api/Endpoints/AuthEndpoints.cs:111`

**Zmiana**: Zastąpienie `Results.StatusCode(501)` implementacją:

```csharp
group.MapGet("/me", async (
    IQueryDispatcher dispatcher,
    CancellationToken cancellationToken) =>
{
    var query = new GetMeQuery();
    var result = await dispatcher.Dispatch(query, cancellationToken);

    return result.ToHttpResult(value => Results.Ok(value));
})
.WithName("GetMe")
.WithSummary("Get current user information")
.WithDescription("Returns essential information about the authenticated user for App Shell initialization.")
.Produces<MeResponse>(200)
.Produces(401)
.Produces(404)
.RequireAuthorization();
```

**Uwagi**:
- Dodano `IQueryDispatcher` jako parametr
- Dodano `WithSummary` i `WithDescription` dla dokumentacji
- Dodano status code 404 do `Produces`

---

### Krok 3: Testy jednostkowe

**Plik**: `HabitFlow.Tests/UnitTests/Auth/GetMeQueryHandlerTests.cs`

**Zakres testów**:
1. **Test: Should return user data when user exists**
   - Mock `ILoggedUserContext` → zwraca valid UserId
   - Mock `HabitFlowDbContext.Users` → zwraca ApplicationUser
   - Assert: Result.IsSuccess, response zawiera poprawne dane

2. **Test: Should return NotFound when user does not exist**
   - Mock `ILoggedUserContext` → zwraca valid UserId
   - Mock `HabitFlowDbContext.Users` → zwraca null
   - Assert: Result.IsFailure, error code = "User.NotFound"

3. **Test: Should map all fields correctly**
   - Mock user z wszystkimi polami
   - Assert: każde pole MeResponse odpowiada polom ApplicationUser

**Framework**: XUnit + NSubstitute (zgodnie z tech-stack)

**Przykład testu**:
```csharp
[Fact]
public async Task Handle_ShouldReturnUserData_WhenUserExists()
{
    // Arrange
    var userId = Guid.NewGuid();
    var user = new ApplicationUser
    {
        Id = userId,
        Email = "test@example.com",
        EmailConfirmed = true,
        TimeZoneId = "Europe/Warsaw",
        CreatedAtUtc = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc)
    };

    var loggedUserContext = Substitute.For<ILoggedUserContext>();
    loggedUserContext.GetUser().Returns(new CurrentUser(userId, "Europe/Warsaw", "test@example.com"));

    var dbContext = Substitute.For<HabitFlowDbContext>();
    // Mock DbSet<ApplicationUser> setup (using NSubstitute extensions)

    var handler = new GetMeQueryHandler(loggedUserContext, dbContext);
    var query = new GetMeQuery();

    // Act
    var result = await handler.Handle(query, CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(userId.ToString(), result.Value.UserId);
    Assert.Equal("test@example.com", result.Value.Email);
    Assert.True(result.Value.EmailConfirmed);
    Assert.Equal("Europe/Warsaw", result.Value.TimeZoneId);
}
```

**Uwaga**: Mockowanie `DbContext` z EF Core jest skomplikowane; rozważ użycie In-Memory Database lub TestContainers dla testów integracyjnych.

---

### Krok 4: Testy integracyjne

**Plik**: `HabitFlow.Tests/IntegrationTests/Auth/MeEndpointTests.cs`

**Zakres testów**:
1. **Test: GET /api/v1/auth/me returns 401 when not authenticated**
   - Request bez cookie
   - Assert: 401 Unauthorized

2. **Test: GET /api/v1/auth/me returns 200 with user data when authenticated**
   - Seed user do TestContainers SQL Server
   - Login (ustawienie cookie)
   - Request GET /api/v1/auth/me
   - Assert: 200 OK, response zawiera dane użytkownika

3. **Test: GET /api/v1/auth/me returns 404 when user deleted**
   - Login user
   - Delete user z bazy (z zachowaniem sesji)
   - Request GET /api/v1/auth/me
   - Assert: 404 Not Found

**Framework**: XUnit + TestContainers (zgodnie z tech-stack)

**Setup**:
- TestContainers SQL Server (shared dla wszystkich testów)
- WebApplicationFactory dla API
- HttpClient z cookie container

**Przykład testu**:
```csharp
[Fact]
public async Task GetMe_ReturnsOk_WhenAuthenticated()
{
    // Arrange
    var client = _factory.CreateClient(); // WebApplicationFactory
    await SeedUserAsync("test@example.com", "Password123!");
    await LoginAsync(client, "test@example.com", "Password123!");

    // Act
    var response = await client.GetAsync("/api/v1/auth/me");

    // Assert
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    var meResponse = JsonSerializer.Deserialize<MeResponse>(content);

    Assert.NotNull(meResponse);
    Assert.Equal("test@example.com", meResponse.Email);
    Assert.Equal("Europe/Warsaw", meResponse.TimeZoneId);
}
```

---

### Krok 5: Aktualizacja dokumentacji (opcjonalne)

**Plik**: `.ai/api-plan.md` lub dedykowany plik dokumentacji API

**Zawartość**:
- Szczegółowy opis endpointa GET `/api/v1/auth/me`
- Przykłady request/response
- Kody błędów

**Uwaga**: W MVP można pominąć (OpenAPI w Development mode dostarcza dokumentację).

---

### Krok 6: Manualne testowanie

1. **Uruchomienie aplikacji**:
   ```bash
   dotnet run --project HabitFlow.Api
   ```

2. **Testowanie w Swagger** (Development):
   - Otwórz `https://localhost:5001/swagger`
   - Zaloguj się przez endpoint `/api/v1/auth/login`
   - Wywołaj GET `/api/v1/auth/me`
   - Zweryfikuj response

3. **Testowanie z Blazor**:
   - Uruchom `HabitFlow.Blazor`
   - Zaloguj się
   - Zweryfikuj, że App Shell wyświetla poprawne dane użytkownika

4. **Edge cases**:
   - Logout → wywołaj `/me` → sprawdź 401
   - Usuń użytkownika z bazy (SQL) → wywołaj `/me` → sprawdź 404

---

### Krok 7: Code review i merge

1. **Samokontrola**:
   - Przejrzyj kod pod kątem conventions (CLAUDE.md)
   - Uruchom `dotnet format`
   - Upewnij się, że testy przechodzą: `dotnet test`

2. **Pull Request**:
   - Tytuł: `feat(auth): implement GET /api/v1/auth/me endpoint`
   - Opis: Link do planu implementacji, scenariusze testowe
   - Reviewers: Zespół

3. **CI/CD**:
   - GitHub Actions wykonuje build + testy
   - Merge po zatwierdzeniu

---

## 10. Podsumowanie

### Kluczowe decyzje implementacyjne

1. **Query pattern (CQRS)**: `GetMeQuery` + handler dla spójności z architekturą
2. **Result pattern**: Obsługa błędów przez `Result<MeResponse>`
3. **ILoggedUserContext**: Pobieranie UserId z ClaimsPrincipal
4. **AsNoTracking**: Optymalizacja read-only query
5. **404 dla brakującego użytkownika**: Edge case handling (user deleted, session active)
6. **Brak cachingu w MVP**: Prostota, niskie obciążenie

### Kryteria akceptacji

- ✅ Endpoint zwraca 200 z danymi użytkownika dla zalogowanych
- ✅ Endpoint zwraca 401 dla niezalogowanych
- ✅ Endpoint zwraca 404 gdy użytkownik nie istnieje
- ✅ Wszystkie testy jednostkowe i integracyjne przechodzą
- ✅ Response zgodny ze specyfikacją (MeResponse DTO)
- ✅ Dokumentacja OpenAPI aktualna (Swagger)

### Szacowany czas implementacji

- Krok 1-2 (Query + Endpoint): **30 min**
- Krok 3 (Testy jednostkowe): **45 min**
- Krok 4 (Testy integracyjne): **60 min**
- Krok 5-6 (Dokumentacja + Manualne testy): **30 min**
- **Total**: ~**2.5 godz**

### Followup (po MVP)

- Dodanie cachingu (IMemoryCache/Redis)
- Rate limiting dla API
- Metryki (Application Insights): czas odpowiedzi, liczba 404
- Client-side cache w Blazor (session storage)
