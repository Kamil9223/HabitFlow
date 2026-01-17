# API Endpoint Implementation Plan: GET /api/v1/checkins

## 1. Przegląd punktu końcowego

Endpoint służy do pobierania wszystkich check-inów użytkownika dla konkretnej daty lokalnej. Jest to endpoint pomocniczy dla widoku "dzisiaj" (today view), który pozwala na szybkie backfillowanie interfejsu użytkownika z danymi dla wszystkich nawyków w wybranym dniu.

**Kluczowe cechy**:
- Read-only operation (GET)
- Filtrowanie po dacie lokalnej i automatycznie po UserId z kontekstu
- Zwraca pełną listę check-inów dla jednej daty
- Wykorzystuje wydajny indeks klastrowy dla szybkich zapytań

## 2. Szczegóły żądania

- **Metoda HTTP**: GET
- **Struktura URL**: `/api/v1/checkins?date={YYYY-MM-DD}`
- **Parametry**:
  - **Wymagane**:
    - `date` (string): Data w formacie ISO 8601 (YYYY-MM-DD), np. "2025-12-07"
  - **Opcjonalne**: brak
- **Request Body**: N/A (GET request)
- **Headers**:
  - `Authorization: Bearer {token}` (wymagany)

**Przykład żądania**:
```http
GET /api/v1/checkins?date=2025-12-07
Authorization: Bearer eyJhbGc...
```

## 3. Wykorzystywane typy

### Istniejące (do wykorzystania):
- `CheckinsByDateResponse` - DTO dla response'a (już zaimplementowany)

### Nowe do stworzenia:

**Query Model**:
```csharp
public sealed record GetCheckinsByDateQuery(string Date);
```

**Validator**:
```csharp
public sealed class GetCheckinsByDateQueryValidator : AbstractValidator<GetCheckinsByDateQuery>
{
    public GetCheckinsByDateQueryValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("Date parameter is required.")
            .Must(BeValidDate)
            .WithMessage("Date must be in YYYY-MM-DD format.");
    }

    private static bool BeValidDate(string date)
    {
        return DateOnly.TryParseExact(date, "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);
    }
}
```

**Handler**:
```csharp
public sealed class GetCheckinsByDateHandler
{
    private readonly HabitFlowDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public async Task<CheckinsByDateResponse> HandleAsync(
        GetCheckinsByDateQuery query,
        CancellationToken ct)
    {
        // Implementacja
    }
}
```

## 4. Szczegóły odpowiedzi

### Sukces (200 OK):
```json
{
  "date": "2025-12-07",
  "items": [
    {
      "id": 9876,
      "habitId": 101,
      "localDate": "2025-12-07",
      "actualValue": 7,
      "isPlanned": true
    }
  ]
}
```

### Błędy:

**400 Bad Request** - Nieprawidłowy format daty:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Date": ["Date must be in YYYY-MM-DD format."]
  }
}
```

**401 Unauthorized** - Brak lub nieprawidłowy token:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401
}
```

## 5. Przepływ danych

```
1. HTTP Request → Minimal API Endpoint
   ↓
2. Model Binding (GetCheckinsByDateQuery)
   ↓
3. FluentValidation (GetCheckinsByDateQueryValidator)
   ↓ (jeśli valid)
4. Handler (GetCheckinsByDateHandler)
   ↓
5. Pobranie UserId z HttpContext.User.GetUserId()
   ↓
6. Parse DateOnly z query.Date
   ↓
7. EF Core Query:
   SELECT Id, HabitId, LocalDate, ActualValue, IsPlanned
   FROM Checkins
   WHERE UserId = @userId AND LocalDate = @date
   ORDER BY HabitId
   (wykorzystuje indeks IX_Checkins_UserId_LocalDate_HabitId)
   ↓
8. Mapowanie encji → CheckinsByDateResponse.CheckinItem
   ↓
9. Utworzenie CheckinsByDateResponse
   ↓
10. Results.Ok(response)
```

**Szczegóły zapytania do bazy**:
- Filtrowanie: `UserId` (z kontekstu) + `LocalDate` (z query)
- Indeks: Clustered index `IX_Checkins_UserId_LocalDate_HabitId` zapewnia optymalne pokrycie
- Projekcja: Wybierane tylko wymagane kolumny (Id, HabitId, LocalDate, ActualValue, IsPlanned)
- Sortowanie: Opcjonalnie po HabitId dla przewidywalnej kolejności

