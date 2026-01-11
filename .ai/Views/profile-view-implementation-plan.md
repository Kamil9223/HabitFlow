# Plan implementacji widoku Profile

## 1. Przegląd

Widok Profile to strona zarządzania profilem użytkownika, która umożliwia przeglądanie danych konta (email, data utworzenia, strefa czasowa, liczba nawyków) oraz wykonywanie dwóch kluczowych operacji: zmianę strefy czasowej oraz trwałe usunięcie konta. Widok zapewnia bezpieczne potwierdzenie przed usunięciem konta poprzez wymaganie wpisania tekstu "DELETE" oraz informuje użytkownika o fakcie, że zmiana strefy czasowej zacznie obowiązywać od następnej doby lokalnej.

Widok jest zgodny z wymaganiami F-002 (zarządzanie strefą czasu) i F-010 (usuwanie danych) oraz realizuje historyjki użytkownika US-004 (ustawienie strefy czasu) i US-019 (usunięcie konta).

## 2. Routing widoku

**Ścieżka:** `/profile`

**Wymagania dostępu:**
- Użytkownik musi być uwierzytelniony (RequireAuthorization)
- Widok dostępny po zalogowaniu, w ramach głównego App Shell

**Nawigacja:**
- Widok dostępny z głównego menu nawigacyjnego (sidebar/drawer)
- Link nawigacyjny w top bar lub bottom navigation (mobile)

## 3. Struktura komponentów

```
Profile.razor (główny widok, strona)
├── ProfileSummary (komponent prezentacyjny)
│   └── Wyświetla informacje o użytkowniku (read-only)
├── TimeZoneEditor (komponent do edycji strefy czasu)
│   ├── MudSelect<string> (dropdown ze strefami czasu)
│   └── MudButton (przycisk zapisania)
└── DeleteAccountSection (komponent usuwania konta)
    ├── MudButton (przycisk "Usuń konto" - trigger dialoga)
    └── ConfirmDeleteAccountDialog (komponent modalny)
        ├── MudTextField (pole do wpisania "DELETE")
        └── MudButton x2 (Anuluj, Potwierdź)
```

**Hierarchia renderowania:**
1. Profile.razor jako root page component (@page "/profile")
2. Wykorzystanie MudGrid/MudContainer do layoutu
3. Sekcje rozdzielone wizualnie (np. MudPaper/MudCard)

## 4. Szczegóły komponentów

### Profile.razor (Główny widok)

**Opis komponentu:**
Główny komponent strony, odpowiedzialny za orkiestrację widoku profilu. Pobiera dane użytkownika przy inicjalizacji, zarządza stanem ładowania i błędów, oraz koordynuje komunikację między komponentami dziecięcymi a API. Implementowany jako strona Razor (@page directive) z dyrektywą @attribute [Authorize].

**Główne elementy HTML i komponenty dzieci:**
```razor
<PageTitle>Profil - HabitFlow</PageTitle>

<MudContainer MaxWidth="MaxWidth.Medium" Class="pa-4">
    <!-- Nagłówek strony -->
    <MudText Typo="Typo.h4" Class="mb-4">Profil użytkownika</MudText>

    @if (_isLoading)
    {
        <MudProgressCircular Color="Color.Primary" Indeterminate="true" />
    }
    else if (_profile != null)
    {
        <!-- Sekcja podsumowania profilu -->
        <MudPaper Class="pa-4 mb-4">
            <ProfileSummary Profile="_profile" />
        </MudPaper>

        <!-- Sekcja edycji strefy czasowej -->
        <MudPaper Class="pa-4 mb-4">
            <TimeZoneEditor
                CurrentTimeZoneId="_profile.TimeZoneId"
                OnTimeZoneChanged="HandleTimeZoneChanged" />
        </MudPaper>

        <!-- Sekcja usuwania konta -->
        <MudPaper Class="pa-4">
            <DeleteAccountSection OnAccountDeleted="HandleAccountDeleted" />
        </MudPaper>
    }
    else if (_errorMessage != null)
    {
        <MudAlert Severity="Severity.Error">@_errorMessage</MudAlert>
    }
</MudContainer>
```

**Obsługiwane zdarzenia:**
- `OnInitializedAsync` - pobranie danych profilu z API
- `HandleTimeZoneChanged` - callback po zmianie strefy czasowej, odświeża dane profilu
- `HandleAccountDeleted` - callback po usunięciu konta, przekierowanie do logout

**Warunki walidacji:**
- Brak walidacji na poziomie tego komponentu (delegowana do komponentów dzieci)
- Walidacja autoryzacji na poziomie routingu

**Typy:**
- `ProfileViewModel` - model widoku dla danych profilu
- `string?` dla `_errorMessage`
- `bool` dla `_isLoading`

**Propsy:**
Brak - komponent root page, nie przyjmuje propsów.

---

### ProfileSummary (Komponent prezentacyjny)

**Opis komponentu:**
Prosty komponent prezentacyjny wyświetlający dane użytkownika w formie read-only. Pokazuje email, datę utworzenia konta, aktualną strefę czasową oraz liczbę nawyków. Wykorzystuje komponenty MudBlazor do wyświetlania danych w uporządkowany sposób.

