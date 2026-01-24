# Plan implementacji widoku Notifications

## 1. Przegląd

Widok Notifications ma na celu umożliwienie użytkownikom przeglądania historii powiadomień motywacyjnych generowanych przez AI, wyzwalanych przy zdarzeniu „miss due" (pominięcie zaplanowanego dnia nawyku). Widok prezentuje powiadomienia w formie paginowanej listy, z jasnym rozróżnieniem statusu AI (sukces, fallback, błąd), wyświetleniem tytułu nawyku, daty i treści powiadomienia. Użytkownik może przeglądać szczegóły powiadomień i nawigować między stronami.

## 2. Routing widoku

- **Ścieżka**: `/notifications`
- **Autoryzacja**: Wymagane zalogowanie (`@attribute [Authorize]`)
- **Tryb renderowania**: `InteractiveServer`
- **Tytuł strony**: "Powiadomienia - HabitFlow"

## 3. Struktura komponentów

```
Notifications.razor (główny widok, kontener, zarządzanie stanem)
├── NotificationsList.razor (prezentacja listy powiadomień)
│   ├── NotificationItem.razor (pojedynczy element powiadomienia)
│   └── MudPagination (komponent paginacji z MudBlazor)
└── EmptyState.razor (stan pusty gdy brak powiadomień)
```

**Istniejące komponenty do ponownego wykorzystania:**
- `NotificationsBell.razor` – dzwonek w top bar (już istnieje, wymaga rozbudowy o licznik)
- `EmptyState.razor` – komponent stanu pustego z CTA (z widoku Habits)

## 4. Szczegóły komponentów

### 4.1. Notifications.razor (główny widok)

**Opis komponentu:**
Główny widok strony `/notifications`, odpowiedzialny za zarządzanie stanem listy powiadomień, paginację, sortowanie oraz komunikację z API. Komponent ładuje dane przy inicjalizacji i reaguje na zmiany paginacji. Obsługuje stany ładowania, błędów oraz pusty stan (brak powiadomień).

**Główne elementy HTML i komponenty dzieci:**
- `MudContainer` z `MaxWidth.Large` – kontener główny
- `MudText Typo.h4` – nagłówek "Powiadomienia"
- `MudAlert` – komunikaty błędów (gdy `ErrorMessage != null`)
- `MudProgressLinear` – pasek postępu ładowania (opcjonalnie globalny)
- `NotificationsList` – lista powiadomień z paginacją
- `EmptyState` – widok stanu pustego (brak powiadomień)

**Obsługiwane zdarzenia:**
- `OnInitializedAsync()` – inicjalizacja i pierwsze załadowanie danych
- `HandlePageChanged(int newPage)` – zmiana strony paginacji
- `HandlePageSizeChanged(int newPageSize)` – zmiana rozmiaru strony
- `Dispose()` – anulowanie tokenów i czyszczenie zasobów

**Warunki walidacji:**
- Parametry paginacji: `page >= 1`, `pageSize` w zakresie `1-100` (walidowane przez API, frontend używa domyślnych wartości)
- Obsługa `401` (nieautoryzowany) – przekierowanie do `/auth/login?returnUrl=/notifications`
- Obsługa błędów API (wyświetlenie komunikatu w `MudAlert`)
- Obsługa anulowania requestów przy dispose/nowym żądaniu

**Typy (DTO i ViewModel):**
- `NotificationListState` (stan widoku)
- `NotificationListItemVm` (model widoku pojedynczego powiadomienia)
- `NotificationResponse` (DTO z API)
- `PagedResponse<NotificationResponse>` (odpowiedź paginowana z API)

**Propsy (interfejs komponentu):**
- Brak – komponent główny nie przyjmuje parametrów

---

### 4.2. NotificationsList.razor

**Opis komponentu:**
Komponent prezentacyjny odpowiedzialny za renderowanie listy powiadomień oraz kontrolek paginacji. Wyświetla loader w trakcie ładowania, stan pusty gdy brak elementów, oraz listę powiadomień z paginacją, gdy dane są dostępne.

**Główne elementy HTML i komponenty dzieci:**
- `MudStack Spacing="3"` – kontener listy
- `MudProgressLinear` (widoczny gdy `IsLoading == true`)
- `EmptyState` (widoczny gdy `Items == null || !Items.Any()` i `!IsLoading`)
- Pętla `@foreach` renderująca `NotificationItem` dla każdego elementu
- `MudPagination` – kontrolka paginacji (widoczna gdy `TotalCount > PageSize`)
- `MudText` – informacja o zakresie wyświetlanych elementów ("Wyświetlanie X-Y z Z powiadomień")
- `MudSelect` – wybór liczby elementów na stronę (10, 20, 50)

