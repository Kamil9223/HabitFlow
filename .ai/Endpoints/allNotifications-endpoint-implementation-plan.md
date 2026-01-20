# API Endpoint Implementation Plan: GET /api/v1/notifications

## 1. Przegląd punktu końcowego

Endpoint służy do pobierania paginowanej listy powiadomień dla zalogowanego użytkownika. Zwraca wszystkie powiadomienia wygenerowane przez system (np. typu MissDue) wraz z informacjami o statusie generowania AI, posortowane domyślnie chronologicznie malejąco (najnowsze pierwsze).

**Kluczowe funkcjonalności:**
- Pobieranie powiadomień tylko dla aktualnie zalogowanego użytkownika
- Paginacja wyników (page, pageSize)
- Sortowanie (domyślnie: createdAtUtc:desc)
- Zwracanie metadanych paginacji (totalCount)

---

## 2. Szczegóły żądania

### Metoda HTTP
`GET`

### Struktura URL
`/api/v1/notifications`

### Parametry zapytania (Query Parameters)

#### Opcjonalne:
- **page** (int, opcjonalny)
  - Numer strony (1-based)
  - Domyślnie: 1
  - Minimum: 1
  - Przykład: `?page=2`

- **pageSize** (int, opcjonalny)
  - Liczba elementów na stronę
  - Domyślnie: 20
  - Minimum: 1
  - Maksimum: 100
  - Przykład: `?pageSize=50`

- **sort** (string, opcjonalny)
  - Format: `field:direction`
  - Domyślnie: `createdAtUtc:desc`
  - Dozwolone pola: `createdAtUtc`, `localDate`, `type`
  - Dozwolone kierunki: `asc`, `desc`
  - Przykład: `?sort=localDate:asc`

### Nagłówki
- **Authorization**: Required (Bearer token lub Cookie authentication)

### Request Body
Brak (GET endpoint)

---

## 3. Wykorzystywane typy

### 3.1. Query Model (Core Layer)

**Lokalizacja**: `HabitFlow.Core/Features/Notifications/GetNotificationsQuery.cs`

```csharp
/// <summary>
/// Supported fields for sorting notifications.
/// </summary>
public enum NotificationSortField
{
    CreatedAtUtc,
    LocalDate,
    Type
}

/// <summary>
/// Query to retrieve a paginated list of notifications for the current user.
/// </summary>
public record GetNotificationsQuery(
    int Page = 1,
    int PageSize = 20,
    NotificationSortField SortField = NotificationSortField.CreatedAtUtc,
    SortDirection SortDirection = SortDirection.Desc
) : IQuery<Result<PagedNotificationsDto>>;

/// <summary>
/// Data transfer object for paginated notifications list.
/// </summary>
public record PagedNotificationsDto(
    int TotalCount,
    IReadOnlyList<NotificationDto> Items
);

/// <summary>
/// Data transfer object for a single notification.
/// </summary>
public record NotificationDto(
    long Id,
    int HabitId,
    DateOnly LocalDate,
    NotificationType Type,
    string Content,
    AiGenerationStatus? AiStatus,
    DateTime CreatedAtUtc
);
```

### 3.2. Response DTO (API Contracts)

**Lokalizacja**: `HabitFlow.Api/Contracts/Notifications/NotificationResponse.cs` (już istnieje)

```csharp
public record NotificationResponse(
    long Id,
    int HabitId,
    DateOnly LocalDate,
    int Type,
    string Content,
    int? AiStatus,
    DateTimeOffset CreatedAtUtc
);
```

**Lokalizacja**: `HabitFlow.Api/Contracts/Common/PagedResponse.cs` (już istnieje)

```csharp
public record PagedResponse<T>(
    int TotalCount,
    IReadOnlyList<T> Items
);
```

### 3.3. Query Handler (Core Layer)

**Lokalizacja**: `HabitFlow.Core/Features/Notifications/GetNotificationsQueryHandler.cs`

```csharp
/// <summary>
/// Handler for retrieving a paginated and sorted list of notifications for the current user.
/// </summary>
public class GetNotificationsQueryHandler(
    HabitFlowDbContext context,
    ILoggedUserContext loggedUserContext)
    : IQueryHandler<GetNotificationsQuery, Result<PagedNotificationsDto>>
{
    private const int MinPageSize = 1;
    private const int MaxPageSize = 100;
    private const int MinPage = 1;

    public async Task<Result<PagedNotificationsDto>> Handle(
        GetNotificationsQuery query,
        CancellationToken cancellationToken)
    {
        // Implementation details in section 4
    }
}
```