**Główne elementy HTML i komponenty dzieci:**
```razor
<MudText Typo="Typo.h6" Class="mb-3">Informacje o koncie</MudText>

<MudStack Spacing="2">
    <MudField Label="Email" Variant="Variant.Text">
        @Profile.Email
    </MudField>

    <MudField Label="Status weryfikacji" Variant="Variant.Text">
        @if (Profile.EmailConfirmed)
        {
            <MudChip Color="Color.Success" Size="Size.Small">Zweryfikowany</MudChip>
        }
        else
        {
            <MudChip Color="Color.Warning" Size="Size.Small">Niezweryfikowany</MudChip>
        }
    </MudField>

    <MudField Label="Data utworzenia" Variant="Variant.Text">
        @Profile.CreatedAtUtc.ToString("dd.MM.yyyy HH:mm")
    </MudField>

    <MudField Label="Aktualna strefa czasowa" Variant="Variant.Text">
        @Profile.TimeZoneId
    </MudField>

    <MudField Label="Liczba nawyków" Variant="Variant.Text">
        @Profile.HabitsCount
    </MudField>
</MudStack>
```

**Obsługiwane zdarzenia:**
Brak - komponent prezentacyjny bez interakcji.

**Warunki walidacji:**
Brak - tylko wyświetlanie danych.

**Typy:**
- `ProfileViewModel` - dane profilu do wyświetlenia

**Propsy:**
```csharp
[Parameter]
[EditorRequired]
public ProfileViewModel Profile { get; set; } = null!;
```

---

### TimeZoneEditor (Komponent edycji strefy czasowej)

**Opis komponentu:**
Komponent umożliwiający zmianę strefy czasowej użytkownika. Składa się z dropdownu (MudSelect) zawierającego listę dostępnych stref czasowych IANA oraz przycisku zapisania zmian. Po zapisaniu pokazuje komunikat sukcesu oraz informację, że zmiana zacznie obowiązywać od następnej doby. Obsługuje stany ładowania i błędów.

**Główne elementy HTML i komponenty dzieci:**
```razor
<MudText Typo="Typo.h6" Class="mb-3">Strefa czasowa</MudText>

<MudForm @ref="_form">
    <MudStack Spacing="3">
        <MudSelect
            @bind-Value="_selectedTimeZoneId"
            Label="Wybierz strefę czasową"
            Variant="Variant.Outlined"
            Required="true"
            RequiredError="Strefa czasowa jest wymagana">
            @foreach (var tz in _availableTimeZones)
            {
                <MudSelectItem Value="@tz.Id">@tz.DisplayName</MudSelectItem>
            }
        </MudSelect>

        <MudAlert Severity="Severity.Info" Dense="true">
            Zmiana strefy czasowej zacznie obowiązywać od następnej doby lokalnej.
        </MudAlert>

        @if (_successMessage != null)
        {
            <MudAlert Severity="Severity.Success" Dense="true">@_successMessage</MudAlert>
        }

        @if (_errorMessage != null)
        {
            <MudAlert Severity="Severity.Error" Dense="true">@_errorMessage</MudAlert>
        }

        <MudButton
            Variant="Variant.Filled"
            Color="Color.Primary"
            OnClick="SaveTimeZone"
            Disabled="@(_isSaving || _selectedTimeZoneId == CurrentTimeZoneId)">
            @if (_isSaving)
            {
                <MudProgressCircular Size="Size.Small" Indeterminate="true" />
                <MudText Class="ml-2">Zapisywanie...</MudText>
            }
            else
            {
                <MudText>Zapisz zmiany</MudText>
            }
        </MudButton>
    </MudStack>
</MudForm>
```

**Obsługiwane zdarzenia:**
- `OnInitialized` - załadowanie listy dostępnych stref czasowych z `TimeZoneInfo.GetSystemTimeZones()`
- `SaveTimeZone` - wysłanie żądania PATCH do API, obsługa odpowiedzi, wywołanie callbacku `OnTimeZoneChanged`

**Warunki walidacji:**
- `TimeZoneId` jest wymagane (Required)
- `TimeZoneId` musi być prawidłowym identyfikatorem IANA (walidacja po stronie API - błąd 422)
- Przycisk zapisania jest wyłączony, gdy:
  - Trwa zapisywanie (`_isSaving == true`)
  - Wybrana strefa jest taka sama jak aktualna (`_selectedTimeZoneId == CurrentTimeZoneId`)

**Typy:**
- `string` dla `CurrentTimeZoneId` (props)
- `string` dla `_selectedTimeZoneId` (local state)
- `List<TimeZoneViewModel>` dla `_availableTimeZones`
- `bool` dla `_isSaving`
- `string?` dla `_successMessage` i `_errorMessage`
- `EventCallback` dla `OnTimeZoneChanged`

**Propsy:**
```csharp
[Parameter]
[EditorRequired]
public string CurrentTimeZoneId { get; set; } = null!;

[Parameter]
public EventCallback OnTimeZoneChanged { get; set; }
```

---

### DeleteAccountSection (Sekcja usuwania konta)

**Opis komponentu:**
Komponent zawierający przycisk inicjujący proces usuwania konta oraz zarządzający stanem modalu potwierdzenia. Wyświetla ostrzeżenie o nieodwracalności operacji oraz przycisk otwierający dialog potwierdzenia.

**Główne elementy HTML i komponenty dzieci:**
```razor
<MudText Typo="Typo.h6" Class="mb-3">Strefa niebezpieczna</MudText>

<MudStack Spacing="3">
    <MudAlert Severity="Severity.Warning">
        Usunięcie konta jest <strong>nieodwracalne</strong>.
        Wszystkie Twoje dane, w tym nawyki, check-iny i powiadomienia zostaną trwale usunięte.
    </MudAlert>

    <MudButton
        Variant="Variant.Filled"
        Color="Color.Error"
        OnClick="OpenDeleteDialog"
        StartIcon="@Icons.Material.Filled.Delete">
        Usuń konto
    </MudButton>
</MudStack>

<ConfirmDeleteAccountDialog
    @ref="_deleteDialog"
    OnConfirmed="HandleDeleteConfirmed" />
```