**Obsługiwane zdarzenia:**
- `HandlePageChanged(int newPage)` – wywołuje `OnPageChanged`
- `HandlePageSizeChanged(int newPageSize)` – wywołuje `OnPageSizeChanged`

**Warunki walidacji:**
- Brak walidacji wewnętrznej (komponent prezentacyjny)
- Warunek wyświetlania paginacji: `TotalCount > PageSize`

**Typy (DTO i ViewModel):**
- `NotificationListItemVm` (lista elementów)

**Propsy (interfejs komponentu):**
```csharp
[Parameter] public List<NotificationListItemVm>? Items { get; set; }
[Parameter] public int TotalCount { get; set; }
[Parameter] public int CurrentPage { get; set; } = 1;
[Parameter] public int PageSize { get; set; } = 20;
[Parameter] public bool IsLoading { get; set; }
[Parameter] public EventCallback<int> OnPageChanged { get; set; }
[Parameter] public EventCallback<int> OnPageSizeChanged { get; set; }
```

---

### 4.3. NotificationItem.razor

**Opis komponentu:**
Komponent prezentacyjny wyświetlający pojedyncze powiadomienie w formie karty. Pokazuje tytuł nawyku (jeśli dostępny z pamięci podręcznej lub jako ID), datę lokalną zdarzenia, treść powiadomienia oraz status AI (ikona + tooltip). Karta jest readonly – brak akcji edycji lub usuwania.

**Główne elementy HTML i komponenty dzieci:**
- `MudPaper Elevation="1" Class="pa-4 mb-2"` – karta powiadomienia
- `MudStack` – układ wertykalny treści
- `MudStack Row` – nagłówek (tytuł nawyku, data, status AI)
- `MudText Typo.h6` – tytuł nawyku lub "Nawyk #[HabitId]"
- `MudChip` – data lokalna w formacie `dd.MM.yyyy`
- `MudIcon` z `MudTooltip` – ikona statusu AI (sukces: zielony check, fallback: żółty warning, błąd: czerwony error)
- `MudText Typo.body1` – treść powiadomienia
- `MudText Typo.caption Color.Secondary` – data utworzenia (CreatedAtUtc) w formacie "dd.MM.yyyy HH:mm"

**Obsługiwane zdarzenia:**
- Brak (komponent readonly)

**Warunki walidacji:**
- Brak walidacji (komponent prezentacyjny)

**Typy (DTO i ViewModel):**
- `NotificationListItemVm` (pojedynczy element)

**Propsy (interfejs komponentu):**
```csharp
[Parameter, EditorRequired] public NotificationListItemVm Item { get; set; } = null!;
```

---

### 4.4. EmptyState.razor

**Opis komponentu:**
Komponent stanu pustego wyświetlany, gdy użytkownik nie ma jeszcze żadnych powiadomień. Prezentuje przyjazny komunikat i ikonę informującą o braku danych, z opcjonalnym linkiem do ekranu „Dziś".

**Główne elementy HTML i komponenty dzieci:**
- `MudPaper Class="pa-8 mt-4" Elevation="0"` – kontener stanu pustego
- `MudStack Spacing="4" AlignItems="AlignItems.Center"` – układ wertykalny
- `MudIcon` – ikona (np. `Icons.Material.Outlined.NotificationsNone`)
- `MudText Typo.h5` – nagłówek "Brak powiadomień"
- `MudText Typo.body1 Color.Secondary` – opis "Powiadomienia pojawią się tutaj, gdy pominiesz zaplanowany dzień nawyku."
- `MudButton Variant.Filled Color.Primary Href="/today"` – przycisk CTA "Przejdź do Dziś"

**Obsługiwane zdarzenia:**
- Brak (komponent statyczny)

**Warunki walidacji:**
- Brak

**Typy:**
- Brak

**Propsy:**
- Brak (komponent statyczny, użyty wewnątrz `NotificationsList`)

---

### 4.5. NotificationsBell.razor (rozbudowa istniejącego)