### 3.4. Wykorzystywane Enumy

**NotificationType** (już istnieje w `HabitFlow.Data/Enums/NotificationType.cs`):
- MissDue = 1

**AiGenerationStatus** (już istnieje w `HabitFlow.Data/Enums/AiGenerationStatus.cs`):
- Success = 1
- Fallback = 2
- Error = 3

**SortDirection** (już istnieje w `HabitFlow.Core/Common/SortDirection.cs`):
- Asc
- Desc

---

## 4. Szczegóły odpowiedzi

### Odpowiedź sukcesu: 200 OK

**Content-Type**: `application/json`

**Struktura:**
```json
{
  "totalCount": 3,
  "items": [
    {
      "id": 555,
      "habitId": 101,
      "localDate": "2025-12-06",
      "type": 1,
      "content": "You missed yesterday...",
      "aiStatus": 2,
      "createdAtUtc": "2025-12-07T00:30:00Z"
    },
    {
      "id": 554,
      "habitId": 102,
      "localDate": "2025-12-05",
      "type": 1,
      "content": "Keep going! You missed...",
      "aiStatus": 1,
      "createdAtUtc": "2025-12-06T00:15:00Z"
    },
    {
      "id": 553,
      "habitId": 101,
      "localDate": "2025-12-04",
      "type": 1,
      "content": "Don't give up...",
      "aiStatus": 1,
      "createdAtUtc": "2025-12-05T00:20:00Z"
    }
  ]
}
```

### Odpowiedzi błędów

#### 400 Bad Request
Nieprawidłowe parametry zapytania (walidacja po stronie query handlera).

**Przykład:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Error",
  "status": 400,
  "errors": {
    "PageSize": ["PageSize must be between 1 and 100."],
    "Sort": ["Invalid sort parameter format. Expected 'field:direction'."]
  }
}
```

#### 401 Unauthorized
Użytkownik nie jest uwierzytelniony.

**Przykład:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "User is not authenticated."
}
```

#### 500 Internal Server Error
Nieoczekiwany błąd serwera (np. błąd bazy danych).

**Przykład:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred while processing your request."
}
```

---

## 5. Przepływ danych

### 5.1. Szczegółowy przepływ żądania

```
1. HTTP Request (GET /api/v1/notifications?page=1&pageSize=20&sort=createdAtUtc:desc)
   ↓
2. ASP.NET Core Middleware Pipeline
   ↓ [Authentication Middleware]
   ↓ [Authorization Middleware - RequireAuthorization()]
   ↓
3. Minimal API Endpoint (NotificationEndpoints.cs)
   - Bind query parameters
   - Parse sort parameter string → NotificationSortField + SortDirection
   ↓
4. Create GetNotificationsQuery record
   ↓
5. Dispatch to IQueryDispatcher
   ↓
6. GetNotificationsQueryHandler.Handle()
   a) Get current user from ILoggedUserContext
   b) Validate & clamp page/pageSize (1-100)
   c) Build base query: context.Notifications.AsNoTracking()
   d) Filter by UserId (security)
   e) Get total count: await CountAsync()
   f) Apply sorting (CreatedAtUtc DESC by default)
   g) Apply pagination: Skip((page-1) * pageSize).Take(pageSize)
   h) Project to NotificationDto with Select()
   i) Execute: await ToListAsync()
   j) Return Result.Success(new PagedNotificationsDto(...))
   ↓
7. Map Result<PagedNotificationsDto> → HTTP Response
   - Success → 200 OK with PagedResponse<NotificationResponse>
   - Failure → appropriate error code with ProblemDetails
   ↓
8. Response sent to client
```

### 5.2. Interakcje z bazą danych

**Tabela główna**: `Notifications`

**Wykorzystywany indeks** (z db-plan.md):
- `IX_Notifications_UserId_CreatedAtUtc` (UserId, CreatedAtUtc DESC)
  - INCLUDE (Content, Type, HabitId, LocalDate)
  - **Pokrywający indeks** → brak lookup do tabeli bazowej

**Przykładowe zapytanie SQL** (generowane przez EF Core):
```sql
-- Count query
SELECT COUNT(*)
FROM [Notifications]
WHERE [UserId] = @p0;