**Obsługiwane zdarzenia:**
- `OpenDeleteDialog` - otwiera modal `ConfirmDeleteAccountDialog`
- `HandleDeleteConfirmed` - wywołuje API DELETE, następnie wywołuje callback `OnAccountDeleted`

**Warunki walidacji:**
Brak na poziomie tego komponentu (delegowane do dialogu).

**Typy:**
- `ConfirmDeleteAccountDialog?` dla `_deleteDialog` (referencja)
- `EventCallback` dla `OnAccountDeleted`

**Propsy:**
```csharp
[Parameter]
public EventCallback OnAccountDeleted { get; set; }
```

---

### ConfirmDeleteAccountDialog (Modal potwierdzenia usunięcia)

**Opis komponentu:**
Komponent modalny wymagający wpisania dokładnie tekstu "DELETE" w celu potwierdzenia operacji usunięcia konta. Implementowany jako MudDialog. Zawiera pole tekstowe, instrukcję oraz przyciski akcji (Anuluj, Potwierdź). Przycisk potwierdzenia jest aktywny tylko gdy użytkownik wpisze dokładnie "DELETE".

**Główne elementy HTML i komponenty dzieci:**
```razor
<MudDialog @bind-IsVisible="_isVisible">
    <TitleContent>
        <MudText Typo="Typo.h6">
            <MudIcon Icon="@Icons.Material.Filled.Warning" Class="mr-2" Color="Color.Error" />
            Potwierdzenie usunięcia konta
        </MudText>
    </TitleContent>

    <DialogContent>
        <MudStack Spacing="3">
            <MudText>
                Ta operacja jest <strong>nieodwracalna</strong>. Aby potwierdzić, wpisz
                dokładnie <MudChip Size="Size.Small" Color="Color.Error">DELETE</MudChip> w polu poniżej.
            </MudText>

            <MudTextField
                @bind-Value="_confirmationText"
                Label="Wpisz DELETE"
                Variant="Variant.Outlined"
                Immediate="true"
                HelperText="Wielkość liter ma znaczenie" />
        </MudStack>
    </DialogContent>

    <DialogActions>
        <MudButton OnClick="Cancel">Anuluj</MudButton>
        <MudButton
            Color="Color.Error"
            Variant="Variant.Filled"
            OnClick="Confirm"
            Disabled="@(_confirmationText != "DELETE")">
            Usuń konto
        </MudButton>
    </DialogActions>
</MudDialog>
```

**Obsługiwane zdarzenia:**
- `Show` - publiczna metoda otwierająca dialog
- `Cancel` - zamyka dialog, czyści pole tekstowe
- `Confirm` - zamyka dialog, wywołuje callback `OnConfirmed`

**Warunki walidacji:**
- Pole tekstowe musi zawierać dokładnie "DELETE" (case-sensitive)
- Przycisk "Usuń konto" jest wyłączony dopóki warunek nie jest spełniony
- Walidacja tekstowa: `_confirmationText != "DELETE"` → przycisk disabled

**Typy:**
- `string` dla `_confirmationText`
- `bool` dla `_isVisible`
- `EventCallback` dla `OnConfirmed`

**Propsy:**
```csharp
[Parameter]
public EventCallback OnConfirmed { get; set; }
```

**Metody publiczne:**
```csharp
public void Show()
{
    _confirmationText = string.Empty;
    _isVisible = true;
    StateHasChanged();
}
```

## 5. Typy

### ProfileViewModel (Model widoku profilu)

Model przechowujący dane użytkownika pobrane z API. Mapowany z `ProfileResponse`.

```csharp
public class ProfileViewModel
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public string TimeZoneId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public int HabitsCount { get; set; }
}
```

**Pola:**
- `UserId` (Guid) - unikalny identyfikator użytkownika
- `Email` (string) - adres email użytkownika
- `EmailConfirmed` (bool) - status weryfikacji email
- `TimeZoneId` (string) - identyfikator IANA strefy czasowej (np. "Europe/Warsaw")
- `CreatedAtUtc` (DateTimeOffset) - data i czas utworzenia konta w UTC
- `HabitsCount` (int) - liczba nawyków użytkownika

---

### TimeZoneViewModel (Model strefy czasowej)

Model pomocniczy dla listy rozwijanej stref czasowych.

```csharp
public class TimeZoneViewModel
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
```

**Pola:**
- `Id` (string) - identyfikator IANA strefy czasowej (np. "America/New_York")
- `DisplayName` (string) - przyjazna nazwa do wyświetlenia (np. "(UTC-05:00) Eastern Time (US & Canada)")

**Źródło danych:**
Generowane z `TimeZoneInfo.GetSystemTimeZones()`:
```csharp
var timeZones = TimeZoneInfo.GetSystemTimeZones()
    .Select(tz => new TimeZoneViewModel
    {
        Id = tz.Id,
        DisplayName = $"(UTC{tz.BaseUtcOffset:hh\\:mm}) {tz.DisplayName}"
    })
    .OrderBy(tz => tz.DisplayName)
    .ToList();
```

---

### UpdateTimeZoneRequest (Request DTO)

Model żądania aktualizacji strefy czasowej - już zdefiniowany w API.

```csharp
public record UpdateTimeZoneRequest(
    string TimeZoneId
);
```