**Opis komponentu:**
Komponent dzwonka powiadomień w top bar, wyświetlający badge z liczbą nieprzeczytanych powiadomień (opcjonalnie w MVP). W ramach tego planu, komponent pozostaje jako link do `/notifications` z placeholder dla licznika (TODO w kodzie).

**Główne elementy HTML i komponenty dzieci:**
- `MudIconButton` z ikoną `Icons.Material.Filled.Notifications`
- `MudBadge` (opcjonalnie, gdy `_unreadCount > 0`)

**Obsługiwane zdarzenia:**
- Brak (link, brak logiki licznika w MVP)

**Warunki walidacji:**
- Brak w MVP (licznik nieprzeczytanych poza zakresem)

**Typy:**
- Brak w MVP

**Propsy:**
- Brak

---

## 5. Typy

### 5.1. DTO (Data Transfer Objects) z API

#### `PagedResponse<T>` (istniejący)
```csharp
public record PagedResponse<T>(
    int TotalCount,
    IReadOnlyList<T> Items
);
```

#### `NotificationResponse` (istniejący)
```csharp
public record NotificationResponse(
    long Id,             // ID powiadomienia
    int HabitId,         // ID powiązanego nawyku
    DateOnly LocalDate,  // Data lokalna zdarzenia miss due
    int Type,            // Typ powiadomienia (1 = MissDue)
    string Content,      // Treść wiadomości (wygenerowana przez AI lub fallback)
    int? AiStatus,       // Status AI (1=Success, 2=Fallback, 3=Error, null jeśli brak)
    DateTimeOffset CreatedAtUtc // Data utworzenia w UTC
);
```

#### `NotificationDetailResponse` (istniejący, opcjonalny w MVP)
```csharp
public record NotificationDetailResponse(
    long Id,
    int HabitId,
    string HabitName,     // Nazwa nawyku (dostępna w GET /api/v1/notifications/{id})
    DateOnly LocalDate,
    int Type,
    string Content,
    int? AiStatus,
    DateTimeOffset CreatedAtUtc
);
```

### 5.2. ViewModel (modele widoku) – nowe typy

#### `NotificationListState` (stan widoku)
```csharp
namespace HabitFlow.Blazor.Components.Pages.Notifications.Models;

public sealed class NotificationListState
{
    public bool IsLoading { get; set; }
    public string? ErrorMessage { get; set; }
    public List<NotificationListItemVm> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public NotificationSortField SortField { get; set; } = NotificationSortField.CreatedAtUtc;
    public SortDirection SortDirection { get; set; } = SortDirection.Desc;
}
```

**Pola:**
- `IsLoading` (bool) – czy trwa ładowanie danych
- `ErrorMessage` (string?) – komunikat błędu (null jeśli brak)
- `Items` (List<NotificationListItemVm>) – lista powiadomień do wyświetlenia
- `TotalCount` (int) – łączna liczba powiadomień (dla paginacji)
- `CurrentPage` (int) – aktualna strona (domyślnie 1)
- `PageSize` (int) – rozmiar strony (domyślnie 20)
- `SortField` (NotificationSortField) – pole sortowania (domyślnie CreatedAtUtc)
- `SortDirection` (SortDirection) – kierunek sortowania (domyślnie Desc)

#### `NotificationListItemVm` (element listy)
```csharp
namespace HabitFlow.Blazor.Components.Pages.Notifications.Models;

public sealed class NotificationListItemVm
{
    public long Id { get; set; }
    public int HabitId { get; set; }
    public string? HabitTitle { get; set; }  // Opcjonalnie z cache lub null
    public DateOnly LocalDate { get; set; }
    public NotificationType Type { get; set; }
    public string TypeLabel { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public AiGenerationStatus? AiStatus { get; set; }
    public string? AiStatusLabel { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
```

**Pola:**
- `Id` (long) – ID powiadomienia
- `HabitId` (int) – ID powiązanego nawyku
- `HabitTitle` (string?) – tytuł nawyku (opcjonalnie z cache, domyślnie null – wyświetla "Nawyk #[ID]")
- `LocalDate` (DateOnly) – data lokalna miss due
- `Type` (NotificationType) – typ powiadomienia (enum: MissDue = 1)
- `TypeLabel` (string) – label typu ("Miss Due")
- `Content` (string) – treść powiadomienia
- `AiStatus` (AiGenerationStatus?) – status AI (enum: Success=1, Fallback=2, Error=3, null jeśli brak)
- `AiStatusLabel` (string?) – label statusu AI ("Sukces AI", "Fallback", "Błąd AI", null)
- `CreatedAtUtc` (DateTimeOffset) – data utworzenia