-- Data query (with default sort)
SELECT [Id], [HabitId], [LocalDate], [Type], [Content], [AiStatus], [CreatedAtUtc]
FROM [Notifications]
WHERE [UserId] = @p0
ORDER BY [CreatedAtUtc] DESC
OFFSET @p1 ROWS
FETCH NEXT @p2 ROWS ONLY;
```

**Optymalizacje:**
- `AsNoTracking()` → brak trackingu zmian (tylko odczyt)
- `Select()` → projekcja tylko potrzebnych kolumn
- INCLUDE w indeksie → wszystkie kolumny w indeksie
- Brak JOIN → denormalizowane UserId

### 5.3. Row-Level Security (RLS)

**Automatyczne filtrowanie** (z db-plan.md):
```sql
CREATE SECURITY POLICY dbo.NotificationsSecurityPolicy
ADD FILTER PREDICATE dbo.fn_SecurityPredicate(UserId)
    ON dbo.Notifications
WITH (STATE = ON);
```

**Uwaga**: RLS działa na poziomie bazy danych jako dodatkowa warstwa bezpieczeństwa. Handler jawnie filtruje po UserId dla przejrzystości i wydajności (wykorzystanie indeksu).

---

## 6. Względy bezpieczeństwa

### 6.1. Uwierzytelnianie (Authentication)
- **Wymagane**: Endpoint oznaczony `.RequireAuthorization()`
- **Mechanizm**: ASP.NET Core Identity (Cookie lub JWT Bearer)
- **Middleware**: `AuthenticationMiddleware` weryfikuje tożsamość przed wywołaniem endpointu
- **Błąd**: 401 Unauthorized jeśli użytkownik nie jest uwierzytelniony

### 6.2. Autoryzacja (Authorization)
- **Poziom zasobu**: Użytkownik może widzieć tylko swoje powiadomienia
- **Implementacja**:
  - Filtrowanie przez `UserId` w query handlerze
  - `ILoggedUserContext.GetUser()` dostarcza aktualnego zalogowanego użytkownika
  - RLS jako dodatkowa warstwa na poziomie bazy danych
- **Brak ról**: W MVP wszyscy użytkownicy mają równe uprawnienia do swoich zasobów

### 6.3. Walidacja danych wejściowych
- **Page**: Minimum 1, clamping do MinPage (1)
- **PageSize**: Zakres 1-100, clamping do MaxPageSize (100)
- **Sort**: Walidacja formatu `field:direction`
  - Dozwolone pola: createdAtUtc, localDate, type
  - Dozwolone kierunki: asc, desc
  - Nieprawidłowy format → zwróć Result.Failure z ValidationError

### 6.4. SQL Injection
- **Ochrona**: EF Core używa parametryzowanych zapytań (domyślnie)
- **Brak raw SQL**: Wszystkie zapytania przez LINQ → EF Core generuje bezpieczne SQL
- **Sortowanie**: Enum-based (NotificationSortField) → brak dynamicznego SQL stringa

### 6.5. Mass Assignment
- **Nie dotyczy**: GET endpoint, brak request body
- **Projekcja**: Select() do konkretnego DTO → kontrolowane pola

### 6.6. Information Disclosure
- **Bezpieczne pola**: NotificationDto zawiera tylko pola przeznaczone dla użytkownika
- **Brak AiError**: Pole `AiError` nie jest eksponowane w API (tylko dla diagnostyki backendowej)
- **Brak navigation properties**: DTO nie zawiera zagnieżdżonych obiektów User/Habit

### 6.7. Rate Limiting
- **MVP**: Brak dedykowanego rate limitingu
- **Przyszłość**: Rozważyć implementację (np. AspNetCoreRateLimit) dla ochrony przed nadużyciami

### 6.8. HTTPS
- **Wymagane**: Wszystkie żądania przez HTTPS w produkcji
- **Konfiguracja**: ASP.NET Core redirect HTTP → HTTPS (middleware)

---

## 7. Obsługa błędów

### 7.1. Kategorie błędów

#### Błędy walidacji (400 Bad Request)
**Scenariusze:**
- `page < 1` → clamping do 1 (nie błąd, silent fix)
- `pageSize < 1 lub > 100` → clamping do zakresu (nie błąd, silent fix)
- Nieprawidłowy format parametru `sort` → Result.Failure z ValidationError
  - Przykład: `sort=invalid` (brak `:`)
  - Przykład: `sort=unknownField:desc` (nieznane pole)
  - Przykład: `sort=createdAtUtc:xyz` (nieprawidłowy kierunek)

**Response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Error",
  "status": 400,
  "errors": {
    "Sort": ["Invalid sort format. Expected 'field:direction' where field is one of: createdAtUtc, localDate, type and direction is asc or desc."]
  }
}
```