**Pola:**
- `TimeZoneId` (string) - nowy identyfikator IANA strefy czasowej

---

### DeleteAccountRequest (Request DTO)

Model żądania usunięcia konta - już zdefiniowany w API (zgodnie z auth endpoints).

```csharp
public record DeleteAccountRequest(
    string Confirmation
);
```

**Pola:**
- `Confirmation` (string) - tekst potwierdzenia, musi być równy "DELETE"

## 6. Zarządzanie stanem

### Stan komponentu Profile.razor

**Zmienne stanu:**
```csharp
private ProfileViewModel? _profile;
private bool _isLoading = true;
private string? _errorMessage;
```

**Lifecycle i zarządzanie:**
- `OnInitializedAsync` - pobiera dane profilu z API, ustawia `_isLoading = false`
- `HandleTimeZoneChanged` - odświeża dane profilu z API po zmianie strefy
- `HandleAccountDeleted` - przekierowuje do logout endpoint

### Stan komponentu TimeZoneEditor

**Zmienne stanu:**
```csharp
private string _selectedTimeZoneId = string.Empty;
private List<TimeZoneViewModel> _availableTimeZones = new();
private bool _isSaving = false;
private string? _successMessage;
private string? _errorMessage;
private MudForm? _form;
```

**Zarządzanie:**
- `OnInitialized` - inicjalizuje `_selectedTimeZoneId` wartością z props, ładuje listę stref
- `SaveTimeZone` - wysyła request, obsługuje odpowiedź, wywołuje callback
- Komunikaty sukcesu/błędu są automatycznie czyszczone po 5 sekundach (opcjonalne)

### Stan komponentu ConfirmDeleteAccountDialog

**Zmienne stanu:**
```csharp
private bool _isVisible = false;
private string _confirmationText = string.Empty;
```

**Zarządzanie:**
- `Show()` - czyści pole tekstowe, pokazuje dialog
- `Cancel()` - ukrywa dialog, czyści pole
- `Confirm()` - ukrywa dialog, wywołuje callback

### Czy potrzebny customowy hook?

**Nie** - zarządzanie stanem jest wystarczająco proste do obsłużenia przez stan komponentów Blazor. Nie ma potrzeby tworzenia dedykowanego serwisu/hooka dla tego widoku. Komunikacja z API odbywa się przez wstrzykniętą instancję `HttpClient` lub wygenerowanego klienta API.

## 7. Integracja API

### Endpoint: GET /api/v1/profile

**Cel:** Pobranie pełnych danych profilu użytkownika.

**Metoda HTTP:** GET

**Request:**
- Brak body
- Wymagana autoryzacja (cookie-based)

**Response 200 (Success):**
```csharp
public record ProfileResponse(
    Guid UserId,
    string Email,
    bool EmailConfirmed,
    string TimeZoneId,
    DateTimeOffset CreatedAtUtc,
    int HabitsCount
);
```

**Możliwe błędy:**
- 401 Unauthorized - brak autoryzacji lub wygasła sesja

**Implementacja w komponencie:**
```csharp
protected override async Task OnInitializedAsync()
{
    try
    {
        _isLoading = true;
        var response = await HttpClient.GetFromJsonAsync<ProfileResponse>("/api/v1/profile");

        if (response != null)
        {
            _profile = new ProfileViewModel
            {
                UserId = response.UserId,
                Email = response.Email,
                EmailConfirmed = response.EmailConfirmed,
                TimeZoneId = response.TimeZoneId,
                CreatedAtUtc = response.CreatedAtUtc,
                HabitsCount = response.HabitsCount
            };
        }
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
    {
        _errorMessage = "Sesja wygasła. Proszę zalogować się ponownie.";
        NavigationManager.NavigateTo("/auth/login");
    }
    catch (Exception ex)
    {
        _errorMessage = "Nie udało się załadować danych profilu.";
    }
    finally
    {
        _isLoading = false;
    }
}
```

---

### Endpoint: PATCH /api/v1/profile/timezone

**Cel:** Aktualizacja strefy czasowej użytkownika.

**Metoda HTTP:** PATCH

**Request:**
```csharp
public record UpdateTimeZoneRequest(
    string TimeZoneId
);
```

**Request body (JSON):**
```json
{
  "timeZoneId": "America/New_York"
}
```

**Response 204 (Success):**
- Brak body, status No Content

**Możliwe błędy:**
- 400 Bad Request - brak lub nieprawidłowy format `timeZoneId`
- 401 Unauthorized - brak autoryzacji
- 422 Unprocessable Entity - nieobsługiwana strefa czasowa

**Implementacja w komponencie:**
```csharp
private async Task SaveTimeZone()
{
    try
    {
        _isSaving = true;
        _successMessage = null;
        _errorMessage = null;

        var request = new UpdateTimeZoneRequest(_selectedTimeZoneId);
        var response = await HttpClient.PatchAsJsonAsync("/api/v1/profile/timezone", request);

        if (response.IsSuccessStatusCode)
        {
            _successMessage = "Strefa czasowa została zaktualizowana. Zmiana zacznie obowiązywać od następnej doby.";
            await OnTimeZoneChanged.InvokeAsync();
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
        {
            _errorMessage = "Wybrana strefa czasowa nie jest obsługiwana.";
        }
        else
        {
            _errorMessage = "Nie udało się zaktualizować strefy czasowej.";
        }
    }
    catch (Exception ex)
    {
        _errorMessage = "Wystąpił błąd podczas zapisywania zmian.";
    }
    finally
    {
        _isSaving = false;
    }
}
```

---