### 5.3. Enumy (istniejące, z API/Core)

#### `NotificationType` (z HabitFlow.Data.Enums)
```csharp
public enum NotificationType : byte
{
    MissDue = 1
}
```

#### `AiGenerationStatus` (z HabitFlow.Data.Enums)
```csharp
public enum AiGenerationStatus : byte
{
    Success = 1,
    Fallback = 2,
    Error = 3
}
```

#### `NotificationSortField` (z HabitFlow.Core.Features.Notifications)
```csharp
public enum NotificationSortField
{
    CreatedAtUtc,
    LocalDate,
    Type
}
```

#### `SortDirection` (z HabitFlow.Core.Common)
```csharp
public enum SortDirection
{
    Asc,
    Desc
}
```

### 5.4. Metody mapowania (extension methods)

#### `NotificationMappingExtensions` (nowy plik)
```csharp
namespace HabitFlow.Blazor.Components.Pages.Notifications.Helpers;

public static class NotificationMappingExtensions
{
    public static NotificationListItemVm ToListItemVm(this NotificationResponse response)
    {
        return new NotificationListItemVm
        {
            Id = response.Id,
            HabitId = response.HabitId,
            HabitTitle = null, // Cache opcjonalny w rozbudowie
            LocalDate = response.LocalDate,
            Type = (NotificationType)response.Type,
            TypeLabel = GetTypeLabel((NotificationType)response.Type),
            Content = response.Content,
            AiStatus = response.AiStatus.HasValue
                ? (AiGenerationStatus)response.AiStatus.Value
                : null,
            AiStatusLabel = response.AiStatus.HasValue
                ? GetAiStatusLabel((AiGenerationStatus)response.AiStatus.Value)
                : null,
            CreatedAtUtc = response.CreatedAtUtc
        };
    }

    private static string GetTypeLabel(NotificationType type) => type switch
    {
        NotificationType.MissDue => "Miss Due",
        _ => type.ToString()
    };

    private static string GetAiStatusLabel(AiGenerationStatus status) => status switch
    {
        AiGenerationStatus.Success => "Sukces AI",
        AiGenerationStatus.Fallback => "Szablon",
        AiGenerationStatus.Error => "Błąd AI",
        _ => status.ToString()
    };
}
```

---

## 6. Zarządzanie stanem

**Podejście:**
- Stan widoku zarządzany lokalnie w komponencie `Notifications.razor` poprzez obiekt `NotificationListState` (instancja prywatna, bez dedykowanego serwisu State w MVP).
- Brak dedykowanego serwisu `NotificationsService` – komunikacja z API odbywa się bezpośrednio poprzez `IHabitFlowApiClient` (wygenerowany klient NSwag).
- Stan jest synchroniczny (nie współdzielony między komponentami) i resetowany przy każdym nowym ładowaniu danych.
- Paginacja i sortowanie zarządzane przez parametry `CurrentPage`, `PageSize`, `SortField`, `SortDirection` w stanie.

**Przepływ danych:**
1. `OnInitializedAsync()` → wywołanie `LoadNotificationsAsync()` z domyślnymi parametrami paginacji i sortowania.
2. `LoadNotificationsAsync()` → wywołanie `ApiClient.GetNotificationsAsync(page, pageSize, sortField, sortDirection)`.
3. Odpowiedź z API mapowana na `NotificationListItemVm` i zapisana w `_state.Items`.
4. Zmiana strony/rozmiaru → aktualizacja stanu i ponowne wywołanie `LoadNotificationsAsync()`.
5. Błędy API obsługiwane poprzez `_state.ErrorMessage` i wyświetlane w `MudAlert`.
6. Token anulowania (`CancellationTokenSource`) używany do anulowania requestów przy dispose lub nowym żądaniu.

**Czy wymagany customowy hook?**
- Nie. Zarządzanie stanem odbywa się w ramach komponentu `Notifications.razor` bez dedykowanego hooka czy serwisu State (podobnie jak w widoku Habits).

---

## 7. Integracja API

**Endpoint:**
- `GET /api/v1/notifications`