#### Błędy uwierzytelniania (401 Unauthorized)
**Scenariusze:**
- Brak tokenu/cookie authentication
- Token wygasły
- Token nieprawidłowy

**Obsługa:** ASP.NET Core Authentication Middleware automatycznie zwraca 401

**Response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

#### Błędy bazy danych (500 Internal Server Error)
**Scenariusze:**
- Timeout połączenia z SQL Server
- Deadlock
- Constraint violation (nieprzewidziana)

**Obsługa:**
- Handler nie catchuje wyjątków → propagacja do Global Exception Handler
- Logowanie pełnego stack trace przez ILogger
- Zwrot generycznego ProblemDetails (bez szczegółów technicznych dla użytkownika)

**Response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred."
}
```

### 7.2. Logowanie błędów

**ILogger<GetNotificationsQueryHandler>**:
- **Warning**: Nieprawidłowe parametry sort (po parsowaniu)
- **Error**: Wyjątki z EF Core (connection issues, timeouts)
- **Information**: Każde wywołanie handlera z podstawowymi parametrami (dla audytu)

**Przykład:**
```csharp
_logger.LogInformation(
    "Fetching notifications for user {UserId}, page {Page}, pageSize {PageSize}, sort {Sort}",
    user.UserId, page, pageSize, $"{query.SortField}:{query.SortDirection}");

_logger.LogWarning(
    "Invalid sort parameter: {SortString}. Using default sort.",
    sortString);

_logger.LogError(exception,
    "Database error while fetching notifications for user {UserId}",
    user.UserId);
```

### 7.3. Scenariusze brzegowe

#### Brak powiadomień
- **Zachowanie**: Zwróć pustą listę z `totalCount = 0`
- **Kod statusu**: 200 OK
- **Response**:
```json
{
  "totalCount": 0,
  "items": []
}
```

#### Strona poza zakresem (np. page=999 gdy jest tylko 10 elementów)
- **Zachowanie**: Zwróć pustą listę dla tej strony
- **Kod statusu**: 200 OK
- **totalCount**: Zawsze zwraca całkowitą liczbę elementów (niezależnie od page)
- **Response**:
```json
{
  "totalCount": 10,
  "items": []
}
```

#### Sortowanie null-safe
- **AiStatus może być NULL**: Obsługa w SQL ORDER BY (NULL last/first)
- **EF Core domyślnie**: NULL values sortowane na końcu dla ASC, na początku dla DESC

---

## 8. Rozważania dotyczące wydajności

### 8.1. Optymalizacje zapytań

**AsNoTracking()**
- Wyłącza change tracking EF Core
- Zmniejsza zużycie pamięci (~30-40%)
- Szybsze materializowanie obiektów

**Projekcja Select()**
- Wybór tylko potrzebnych kolumn
- Unika ładowania navigation properties (User, Habit)
- Mniejszy transfer danych z bazy

**Pokrywający indeks**
- `IX_Notifications_UserId_CreatedAtUtc` z INCLUDE
- Wszystkie potrzebne kolumny w indeksie
- **Brak lookup do tabeli bazowej** (covering index scan)

**Paginacja**
- OFFSET/FETCH NEXT → efektywne dla małych/średnich offsetów
- Dla bardzo dużych offsetów (page > 1000) rozważyć cursor-based pagination w przyszłości

### 8.2. Potencjalne wąskie gardła

**COUNT(*) query**
- Wykonywany przed każdym żądaniem dla totalCount
- Dla dużych tabel (>100k notifications) może być powolny
- **Rozwiązanie w przyszłości**: Cache totalCount lub approximate count

**Deep pagination (high page numbers)**
- OFFSET 10000 ROWS → baza musi przeskanować i pominąć 10k wierszy
- **Mitygacja MVP**: MaxPageSize = 100 ogranicza głębokość
- **Rozwiązanie w przyszłości**: Cursor-based pagination (seek method)

**Brak cache'owania**
- W MVP brak cache'u (Redis, in-memory)
- Każde żądanie → query do bazy
- **Rozwiązanie w przyszłości**: Cache pierwszej strony (najczęściej żądana)

### 8.3. Metryki do monitorowania

**Application Insights / Logging**:
- Średni czas odpowiedzi handlera
- P95/P99 response time
- Liczba powiadomień per użytkownik (outliers)
- Częstotliwość żądań dla różnych stron

**SQL Server**:
- Index usage stats dla `IX_Notifications_UserId_CreatedAtUtc`
- Query execution time
- Logical reads (powinno być niskie dzięki covering index)

### 8.4. Compiled Queries

**Opcjonalne**: Jeśli endpoint stanie się hot path (>1000 req/s):
```csharp
private static readonly Func<HabitFlowDbContext, Guid, int, int, IAsyncEnumerable<Notification>>
    CompiledQuery = EF.CompileAsyncQuery(
        (HabitFlowDbContext ctx, Guid userId, int skip, int take) =>
            ctx.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAtUtc)
                .Skip(skip)
                .Take(take)
    );