## 6. Względy bezpieczeństwa

### Uwierzytelnianie:
- **Wymagane**: Endpoint musi być oznaczony `[Authorize]` lub `.RequireAuthorization()`
- **Mechanizm**: JWT Bearer token z ASP.NET Core Identity
- **Brak tokenu/nieprawidłowy token**: 401 Unauthorized

### Autoryzacja (Row-Level Security):
- **UserId z kontekstu**: Automatyczne pobieranie `UserId` z `HttpContext.User.GetUserId()`
- **Filtrowanie**: Query zawsze zawiera warunek `WHERE UserId = @currentUserId`
- **Ochrona**: Użytkownik nie może pobrać check-inów innych użytkowników
- **Denormalizacja**: Kolumna `Checkins.UserId` umożliwia efektywne RLS bez joina do Habits

### Walidacja danych wejściowych:
- **Format daty**: Ścisła walidacja YYYY-MM-DD przez FluentValidation
- **SQL Injection**: Parametryzowane zapytania EF Core (automatyczne)
- **XSS**: Brak ryzyka (endpoint zwraca JSON z wartościami numerycznymi/datami)

### Dodatkowe zabezpieczenia:
- **Rate limiting**: Rozważyć dla endpointów GET (wymaga konfiguracji middleware)
- **CORS**: Skonfigurowany dla dozwolonych origin w `Program.cs`

## 7. Obsługa błędów

| Scenariusz | Kod HTTP | Response | Logowanie |
|------------|----------|----------|-----------|
| Brak parametru `date` | 400 | ValidationProblemDetails | Info |
| Nieprawidłowy format daty | 400 | ValidationProblemDetails z komunikatem | Info |
| Brak tokenu uwierzytelnienia | 401 | ProblemDetails (middleware) | Warning |
| Nieprawidłowy/wygasły token | 401 | ProblemDetails (middleware) | Warning |
| Błąd połączenia z bazą | 500 | ProblemDetails (middleware) | Error |
| Timeout zapytania | 500 | ProblemDetails (middleware) | Error |
| Nieoczekiwany wyjątek | 500 | ProblemDetails (middleware) | Critical |

**Obsługa wyjątków**:
- FluentValidation automatycznie zwraca 400 z ValidationProblemDetails
- Middleware do obsługi globalnych wyjątków (już skonfigurowany w projekcie)
- Logowanie przez ILogger w handlerze dla błędów biznesowych/infrastrukturalnych

**Przykład logowania w handlerze**:
```csharp
try
{
    // Query execution
}
catch (DbException ex)
{
    _logger.LogError(ex,
        "Database error while fetching checkins for date {Date} and user {UserId}",
        query.Date, userId);
    throw; // Middleware obsłuży
}
```

## 8. Rozważania dotyczące wydajności

### Optymalizacje zapytań:
- **Indeks klastrowy**: `IX_Checkins_UserId_LocalDate_HabitId` zapewnia optymalne pokrycie dla WHERE + ORDER BY
- **Projekcja**: SELECT tylko wymaganych kolumn (Id, HabitId, LocalDate, ActualValue, IsPlanned)
- **AsNoTracking()**: Brak potrzeby śledzenia zmian dla read-only query
- **Paginacja**: Nie wymagana (jeden dzień to typowo ~10-50 check-inów)

### Caching:
- **Response caching**: Opcjonalnie dla stabilnych danych historycznych (> 1 dzień wstecz)
- **Distributed cache**: Redis dla środowiska produkcyjnego (wymaga konfiguracji)
- **Cache key**: `checkins:user:{userId}:date:{date}`
- **TTL**: 5-15 minut dla bieżącego dnia, dłużej dla danych historycznych

### Szacunki wydajności:
- **Rozmiar response**: ~200-500 bytes dla 10-50 check-inów
- **Czas zapytania**: < 10ms (z wykorzystaniem clustered index)
- **Throughput**: 1000+ req/s (bez cachingu)

### Potencjalne wąskie gardła:
- Brak - endpoint bardzo lekki i dobrze zindeksowany
- Monitorować wydajność dla użytkowników z 100+ nawykami (ekstremalny przypadek)

## 9. Etapy wdrożenia