**Parametry żądania:**
- `page` (int?, opcjonalny, domyślnie 1) – numer strony
- `pageSize` (int?, opcjonalny, domyślnie 20) – liczba elementów na stronie (zakres 1-100)
- `sortField` (NotificationSortField?, opcjonalny, domyślnie CreatedAtUtc) – pole sortowania
- `sortDirection` (SortDirection?, opcjonalny, domyślnie Desc) – kierunek sortowania

**Typ odpowiedzi (200 OK):**
```csharp
PagedResponse<NotificationResponse>
```

**Przykład wywołania w komponencie:**
```csharp
var response = await ApiClient.GetNotificationsAsync(
    page: _state.CurrentPage,
    pageSize: _state.PageSize,
    sortField: _state.SortField,
    sortDirection: _state.SortDirection,
    cancellationToken: token
);

_state.Items = response.Items.Select(n => n.ToListItemVm()).ToList();
_state.TotalCount = response.TotalCount;
```

**Obsługa błędów:**
- `401 Unauthorized` → przekierowanie do `/auth/login?returnUrl=/notifications`
- `400 Bad Request` → wyświetlenie komunikatu błędu w `MudAlert`
- `500 Internal Server Error` → ogólny komunikat "Wystąpił nieoczekiwany błąd. Spróbuj odświeżyć stronę."
- `OperationCanceledException` → ignorowane (anulowanie requestu)

**Endpoint opcjonalny (szczegóły powiadomienia, MVP może pominąć):**
- `GET /api/v1/notifications/{id}` → zwraca `NotificationDetailResponse` z `HabitName`

---

## 8. Interakcje użytkownika

### 8.1. Przeglądanie listy powiadomień
- **Akcja:** Użytkownik wchodzi na `/notifications`
- **Wynik:** System ładuje pierwszą stronę powiadomień (20 elementów, sortowanie CreatedAtUtc Desc) i wyświetla listę. Każde powiadomienie pokazuje tytuł nawyku (lub ID), datę, treść i status AI.

### 8.2. Zmiana strony paginacji
- **Akcja:** Użytkownik klika na inny numer strony w kontrolce `MudPagination`
- **Wynik:** System wywołuje `HandlePageChanged(newPage)`, aktualizuje `_state.CurrentPage` i ładuje nową stronę powiadomień. Podczas ładowania wyświetlany jest `MudProgressLinear`.

### 8.3. Zmiana rozmiaru strony
- **Akcja:** Użytkownik wybiera inną wartość w `MudSelect` (10, 20, 50)
- **Wynik:** System wywołuje `HandlePageSizeChanged(newPageSize)`, aktualizuje `_state.PageSize`, resetuje `_state.CurrentPage` do 1 i ładuje pierwszą stronę z nowym rozmiarem.

### 8.4. Brak powiadomień (stan pusty)
- **Akcja:** Użytkownik wchodzi na `/notifications`, a lista jest pusta
- **Wynik:** System wyświetla komponent `EmptyState` z komunikatem "Brak powiadomień" i przyciskiem CTA "Przejdź do Dziś".

### 8.5. Błąd API
- **Akcja:** System nie może załadować powiadomień (np. błąd serwera)
- **Wynik:** Wyświetlany jest `MudAlert` z komunikatem błędu i użytkownik może odświeżyć stronę.

### 8.6. Nieautoryzowany dostęp (401)
- **Akcja:** Użytkownik traci sesję lub próbuje dostać się bez logowania
- **Wynik:** System przekierowuje do `/auth/login?returnUrl=/notifications`.

---

## 9. Warunki i walidacja

### 9.1. Warunki weryfikowane przez API (backend)
- **Paginacja:**
  - `page >= 1` (clampowane do 1 jeśli mniejsze)
  - `pageSize` w zakresie `1-100` (clampowane)
- **Autoryzacja:**
  - Endpoint wymaga autoryzacji (`[Authorize]`)
  - Powiadomienia filtrowane po `UserId` użytkownika zalogowanego (security w handlerze)
- **Walidacja ID nawyku:**
  - Endpoint nie waliduje, czy nawyk istnieje (powiadomienia mogą istnieć dla usuniętych nawyków)

### 9.2. Warunki weryfikowane przez frontend (UI)
- **Brak bezpośredniej walidacji inputów użytkownika** – paginacja i rozmiar strony kontrolowane przez UI (wybór z listy, przyciski).
- **Obsługa stanów:**
  - `IsLoading` – wyświetla loader w trakcie ładowania
  - `ErrorMessage != null` – wyświetla alert z błędem
  - `Items.Count == 0 && !IsLoading` – wyświetla `EmptyState`