```

**Zysk**: ~10-20% szybsze wykonanie (eliminacja ponownego parsowania LINQ)
**Trade-off**: Złożoność kodu
**Rekomendacja dla MVP**: Pomiń (premature optimization)

---

## 9. Etapy wdrożenia

### Krok 1: Utworzenie Query Model i DTO (Core Layer)
**Plik**: `HabitFlow.Core/Features/Notifications/GetNotificationsQuery.cs`

**Zadania**:
- [ ] Utworzyć enum `NotificationSortField` (CreatedAtUtc, LocalDate, Type)
- [ ] Utworzyć rekord `GetNotificationsQuery : IQuery<Result<PagedNotificationsDto>>`
  - Parametry: Page, PageSize, SortField, SortDirection
  - Wartości domyślne: Page=1, PageSize=20, SortField=CreatedAtUtc, Direction=Desc
- [ ] Utworzyć rekord `PagedNotificationsDto(int TotalCount, IReadOnlyList<NotificationDto> Items)`
- [ ] Utworzyć rekord `NotificationDto` z polami:
  - long Id, int HabitId, DateOnly LocalDate, NotificationType Type
  - string Content, AiGenerationStatus? AiStatus, DateTime CreatedAtUtc

**Zależności**:
- `IQuery<TResult>` (już istnieje)
- `Result<T>` (już istnieje)
- `SortDirection` (już istnieje)
- `NotificationType`, `AiGenerationStatus` (już istnieją)

**Czas**: ~15 min

---

### Krok 2: Implementacja Query Handler (Core Layer)
**Plik**: `HabitFlow.Core/Features/Notifications/GetNotificationsQueryHandler.cs`

**Zadania**:
- [ ] Utworzyć klasę `GetNotificationsQueryHandler : IQueryHandler<GetNotificationsQuery, Result<PagedNotificationsDto>>`
- [ ] Wstrzyknąć zależności: `HabitFlowDbContext`, `ILoggedUserContext`
- [ ] Zdefiniować stałe: `MinPageSize = 1`, `MaxPageSize = 100`, `MinPage = 1`
- [ ] Implementacja `Handle()`:
  - [ ] Walidacja i clamping page/pageSize
  - [ ] Pobranie aktualnego użytkownika: `loggedUserContext.GetUser()`
  - [ ] Budowa base query: `context.Notifications.AsNoTracking().Where(n => n.UserId == userId)`
  - [ ] COUNT(*): `totalCount = await query.CountAsync(cancellationToken)`
  - [ ] Sortowanie: wywołanie `ApplySort(query, sortField, direction)`
  - [ ] Paginacja: `Skip((page-1)*pageSize).Take(pageSize)`
  - [ ] Projekcja: `Select(n => new NotificationDto(...))`
  - [ ] Materializacja: `await ToListAsync(cancellationToken)`
  - [ ] Zwrot: `Result.Success(new PagedNotificationsDto(totalCount, items))`
- [ ] Implementacja metody pomocniczej `ApplySort()`:
  - [ ] Pattern matching na (SortField, Direction)
  - [ ] Obsługa wszystkich kombinacji (CreatedAtUtc, LocalDate, Type × Asc/Desc)
  - [ ] Default: OrderByDescending CreatedAtUtc

**Logowanie**:
- [ ] `LogInformation` na początku Handle() z parametrami query
- [ ] `LogError` w przypadku wyjątków (opcjonalnie, jeśli dodamy try-catch)

**Testy jednostkowe** (do wykonania w późniejszym kroku):
- Happy path: domyślne parametry
- Paginacja: różne page/pageSize
- Sortowanie: wszystkie kombinacje SortField × Direction
- Edge cases: pusta lista, page poza zakresem
- Clamping: pageSize > 100, page < 1

**Czas**: ~45 min

---

### Krok 3: Parsowanie parametru sort w endpoincie (API Layer)
**Plik**: `HabitFlow.Api/Endpoints/NotificationEndpoints.cs`

**Zadania**:
- [ ] Utworzyć metodę pomocniczą `ParseSortParameter(string? sort)`:
  - Input: `"createdAtUtc:desc"` lub null
  - Output: `(NotificationSortField field, SortDirection direction)` lub default
  - Walidacja formatu (split przez `:`)
  - Walidacja dozwolonych wartości (enum TryParse)
  - Ignorowanie case (StringComparison.OrdinalIgnoreCase)
  - Dla null/invalid → zwróć default (CreatedAtUtc, Desc)
- [ ] Aktualizacja endpointu GET `/`:
  - [ ] Bind parametrów: `int? page, int? pageSize, string? sort`
  - [ ] Parse sort → `(sortField, sortDirection)`
  - [ ] Utworzenie `GetNotificationsQuery(page ?? 1, pageSize ?? 20, sortField, sortDirection)`
  - [ ] Dispatch: `await queryDispatcher.Dispatch(query, cancellationToken)`
  - [ ] Mapowanie Result<PagedNotificationsDto> → IResult:
    - Success → `Results.Ok(new PagedResponse<NotificationResponse>(dto.TotalCount, Map(dto.Items)))`
    - Failure → mapowanie przez Result mapper (ValidationError → 400)

**Mapowanie DTO → Response**:
- [ ] Utworzyć lokalną funkcję lub metodę rozszerzenia dla mapowania:
  ```csharp
  static NotificationResponse MapToResponse(NotificationDto dto) =>
      new(
          dto.Id,
          dto.HabitId,
          dto.LocalDate,
          (int)dto.Type,
          dto.Content,
          (int?)dto.AiStatus,
          new DateTimeOffset(dto.CreatedAtUtc, TimeSpan.Zero)
      );
  ```

**Czas**: ~30 min

---

### Krok 4: Rejestracja handlera w DI (Infrastructure/Program.cs)
**Plik**: `HabitFlow.Api/Program.cs` lub dedykowany DI configuration

**Zadania**:
- [ ] Zarejestrować `GetNotificationsQueryHandler` jako scoped:
  ```csharp
  services.AddScoped<IQueryHandler<GetNotificationsQuery, Result<PagedNotificationsDto>>,
                     GetNotificationsQueryHandler>();
  ```
- [ ] Lub użyć assembly scanning (jeśli już zaimplementowane):
  ```csharp
  services.Scan(scan => scan
      .FromAssemblyOf<GetNotificationsQueryHandler>()
      .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)))
      .AsImplementedInterfaces()
      .WithScopedLifetime());
  ```

**Weryfikacja**:
- [ ] Uruchomić aplikację i sprawdzić, czy DI resolve działa

**Czas**: ~10 min

---

### Krok 5: Testy jednostkowe (Query Handler)
**Plik**: `HabitFlow.Tests/UnitTests/Features/Notifications/GetNotificationsQueryHandlerTests.cs`

**Zadania**:
- [ ] Setup: In-memory DbContext + mock ILoggedUserContext
- [ ] Seed testowe dane (user + kilka notyfikacji)
- [ ] Testy:
  - [ ] `Handle_DefaultParameters_ReturnsFirstPageSortedByCreatedAtDesc()`
  - [ ] `Handle_CustomPageSize_ReturnsCorrectNumberOfItems()`
  - [ ] `Handle_SecondPage_ReturnsCorrectItems()`
  - [ ] `Handle_PageOutOfRange_ReturnsEmptyList()`
  - [ ] `Handle_SortByLocalDateAsc_ReturnsSortedResults()`
  - [ ] `Handle_SortByTypeDesc_ReturnsSortedResults()`
  - [ ] `Handle_PageSizeExceedsMax_ClampsToMaxPageSize()`
  - [ ] `Handle_NoNotifications_ReturnsEmptyListWithZeroCount()`
  - [ ] `Handle_FiltersOnlyCurrentUser_DoesNotReturnOtherUsersNotifications()`

**Czas**: ~90 min

---

### Krok 6: Testy integracyjne (Endpoint)
**Plik**: `HabitFlow.Tests/IntegrationTests/Endpoints/NotificationEndpointsTests.cs`

**Zadania**:
- [ ] Setup: TestContainers SQL Server + WebApplicationFactory
- [ ] Autentykacja testowego użytkownika
- [ ] Seed notyfikacji do bazy
- [ ] Testy:
  - [ ] `GetNotifications_Authenticated_Returns200WithPagedResults()`
  - [ ] `GetNotifications_Unauthenticated_Returns401()`
  - [ ] `GetNotifications_WithPagination_ReturnsCorrectPage()`
  - [ ] `GetNotifications_WithSortParameter_ReturnsSortedResults()`
  - [ ] `GetNotifications_InvalidSortFormat_Returns400()` (jeśli walidujemy)
  - [ ] `GetNotifications_EmptyList_Returns200WithZeroCount()`

**NSwag Client**:
- [ ] Użyć wygenerowanego klienta HTTP do wywołań
- [ ] Sprawdzić response model i status codes

**Czas**: ~60 min

---

### Krok 7: Dokumentacja OpenAPI (Swagger)
**Plik**: `HabitFlow.Api/Endpoints/NotificationEndpoints.cs`

**Zadania**:
- [ ] Dodać szczegółowe opisy parametrów:
  ```csharp
  .WithOpenApi(operation =>
  {
      operation.Summary = "Get paginated notifications for the current user";
      operation.Description = "Returns a paginated list of notifications sorted by creation date (newest first by default).";

      operation.Parameters[0].Description = "Page number (1-based, default: 1)";
      operation.Parameters[1].Description = "Page size (1-100, default: 20)";
      operation.Parameters[2].Description = "Sort format: field:direction. Fields: createdAtUtc, localDate, type. Directions: asc, desc. Default: createdAtUtc:desc";

      return operation;
  })
  ```
- [ ] Dodać przykłady odpowiedzi dla różnych kodów statusu

**Czas**: ~15 min

---

### Krok 8: Weryfikacja manualna i QA
**Zadania**:
- [ ] Uruchomić aplikację lokalnie (`dotnet run --project HabitFlow.Api`)
- [ ] Otworzyć Swagger UI (`https://localhost:5001/swagger`)
- [ ] Zalogować się (użyć endpointu /api/v1/auth/login)
- [ ] Wywołać GET /api/v1/notifications z różnymi parametrami:
  - [ ] Bez parametrów (default)
  - [ ] `?page=2&pageSize=5`
  - [ ] `?sort=localDate:asc`
  - [ ] `?sort=type:desc`