### Endpoint: DELETE /api/v1/profile (lub /api/v1/auth/delete-account)

**Cel:** Trwałe usunięcie konta użytkownika.

**Metoda HTTP:** POST (według AuthEndpoints) lub DELETE

**Ścieżka:** `/api/v1/auth/delete-account` (zgodnie z implementacją w AuthEndpoints.cs)

**Request:**
```csharp
public record DeleteAccountRequest(
    string Confirmation
);
```

**Request body (JSON):**
```json
{
  "confirmation": "DELETE"
}
```

**Response 204 (Success):**
- Brak body, status No Content
- Sesja zostaje zakończona

**Możliwe błędy:**
- 400 Bad Request - nieprawidłowe lub brakujące potwierdzenie
- 401 Unauthorized - brak autoryzacji

**Implementacja w komponencie:**
```csharp
private async Task HandleDeleteConfirmed()
{
    try
    {
        var request = new DeleteAccountRequest("DELETE");
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/delete-account", request);

        if (response.IsSuccessStatusCode)
        {
            await OnAccountDeleted.InvokeAsync();
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            // To nie powinno się wydarzyć, bo dialog wymusza "DELETE"
            Snackbar.Add("Nieprawidłowe potwierdzenie.", Severity.Error);
        }
        else
        {
            Snackbar.Add("Nie udało się usunąć konta.", Severity.Error);
        }
    }
    catch (Exception ex)
    {
        Snackbar.Add("Wystąpił błąd podczas usuwania konta.", Severity.Error);
    }
}
```

## 8. Interakcje użytkownika

### Interakcja 1: Przeglądanie informacji o profilu

**Akcja użytkownika:**
- Użytkownik przechodzi na stronę `/profile`

**Oczekiwany wynik:**
- Wyświetlany jest loader (MudProgressCircular)
- Po załadowaniu danych wyświetlane są wszystkie informacje o profilu w sekcji ProfileSummary
- Email, status weryfikacji, data utworzenia, strefa czasowa i liczba nawyków są widoczne

**Flow:**
1. Komponent Profile.razor wywołuje `OnInitializedAsync`
2. Wykonywane jest żądanie GET /api/v1/profile
3. Dane są mapowane na `ProfileViewModel`
4. Komponent się re-renderuje z wypełnionymi danymi

---

### Interakcja 2: Zmiana strefy czasowej

**Akcja użytkownika:**
1. Użytkownik otwiera dropdown strefy czasowej
2. Wybiera nową strefę z listy
3. Klika przycisk "Zapisz zmiany"

**Oczekiwany wynik:**
- Przycisk pokazuje stan ładowania ("Zapisywanie...")
- Po sukcesie wyświetlany jest komunikat: "Strefa czasowa została zaktualizowana. Zmiana zacznie obowiązywać od następnej doby."
- Dane profilu są odświeżane (opcjonalnie)
- W przypadku błędu wyświetlany jest odpowiedni komunikat

**Flow:**
1. Użytkownik wybiera nową wartość w `MudSelect` - zmienia się `_selectedTimeZoneId`
2. Przycisk zostaje aktywowany (już nie disabled, bo wartość różna od aktualnej)
3. Kliknięcie wywołuje `SaveTimeZone()`
4. `_isSaving` ustawiane na true, przycisk pokazuje loader
5. Wykonywane jest żądanie PATCH /api/v1/profile/timezone
6. W zależności od odpowiedzi:
   - 204: `_successMessage` jest ustawiane, wywołany `OnTimeZoneChanged` callback
   - 422: `_errorMessage` = "Wybrana strefa czasowa nie jest obsługiwana."
   - Inne błędy: generyczny komunikat błędu
7. `_isSaving` ustawiane na false

---

### Interakcja 3: Usunięcie konta - otwarcie dialogu

**Akcja użytkownika:**
- Użytkownik klika przycisk "Usuń konto" w sekcji DeleteAccountSection

**Oczekiwany wynik:**
- Otwiera się modal potwierdzenia (ConfirmDeleteAccountDialog)
- Pole tekstowe jest puste
- Przycisk "Usuń konto" w dialogu jest wyłączony

**Flow:**
1. Kliknięcie wywołuje `OpenDeleteDialog()`
2. Metoda wywołuje `_deleteDialog.Show()`
3. Dialog ustawia `_isVisible = true` i `_confirmationText = ""`
4. Dialog się re-renderuje i wyświetla

---

### Interakcja 4: Potwierdzenie usunięcia konta

**Akcja użytkownika:**
1. Użytkownik wpisuje tekst w pole tekstowe dialogu
2. Gdy wpisze dokładnie "DELETE", przycisk "Usuń konto" staje się aktywny
3. Użytkownik klika "Usuń konto"

**Oczekiwany wynik:**
- Dialog się zamyka
- Konto użytkownika jest usuwane z bazy danych
- Użytkownik jest wylogowywany i przekierowywany do strony logowania

**Flow:**
1. Użytkownik wpisuje znaki - zmienia się `_confirmationText`
2. Warunek `_confirmationText != "DELETE"` jest sprawdzany z `Immediate="true"`
3. Gdy `_confirmationText == "DELETE"`, przycisk przestaje być disabled
4. Kliknięcie wywołuje `Confirm()`
5. Dialog wywołuje callback `OnConfirmed`
6. `HandleDeleteConfirmed()` w DeleteAccountSection jest wywoływane
7. Wykonywane jest żądanie POST /api/v1/auth/delete-account z body `{ "confirmation": "DELETE" }`
8. W przypadku sukcesu (204):
   - Wywołany `OnAccountDeleted` callback w Profile.razor
   - `HandleAccountDeleted()` przekierowuje do `/auth/logout` lub `/auth/login`