- **Anulowanie requestów:**
  - Token anulowania (`CancellationTokenSource`) przy dispose lub nowym żądaniu, aby zapobiec race conditions i niepotrzebnym requestom.

### 9.3. Wpływ warunków na stan UI
- **Podczas ładowania (`IsLoading == true`):**
  - Kontrolki paginacji (`MudPagination`, `MudSelect`) są disabled
  - Wyświetlany jest `MudProgressLinear`
- **Gdy brak danych (`Items.Count == 0` i `!IsLoading`):**
  - Komponent `EmptyState` zastępuje listę
  - Paginacja nie jest wyświetlana
- **Gdy błąd (`ErrorMessage != null`):**
  - `MudAlert` z komunikatem błędu nad listą
  - Użytkownik może odświeżyć stronę (F5) lub spróbować ponownie po zamknięciu alertu
- **Gdy 401 (brak autoryzacji):**
  - Automatyczne przekierowanie do `/auth/login?returnUrl=/notifications`

---

## 10. Obsługa błędów

### 10.1. Błędy API

| Kod błędu | Znaczenie | Obsługa w UI |
|-----------|-----------|--------------|
| 401 Unauthorized | Brak sesji lub wygasła | Przekierowanie do `/auth/login?returnUrl=/notifications` |
| 400 Bad Request | Błędne parametry paginacji | Alert: "Błąd podczas ładowania powiadomień: [message]" |
| 404 Not Found | Endpoint nie istnieje (nieoczekiwane) | Alert: "Wystąpił nieoczekiwany błąd. Spróbuj odświeżyć stronę." |
| 500 Internal Server Error | Błąd serwera | Alert: "Wystąpił nieoczekiwany błąd. Spróbuj odświeżyć stronę." |
| OperationCanceledException | Anulowanie requestu | Brak akcji (ignorowane) |

### 10.2. Przypadki brzegowe

| Przypadek | Obsługa |
|-----------|---------|
| Pusta lista powiadomień | Wyświetlenie `EmptyState` z komunikatem i przyciskiem CTA do `/today` |
| Usunięty nawyk | Powiadomienie nadal wyświetlane, `HabitTitle` pokazuje "Nawyk #[ID]" |
| Brak statusu AI (`AiStatus == null`) | Brak ikony statusu AI w `NotificationItem` |
| Bardzo długa treść powiadomienia | Treść wyświetlana w pełnej długości (bez truncate w MVP, możliwe rozwinięcie "czytaj więcej" w rozbudowie) |
| Bardzo duża liczba powiadomień (>1000) | Paginacja zapewnia płynną obsługę, API zwraca max 100 na stronę |
| Race condition (kilka requestów jednocześnie) | Token anulowania (`_cts?.Cancel()`) anuluje poprzedni request przed rozpoczęciem nowego |

### 10.3. Graceful degradation
- Jeśli API nie zwróci `AiStatus`, ikona statusu AI nie jest wyświetlana.
- Jeśli brak tytułu nawyku (brak cache), wyświetlany jest "Nawyk #[ID]".
- Jeśli paginacja zwróci stronę poza zakresem (np. page=999, a jest 10 stron), API zwróci pustą listę – frontend wyświetli `EmptyState`.

---

## 11. Kroki implementacji

### Krok 1: Utworzenie struktury folderów i plików
- Utworzyć folder `HabitFlow.Blazor/Components/Pages/Notifications/Models/`
- Utworzyć folder `HabitFlow.Blazor/Components/Pages/Notifications/Helpers/`
- Przygotować pliki:
  - `Notifications.razor` (główny widok)
  - `Notifications.razor.cs` (code-behind)
  - `NotificationsList.razor` (lista)
  - `NotificationItem.razor` (element listy)
  - `Models/NotificationListState.cs` (stan widoku)
  - `Models/NotificationListItemVm.cs` (model widoku)
  - `Helpers/NotificationMappingExtensions.cs` (metody mapowania)

### Krok 2: Zdefiniowanie modeli widoku (ViewModel)
- Zaimplementować `NotificationListState` zgodnie z sekcją 5.2.
- Zaimplementować `NotificationListItemVm` zgodnie z sekcją 5.2.
- Dodać enumy i aliasy typów (jeśli potrzebne) w namespace `HabitFlow.Blazor.Components.Pages.Notifications.Models`.