- [ ] Sprawdzić odpowiedzi:
  - [ ] totalCount się zgadza
  - [ ] items są posortowane poprawnie
  - [ ] paginacja działa
- [ ] Sprawdzić 401 bez uwierzytelnienia (logout)
- [ ] Sprawdzić logi aplikacji (brak błędów)

**Czas**: ~30 min

---

### Krok 9: Code Review i refactoring
**Zadania**:
- [ ] Przejrzeć kod pod kątem:
  - [ ] Zgodność z konwencjami projektu (file-scoped namespaces, 4 spacje)
  - [ ] XML comments dla publicznych typów
  - [ ] Null safety (nullable annotations)
  - [ ] Logowanie odpowiednich zdarzeń
- [ ] Uruchomić `dotnet format`
- [ ] Sprawdzić pokrycie testów (≥80% dla Core layer)

**Czas**: ~20 min

---

### Krok 10: Commit i dokumentacja
**Zadania**:
- [ ] Commit zmian z conventional commit message:
  ```
  feat(api): implement GET /api/v1/notifications endpoint

  - Add GetNotificationsQuery and handler
  - Support pagination (page, pageSize)
  - Support sorting (createdAtUtc, localDate, type)
  - Add unit and integration tests
  - Update OpenAPI documentation
  ```