9. W przypadku błędu: wyświetlany komunikat przez Snackbar

---

### Interakcja 5: Anulowanie usunięcia konta

**Akcja użytkownika:**
- Użytkownik klika "Anuluj" w dialogu potwierdzenia

**Oczekiwany wynik:**
- Dialog się zamyka
- Pole tekstowe jest czyszczone
- Żadne zmiany nie są zapisywane

**Flow:**
1. Kliknięcie wywołuje `Cancel()`
2. `_isVisible` ustawiane na false
3. `_confirmationText` czyszczony
4. Dialog się ukrywa

## 9. Warunki i walidacja

### Walidacja 1: Pole strefy czasowej (TimeZoneEditor)

**Komponent:** TimeZoneEditor

**Warunek:** Strefa czasowa jest wymagana

**Implementacja:**
```razor
<MudSelect
    @bind-Value="_selectedTimeZoneId"
    Label="Wybierz strefę czasową"
    Required="true"
    RequiredError="Strefa czasowa jest wymagana">
```

**Wpływ na UI:**
- Jeśli użytkownik nie wybierze strefy, walidacja MudForm blokuje submit
- Komunikat "Strefa czasowa jest wymagana" wyświetlany pod polem

---

### Walidacja 2: Niezmienna strefa czasowa (TimeZoneEditor)

**Komponent:** TimeZoneEditor

**Warunek:** Przycisk zapisania wyłączony, gdy wybrana strefa = aktualna strefa

**Implementacja:**
```razor
<MudButton
    Disabled="@(_isSaving || _selectedTimeZoneId == CurrentTimeZoneId)">
```

**Wpływ na UI:**
- Przycisk jest nieaktywny i szary
- Użytkownik nie może kliknąć, dopóki nie zmieni wartości

---

### Walidacja 3: Potwierdzenie usunięcia konta (ConfirmDeleteAccountDialog)

**Komponent:** ConfirmDeleteAccountDialog

**Warunek:** Pole tekstowe musi zawierać dokładnie "DELETE" (case-sensitive)

**Implementacja:**
```razor
<MudTextField
    @bind-Value="_confirmationText"
    Immediate="true" />

<MudButton
    Disabled="@(_confirmationText != "DELETE")">
```

**Wpływ na UI:**
- Przycisk "Usuń konto" jest wyłączony, dopóki warunek nie jest spełniony
- Użytkownik musi wpisać dokładnie "DELETE" (nie "delete", nie "Delete")
- Zmiana w polu jest sprawdzana na bieżąco (Immediate="true")

---

### Walidacja 4: Stan ładowania (wszystkie komponenty z akcjami)

**Komponenty:** TimeZoneEditor, DeleteAccountSection

**Warunek:** Podczas wykonywania operacji asynchronicznej, przyciski akcji są wyłączone

**Implementacja TimeZoneEditor:**
```razor
<MudButton
    Disabled="@(_isSaving || _selectedTimeZoneId == CurrentTimeZoneId)">
```

**Wpływ na UI:**
- Przycisk pokazuje loader podczas `_isSaving == true`
- Użytkownik nie może wielokrotnie wysłać tego samego żądania
- Zapobiega race conditions

---

### Walidacja 5: Odpowiedzi API (backend validation)

**Komponenty:** TimeZoneEditor

**Warunki weryfikowane przez backend:**
- 400 Bad Request: brak lub nieprawidłowy format timeZoneId
- 422 Unprocessable Entity: nieobsługiwana strefa czasowa

**Wpływ na UI:**
- Dla 422: wyświetlany komunikat "Wybrana strefa czasowa nie jest obsługiwana."
- Dla 400: wyświetlany generyczny komunikat błędu
- Komunikaty wyświetlane jako MudAlert z Severity.Error

## 10. Obsługa błędów

### Błąd 1: Nieautoryzowany dostęp (401)

**Scenariusz:**
- Wygasła sesja użytkownika
- Użytkownik nie jest zalogowany

**Obsługa w Profile.razor:**
```csharp
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
{
    _errorMessage = "Sesja wygasła. Proszę zalogować się ponownie.";
    NavigationManager.NavigateTo("/auth/login");
}
```

**Rezultat:**
- Użytkownik jest przekierowywany do strony logowania
- Wyświetlany komunikat o wygasłej sesji

---

### Błąd 2: Nieobsługiwana strefa czasowa (422)

**Scenariusz:**
- Użytkownik próbuje ustawić strefę czasową, która nie jest obsługiwana przez system

**Obsługa w TimeZoneEditor:**
```csharp
else if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
{
    _errorMessage = "Wybrana strefa czasowa nie jest obsługiwana.";
}
```

**Rezultat:**
- Wyświetlany alert z komunikatem błędu
- Użytkownik może wybrać inną strefę i spróbować ponownie

---

### Błąd 3: Błąd walidacji (400)

**Scenariusz:**
- Nieprawidłowe dane w żądaniu (brak wymaganego pola, nieprawidłowy format)

**Obsługa w TimeZoneEditor i DeleteAccountSection:**
```csharp
else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
{
    _errorMessage = "Dane formularza są nieprawidłowe.";
}
```

**Rezultat:**
- Wyświetlany komunikat błędu
- Użytkownik może poprawić dane i spróbować ponownie

---

### Błąd 4: Błąd sieciowy lub timeout

**Scenariusz:**
- Brak połączenia z internetem
- Serwer nie odpowiada
- Timeout żądania