### Krok 3: Zaimplementowanie metod mapowania
- Zaimplementować `NotificationMappingExtensions` w pliku `Helpers/NotificationMappingExtensions.cs`.
- Dodać metody:
  - `ToListItemVm(this NotificationResponse response)` – mapowanie z DTO na ViewModel
  - `GetTypeLabel(NotificationType type)` – mapowanie typu na label
  - `GetAiStatusLabel(AiGenerationStatus status)` – mapowanie statusu AI na label

### Krok 4: Implementacja komponentu `NotificationItem.razor`
- Zaimplementować layout karty powiadomienia zgodnie z sekcją 4.3.
- Dodać renderowanie:
  - Nagłówka z tytułem nawyku (lub "Nawyk #[ID]")
  - Daty lokalnej (`LocalDate`) w formacie `dd.MM.yyyy`
  - Ikony statusu AI (jeśli `AiStatus != null`) z tooltipem
  - Treści powiadomienia (`Content`)
  - Daty utworzenia (`CreatedAtUtc`) w formacie `dd.MM.yyyy HH:mm`
- Dodać metody pomocnicze:
  - `GetAiStatusIcon(AiGenerationStatus? status)` – zwraca ikonę MudBlazor
  - `GetAiStatusColor(AiGenerationStatus? status)` – zwraca kolor MudBlazor

### Krok 5: Implementacja komponentu `NotificationsList.razor`
- Zaimplementować layout listy zgodnie z sekcją 4.2.
- Dodać renderowanie:
  - Loadera (`MudProgressLinear`) gdy `IsLoading == true`
  - `EmptyState` gdy `Items == null || !Items.Any()` i `!IsLoading`
  - Pętli `@foreach` renderującej `NotificationItem` dla każdego elementu
  - Kontrolki paginacji (`MudPagination`) gdy `TotalCount > PageSize`
  - Informacji o zakresie wyświetlanych elementów
  - `MudSelect` do wyboru liczby elementów na stronę (10, 20, 50)
- Zaimplementować metody obsługi zdarzeń:
  - `HandlePageChanged(int newPage)`
  - `HandlePageSizeChanged(int newPageSize)`
- Dodać właściwość obliczaną `TotalPages` (liczba stron paginacji).

### Krok 6: Implementacja komponentu `Notifications.razor` (główny widok)
- Zaimplementować layout głównego widoku zgodnie z sekcją 4.1.
- Dodać renderowanie:
  - Nagłówka "Powiadomienia"
  - `MudAlert` gdy `ErrorMessage != null`
  - `NotificationsList` z przekazaniem propsów
- Zaimplementować code-behind w `Notifications.razor.cs`:
  - Inicjalizacja stanu `_state = new NotificationListState()`
  - Implementacja `OnInitializedAsync()` → wywołanie `LoadNotificationsAsync()`
  - Implementacja `LoadNotificationsAsync()`:
    - Anulowanie poprzedniego requestu (`_cts?.Cancel()`)
    - Utworzenie nowego `CancellationTokenSource`
    - Wywołanie `ApiClient.GetNotificationsAsync(...)` z parametrami z `_state`
    - Mapowanie odpowiedzi na `NotificationListItemVm` i zapis w `_state.Items`
    - Obsługa błędów (401, ApiException, Exception)
  - Implementacja `HandlePageChanged(int newPage)` → aktualizacja `_state.CurrentPage` i wywołanie `LoadNotificationsAsync()`
  - Implementacja `HandlePageSizeChanged(int newPageSize)` → aktualizacja `_state.PageSize`, reset `_state.CurrentPage` do 1, wywołanie `LoadNotificationsAsync()`
  - Implementacja `Dispose()` → anulowanie i dispose `_cts`
- Dodać dyrektywy:
  - `@page "/notifications"`
  - `@attribute [Authorize]`
  - `@rendermode InteractiveServer`
  - `@inject IHabitFlowApiClient ApiClient`
  - `@inject NavigationManager Navigation`
  - `@implements IDisposable`

### Krok 7: Ponowne użycie `EmptyState.razor`
- Sprawdzić, czy istniejący komponent `EmptyState.razor` (z widoku Habits) jest generyczny i może być użyty ponownie.
- Jeśli tak, użyć go w `NotificationsList.razor`.
- Jeśli nie, stworzyć dedykowany `NotificationsEmptyState.razor` z odpowiednim komunikatem i CTA.