### Krok 1: Utworzenie modelu Query i walidatora
**Plik**: `HabitFlow.Api/Features/Checkins/GetCheckinsByDate/GetCheckinsByDateQuery.cs`
```csharp
public sealed record GetCheckinsByDateQuery(string Date);
```

**Plik**: `HabitFlow.Api/Features/Checkins/GetCheckinsByDate/GetCheckinsByDateQueryValidator.cs`
- Implementacja FluentValidation
- Walidacja formatu YYYY-MM-DD
- Walidacja niepustego stringa

### Krok 2: Implementacja Handlera
**Plik**: `HabitFlow.Api/Features/Checkins/GetCheckinsByDate/GetCheckinsByDateHandler.cs`
- Dependency injection: `HabitFlowDbContext`, `IHttpContextAccessor`, `ILogger`
- Metoda `HandleAsync`:
  1. Pobranie UserId z `_httpContextAccessor.HttpContext.User.GetUserId()`
  2. Parse `DateOnly.ParseExact(query.Date, "yyyy-MM-dd")`
  3. Query EF Core z `.AsNoTracking()` i filtrami UserId + LocalDate
  4. Mapowanie do `CheckinsByDateResponse`
  5. Return response

### Krok 3: Dodanie endpointu w CheckinEndpoints
**Plik**: `HabitFlow.Api/Endpoints/CheckinEndpoints.cs`
- Dodać metodę `MapGetCheckinsByDate` (lub rozszerzyć istniejącą)
- Konfiguracja:
  ```csharp
  group.MapGet("/", async (
      [FromQuery] string date,
      GetCheckinsByDateHandler handler,
      CancellationToken ct) =>
  {
      var query = new GetCheckinsByDateQuery(date);
      var response = await handler.HandleAsync(query, ct);
      return Results.Ok(response);
  })
  .RequireAuthorization()
  .WithName("GetCheckinsByDate")
  .WithTags("Checkins")
  .Produces<CheckinsByDateResponse>(200)
  .ProducesProblem(400)
  .ProducesProblem(401);
  ```

### Krok 4: Rejestracja zależności
**Plik**: `HabitFlow.Api/Program.cs` (lub osobny plik DI)
- Zarejestrować `GetCheckinsByDateHandler` jako Scoped
- Zarejestrować `GetCheckinsByDateQueryValidator` w FluentValidation (jeśli nie auto-discovery)

### Krok 5: Testy jednostkowe
**Plik**: `HabitFlow.Tests/UnitTests/Features/Checkins/GetCheckinsByDateHandlerTests.cs`
- Test: Prawidłowe zwracanie check-inów dla daty
- Test: Pusta lista gdy brak check-inów
- Test: Filtrowanie tylko check-inów aktualnego użytkownika
- Test: Mapowanie wszystkich wymaganych pól

**Plik**: `HabitFlow.Tests/UnitTests/Features/Checkins/GetCheckinsByDateQueryValidatorTests.cs`
- Test: Walidacja przechodząca dla prawidłowego formatu
- Test: Błąd dla pustego date
- Test: Błąd dla nieprawidłowego formatu (DD/MM/YYYY, itp.)

### Krok 6: Testy integracyjne
**Plik**: `HabitFlow.Tests/IntegrationTests/Endpoints/CheckinEndpointsTests.cs`
- Test: GET /api/v1/checkins?date=2025-12-07 zwraca 200 z danymi
- Test: 400 dla nieprawidłowego formatu daty
- Test: 401 dla niezalogowanego użytkownika
- Test: Zwrócone check-iny należą tylko do zalogowanego użytkownika
- Konfiguracja: TestContainers + seeding testowych danych

### Krok 7: Dokumentacja OpenAPI
- Automatyczna generacja przez Minimal API z `.Produces()` i `.ProducesProblem()`
- Sprawdzić w Swagger UI (tryb Development) poprawność dokumentacji
- Opcjonalnie: Dodać XML komentarze dla lepszych opisów

### Krok 8: Code review i testy manualne
- Przegląd kodu przez zespół
- Testy manualne przez Swagger UI lub Postman
- Weryfikacja logów dla różnych scenariuszy
- Performance testing dla użytkowników z wieloma nawykami

---

**Kolejność implementacji**: Kroki 1-8 sekwencyjnie, z commit po każdym kroku dla łatwiejszego review.