**Obsługa (wszystkie komponenty z API calls):**
```csharp
catch (HttpRequestException ex)
{
    _errorMessage = "Błąd połączenia. Sprawdź połączenie z internetem i spróbuj ponownie.";
}
catch (Exception ex)
{
    _errorMessage = "Wystąpił nieoczekiwany błąd. Spróbuj ponownie później.";
    // Opcjonalnie: logowanie błędu
}
```

**Rezultat:**
- Wyświetlany przyjazny komunikat błędu
- Użytkownik może spróbować ponownie

---

### Błąd 5: Puste dane profilu

**Scenariusz:**
- API zwróciło null lub puste dane

**Obsługa w Profile.razor:**
```csharp
if (response != null)
{
    _profile = MapToViewModel(response);
}
else
{
    _errorMessage = "Nie udało się załadować danych profilu.";
}
```

**Rezultat:**
- Wyświetlany komunikat błędu zamiast pustego widoku

---

### Błąd 6: Błąd usuwania konta

**Scenariusz:**
- Błąd po stronie serwera podczas usuwania konta
- Naruszenie integralności danych

**Obsługa w DeleteAccountSection:**
```csharp
if (response.IsSuccessStatusCode)
{
    await OnAccountDeleted.InvokeAsync();
}
else
{
    Snackbar.Add("Nie udało się usunąć konta. Spróbuj ponownie później.", Severity.Error);
}
```

**Rezultat:**
- Wyświetlany Snackbar z komunikatem błędu
- Użytkownik pozostaje na stronie profilu

## 11. Kroki implementacji

### Krok 1: Przygotowanie struktury plików
1. Utwórz folder `Components/Pages/Profile/` w projekcie Blazor
2. Utwórz pliki komponentów:
   - `Profile.razor` (główny widok)
   - `ProfileSummary.razor`
   - `TimeZoneEditor.razor`
   - `DeleteAccountSection.razor`
   - `ConfirmDeleteAccountDialog.razor`
3. Utwórz folder `Models/Profile/` dla ViewModels
4. Utwórz pliki ViewModels:
   - `ProfileViewModel.cs`
   - `TimeZoneViewModel.cs`

---

### Krok 2: Implementacja typów (ViewModels)
1. Zaimplementuj `ProfileViewModel` zgodnie z sekcją 5
2. Zaimplementuj `TimeZoneViewModel` zgodnie z sekcją 5
3. Upewnij się, że DTOs (`ProfileResponse`, `UpdateTimeZoneRequest`, `DeleteAccountRequest`) są dostępne w projekcie Blazor (shared project lub referencja)

---

### Krok 3: Implementacja komponentu ProfileSummary
1. Utwórz prosty komponent prezentacyjny
2. Zdefiniuj parameter `Profile` typu `ProfileViewModel`
3. Użyj komponentów MudBlazor (MudField, MudChip) do wyświetlenia danych
4. Zaimplementuj formatowanie daty (`.ToString("dd.MM.yyyy HH:mm")`)
5. Dodaj warunkowe renderowanie dla statusu weryfikacji email (MudChip zielony/żółty)

---

### Krok 4: Implementacja komponentu TimeZoneEditor
1. Zdefiniuj parametry: `CurrentTimeZoneId` i `OnTimeZoneChanged`
2. W `OnInitialized` załaduj listę stref czasowych z `TimeZoneInfo.GetSystemTimeZones()`
3. Zaimplementuj MudSelect z binding do `_selectedTimeZoneId`
4. Dodaj MudButton z obsługą stanu ładowania
5. Zaimplementuj metodę `SaveTimeZone()`:
   - Ustawienie `_isSaving = true`
   - Wywołanie API PATCH
   - Obsługa różnych kodów odpowiedzi (204, 422, 400)
   - Wyświetlenie komunikatów sukcesu/błędu
   - Wywołanie callback `OnTimeZoneChanged`
6. Dodaj informacyjny alert o tym, że zmiana zacznie obowiązywać od następnej doby
7. Zaimplementuj warunki disabled dla przycisku

---

### Krok 5: Implementacja komponentu ConfirmDeleteAccountDialog
1. Zdefiniuj parameter `OnConfirmed`
2. Zaimplementuj MudDialog z polami:
   - TitleContent z ikoną ostrzeżenia
   - DialogContent z instrukcją i MudTextField
   - DialogActions z przyciskami Anuluj/Potwierdź
3. Zaimplementuj binding `_confirmationText` z `Immediate="true"`
4. Dodaj warunek disabled: `_confirmationText != "DELETE"`
5. Zaimplementuj metody:
   - `Show()` - otwiera dialog, czyści pole
   - `Cancel()` - zamyka dialog
   - `Confirm()` - zamyka dialog, wywołuje callback
6. Dodaj stylowanie dla przycisku potwierdzenia (Color.Error)

---

### Krok 6: Implementacja komponentu DeleteAccountSection
1. Zdefiniuj parameter `OnAccountDeleted`
2. Dodaj referencję do `ConfirmDeleteAccountDialog` za pomocą `@ref`
3. Zaimplementuj ostrzegawczy MudAlert z opisem konsekwencji
4. Dodaj MudButton wywołujący `_deleteDialog.Show()`
5. Zaimplementuj metodę `HandleDeleteConfirmed()`:
   - Wywołanie API POST /api/v1/auth/delete-account
   - Obsługa odpowiedzi (204, 400)
   - Użycie Snackbar do wyświetlania komunikatów
   - Wywołanie callback `OnAccountDeleted`

---