### Krok 8: Testowanie integracji z API
- Uruchomić aplikację (`dotnet run --project HabitFlow.Blazor`).
- Zalogować się i przejść do `/notifications`.
- Sprawdzić:
  - Czy dane są poprawnie ładowane z API
  - Czy paginacja działa (zmiana strony, zmiana rozmiaru strony)
  - Czy komunikaty błędów są wyświetlane poprawnie
  - Czy stan pusty jest wyświetlany, gdy brak powiadomień
  - Czy przekierowanie 401 działa poprawnie

### Krok 9: Stylowanie i UX
- Dodać opcjonalne style CSS (jeśli potrzebne) w pliku `Notifications.razor.css`.
- Sprawdzić responsywność na urządzeniach mobilnych (dopasowanie MudBlazor do mobile-first).
- Dostosować kolory, ikony i spacing zgodnie z istniejącym designem aplikacji.

### Krok 10: Rozbudowa `NotificationsBell.razor` (opcjonalne, poza MVP)
- Dodać logikę pobierania liczby nieprzeczytanych powiadomień z API.
- Zaktualizować badge dzwonka w top bar z rzeczywistą liczbą.
- Rozważyć periodyczne odświeżanie licznika (np. co 30 sekund, SignalR w przyszłości).

### Krok 11: Przegląd kodu i refaktoring
- Przejrzeć kod pod kątem zgodności z konwencjami projektu (PascalCase, file-scoped namespaces, expression-bodied members).
- Upewnić się, że wszystkie komponenty mają odpowiednie komentarze XML (opcjonalnie).
- Zweryfikować, że nie ma duplikacji kodu i że metody mapowania są reużywalne.

### Krok 12: Testy (zgodnie z .ai/test-plan.md)
- Napisać testy jednostkowe dla metod mapowania (`NotificationMappingExtensions`).
- Napisać testy komponentów Blazor (bUnit) dla:
  - `NotificationItem.razor` – renderowanie elementu
  - `NotificationsList.razor` – renderowanie listy, paginacji, stanu pustego
  - `Notifications.razor` – ładowanie danych, obsługa błędów, zmiana strony
- Napisać testy integracyjne dla endpointu `GET /api/v1/notifications` (TestContainers + NSwag).
- Rozważyć test E2E (Playwright) dla ścieżki: logowanie → miss due → przeglądanie powiadomień.

### Krok 13: Dokumentacja
- Zaktualizować `.ai/ui-plan.md` z informacją o pełnej implementacji widoku Notifications.
- Dodać komentarze w kodzie wyjaśniające złożone logiki (jeśli występują).
- Opcjonalnie: stworzyć zrzuty ekranu widoku dla dokumentacji użytkownika.

### Krok 14: Commit i PR
- Utworzyć commit: `feat(blazor): implement Notifications view with pagination`
- Opcjonalnie: utworzyć PR i połączyć z issue (np. `Closes #XYZ`).
- Dodać opis PR z celem, krokami testowymi i zrzutami ekranu UI.
- Upewnić się, że CI/CD przechodzi (build, testy, formatowanie).

---

## Uwagi końcowe

- **Kolejność renderowania:** Kontrolki paginacji są disabled podczas ładowania (`IsLoading == true`), aby uniknąć race conditions.
- **Cache tytułów nawyków:** W MVP `HabitTitle` w `NotificationListItemVm` jest null. Rozbudowa może dodać cache lokalny lub dedykowane zapytanie do API (GET /api/v1/notifications/{id} zwraca `HabitName`).
- **Infinite scroll vs paginacja:** Zgodnie z PRD i ui-plan, używamy paginacji (`MudPagination`), nie infinite scroll, dla lepszej kontroli użytkownika i wydajności.
- **Licznik nieprzeczytanych:** W MVP dzwonek (`NotificationsBell.razor`) ma placeholder dla licznika. Rozbudowa wymaga dodania flagi `IsRead` w modelu `Notification` i endpointu do oznaczania jako przeczytane.
- **Sortowanie:** W MVP sortowanie jest hardcoded na `CreatedAtUtc Desc`. Rozbudowa może dodać UI do wyboru sortowania (dropdown z opcjami).
- **Bezpieczeństwo:** API zabezpiecza dostęp do powiadomień po `UserId` użytkownika zalogowanego. Frontend dodatkowo wymaga `[Authorize]` i obsługuje 401.