- [ ] Zaktualizować dokumentację (jeśli wymagana)
- [ ] Utworzyć PR (jeśli workflow wymaga)

**Czas**: ~10 min

---

## 10. Całkowity czas implementacji: ~5.5 godziny

**Breakdown**:
- Core logic (Query, Handler): ~1h
- Endpoint i mapowanie: ~30 min
- Testy jednostkowe: ~90 min
- Testy integracyjne: ~60 min
- Dokumentacja i weryfikacja: ~45 min
- Code review i commit: ~30 min
- Buffer dla nieprzewidzianych problemów: ~45 min

---

## 11. Checklisty weryfikacyjne

### Pre-implementation Checklist
- [ ] Przeczytany i zrozumiany db-plan.md (tabela Notifications, indeksy, RLS)
- [ ] Przeczytany tech stack (EF Core, ASP.NET Core Identity, Minimal APIs)
- [ ] Przeczytane implementation rules (CQS, Result<T>, AsNoTracking, projekcja)
- [ ] Zidentyfikowane istniejące typy do reużycia (PagedResponse, NotificationResponse)
- [ ] Zidentyfikowane wzorce z innych handlerów (GetHabitsQueryHandler)

### Post-implementation Checklist
- [ ] Wszystkie testy przechodzą (`dotnet test`)
- [ ] Kod sformatowany (`dotnet format`)
- [ ] Brak błędów kompilacji
- [ ] Endpoint działa w Swagger UI
- [ ] Logowanie poprawnie skonfigurowane
- [ ] Dokumentacja OpenAPI kompletna
- [ ] Code review wykonane
- [ ] Commit z odpowiednim message