### Krok 7: Implementacja głównego komponentu Profile.razor
1. Dodaj dyrektywy:
   - `@page "/profile"`
   - `@attribute [Authorize]`
2. Wstrzyknij zależności:
   - `@inject HttpClient HttpClient`
   - `@inject NavigationManager NavigationManager`
   - `@inject ISnackbar Snackbar`
3. Zdefiniuj zmienne stanu: `_profile`, `_isLoading`, `_errorMessage`
4. Zaimplementuj `OnInitializedAsync()`:
   - Wywołanie GET /api/v1/profile
   - Mapowanie response na `ProfileViewModel`
   - Obsługa błędów (401 → redirect do login, inne → komunikat)
5. Zaimplementuj layout z MudContainer i MudPaper dla sekcji
6. Dodaj warunkowe renderowanie:
   - Loader gdy `_isLoading`
   - Sekcje komponentów gdy `_profile != null`
   - Alert błędu gdy `_errorMessage != null`
7. Zaimplementuj callback `HandleTimeZoneChanged()`:
   - Ponowne wywołanie API GET /api/v1/profile
   - Odświeżenie danych w `_profile`
8. Zaimplementuj callback `HandleAccountDeleted()`:
   - Przekierowanie do `/auth/logout` lub `/auth/login`

---

### Krok 8: Stylowanie i responsywność
1. Użyj MudContainer z `MaxWidth.Medium` dla lepszej czytelności
2. Dodaj odpowiednie odstępy między sekcjami (`Class="mb-4"`)
3. Użyj MudStack dla verticalnego layoutu w sekcjach
4. Upewnij się, że komponenty MudBlazor są responsywne (domyślnie są)
5. Przetestuj widok na różnych rozmiarach ekranu

---

### Krok 9: Integracja z nawigacją
1. Dodaj link do `/profile` w głównym menu nawigacyjnym
2. Dodaj ikonę profilu (np. `Icons.Material.Filled.Person`)
3. Upewnij się, że link jest widoczny tylko dla zalogowanych użytkowników
4. Opcjonalnie: dodaj highlighting aktywnej strony w nawigacji

---

### Krok 10: Testowanie
1. Test ładowania danych:
   - Zaloguj się i przejdź do `/profile`
   - Sprawdź, czy wszystkie dane są poprawnie wyświetlane
2. Test zmiany strefy czasowej:
   - Wybierz nową strefę z dropdownu
   - Zapisz zmiany
   - Sprawdź komunikat sukcesu
   - Odśwież stronę i sprawdź, czy strefa się zaktualizowała
3. Test błędu 422:
   - Spróbuj ustawić nieobsługiwaną strefę (może wymagać mockowania)
   - Sprawdź, czy komunikat błędu jest wyświetlany
4. Test usuwania konta:
   - Kliknij "Usuń konto"
   - Sprawdź, czy dialog się otwiera
   - Wpisz "DELETE" i sprawdź, czy przycisk staje się aktywny
   - Kliknij "Potwierdź" i sprawdź, czy konto jest usuwane
   - Sprawdź, czy użytkownik jest wylogowywany
5. Test walidacji dialogu:
   - Wpisz "delete" (małe litery) i sprawdź, czy przycisk pozostaje wyłączony
   - Wpisz "DEL" i sprawdź to samo
6. Test błędu 401:
   - Wymuś wygaśnięcie sesji
   - Odśwież stronę profilu
   - Sprawdź, czy użytkownik jest przekierowywany do logowania

---

### Krok 11: Obsługa błędów i edge cases
1. Dodaj obsługę przypadku, gdy API zwraca null
2. Dodaj obsługę timeout'ów sieciowych
3. Dodaj logowanie błędów do konsoli (w development)
4. Upewnij się, że wszystkie komunikaty błędów są przyjazne użytkownikowi

---

### Krok 12: Finalizacja i code review
1. Uruchom `dotnet format` dla formatowania kodu
2. Sprawdź, czy wszystkie using statements są w porządku
3. Dodaj komentarze XML do publicznych metod/properties
4. Sprawdź zgodność z coding guidelines projektu
5. Przejrzyj TODO's i usuń niepotrzebne
6. Wykonaj self-review przed commitowaniem

---

### Krok 13: Dokumentacja (opcjonalnie)
1. Zaktualizuj dokumentację routingu, jeśli istnieje
2. Dodaj screenshots widoku do dokumentacji UI
3. Zaktualizuj user manual, jeśli istnieje

---

### Dodatkowe uwagi implementacyjne:

**Dependency Injection:**
- Użyj konstruktora lub `@inject` dla HttpClient, NavigationManager, ISnackbar
- Upewnij się, że HttpClient jest skonfigurowany z baseAddress API

**Error Handling Best Practices:**
- Zawsze używaj try-catch dla API calls
- Loguj błędy do konsoli w development
- Pokazuj przyjazne komunikaty użytkownikowi

**Accessibility:**
- Upewnij się, że wszystkie pola mają label
- Użyj odpowiednich aria-* atrybutów (MudBlazor robi to automatycznie)
- Przetestuj nawigację klawiaturą

**Performance:**
- Użyj `AsNoTracking()` w EF Core queries (backend)
- Minimalizuj re-renders przez odpowiednie użycie StateHasChanged()
- Rozważ debouncing dla pole tekstowego w dialogu (opcjonalnie)

**Security:**
- Nigdy nie loguj wrażliwych danych
- Upewnij się, że endpoint DELETE konta wymaga autoryzacji
- Waliduj dane wejściowe zarówno na froncie, jak i backendzie