---

## 12. Potencjalne rozszerzenia (post-MVP)

### Filtrowanie
- Dodać parametr `type` (NotificationType filter)
- Dodać parametr `habitId` (notifications dla konkretnego nawyku)
- Dodać parametr `dateFrom`/`dateTo` (zakres localDate)

### Cache
- Implementacja Redis cache dla pierwszej strony
- Cache invalidation przy tworzeniu nowych notyfikacji (background job)

### Cursor-based pagination
- Zamiana OFFSET/FETCH na seek method dla lepszej wydajności przy dużych offsetach
- Parametr `cursor` zamiast `page`

### Read/Unread status
- Dodać kolumnę `IsRead` do tabeli Notifications
- Endpoint PATCH /api/v1/notifications/{id}/read
- Filtrowanie `?unreadOnly=true`

### Soft delete
- Możliwość "usuwania" notyfikacji przez użytkownika (IsDeleted flag)
- Filtrowanie domyślne `WHERE IsDeleted = 0`

### Real-time updates
- SignalR hub do push notifications
- Client otrzymuje nowe notyfikacje bez potrzeby pollingu

---

## 13. Referencje

### Dokumenty projektu
- `.ai/db-plan.md` - schemat bazy danych
- `.ai/tech-stack.md` - stack technologiczny
- `backend.md` - reguły implementacji backendu
- `.ai/test-plan.md` - plan testów

### Istniejące wzorce w kodzie
- `HabitFlow.Core/Features/Habits/GetHabitsQuery.cs` - wzorzec paginacji i sortowania
- `HabitFlow.Api/Endpoints/HabitEndpoints.cs` - wzorzec Minimal API endpoints
- `HabitFlow.Api/Contracts/Common/PagedResponse.cs` - reużywalny typ dla paginacji

### Zewnętrzne zasoby
- [RFC 7807 - Problem Details](https://tools.ietf.org/html/rfc7807)
- [ASP.NET Core Authentication](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/)
- [EF Core Query Performance](https://docs.microsoft.com/en-us/ef/core/performance/efficient-querying)
- [SQL Server Covering Indexes](https://docs.microsoft.com/en-us/sql/relational-databases/indexes/create-indexes-with-included-columns)

---

## 14. Glosariusz

- **RLS (Row-Level Security)**: Mechanizm SQL Server filtrujący wiersze na podstawie kontekstu użytkownika
- **Covering Index**: Indeks zawierający wszystkie kolumny potrzebne do query (INCLUDE clause)
- **AsNoTracking**: EF Core feature wyłączający change tracking dla read-only queries
- **CQS (Command Query Separation)**: Wzorzec oddzielający operacje zapisu (commands) od odczytu (queries)
- **DTO (Data Transfer Object)**: Obiekt przenoszący dane między warstwami bez logiki biznesowej
- **Clamping**: Ograniczanie wartości do określonego zakresu (np. Math.Clamp)
- **Projection**: Transformacja wyników zapytania do konkretnego kształtu (Select w LINQ)

---

**Plan gotowy do wdrożenia.** ✅
