# Plan implementacji widoku Today

## 1. Przeglad
Widok Today pokazuje liste dzisiejszych krokow do wykonania i zapewnia szybki check-in dla kazdego nawyku. Celem jest minimalna liczba klikniec, czytelny stan dnia (X/Y) oraz obsluga bledow API (401/403/409/422) z jasnymi komunikatami.

## 2. Routing widoku
Sciezka: `/today` (strona routowana w `HabitFlow.Blazor/Components/Pages/Today.razor`).

## 3. Struktura komponentow
- TodayPage
  - TodayProgressHeader
  - RefreshButton
  - TodayChecklist
    - TodayChecklistItem (element listy)
  - EmptyStateCard
  - CheckinDialog

## 4. Szczegoly komponentu
### TodayPage
- Opis komponentu: Strona routowana, orkiestruje pobranie danych, stany (loading/error/empty), otwieranie dialogu check-in i odswiezanie.
- Glowne elementy: `MudContainer`, `MudStack`, `MudProgressCircular` (loading), `MudAlert` (error), `MudDivider`.
- Obslugiwane interakcje: init/fetch danych, klik odswiez, klik check-in na elemencie.
- Obslugiwana walidacja: brak bezposredniej, deleguje do CheckinDialog i walidacji API.
- Typy: `TodayResponse`, `TodayItem`, `TodayViewState`, `TodayItemVm`.
- Propsy: brak (komponent routowany).

### TodayProgressHeader
- Opis komponentu: Naglowek z lokalna data i licznikiem postepu dnia (X/Y).
- Glowne elementy: `MudText`, `MudChip` lub `MudBadge` dla X/Y.
- Obslugiwane interakcje: brak.
- Obslugiwana walidacja: brak.
- Typy: `TodayHeaderVm` (Data, CompletedCount, TotalCount).
- Propsy: `DateOnly Date`, `int CompletedCount`, `int TotalCount`.

### RefreshButton
- Opis komponentu: przycisk odswiezajacy dane.
- Glowne elementy: `MudIconButton`.
- Obslugiwane interakcje: `OnClick` -> ponowny fetch.
- Obslugiwana walidacja: brak.
- Typy: brak.
- Propsy: `EventCallback OnRefresh`, `bool IsBusy`.

### TodayChecklist
- Opis komponentu: lista dzisiejszych itemow z akcja check-in.
- Glowne elementy: `MudList` lub `MudTable` (zgodnie z MudBlazor), mapowanie po `Items`.
- Obslugiwane interakcje: klik w `Check-in` na elemencie.
- Obslugiwana walidacja: brak, przekazuje dane do dialogu.
- Typy: `TodayItemVm`.
- Propsy: `IReadOnlyList<TodayItemVm> Items`, `EventCallback<TodayItemVm> OnCheckin`.

### TodayChecklistItem
- Opis komponentu: pojedynczy wiersz nawyku z tytulem, typem, targetem, statusem check-in i przyciskiem akcji.
- Glowne elementy: `MudListItem`, `MudText`, `MudChip` (status), `MudButton`.
- Obslugiwane interakcje: klik przycisku check-in.
- Obslugiwana walidacja: przycisk disabled gdy `HasCheckin` lub `IsSubmitting`.
- Typy: `TodayItemVm`.
- Propsy: `TodayItemVm Item`, `EventCallback<TodayItemVm> OnCheckin`.

### CheckinDialog
- Opis komponentu: modal do wprowadzenia wartosci check-in i potwierdzenia zapisu.
- Glowne elementy: `MudDialog`, `MudText`, `MudNumericField<int>`, opcjonalnie `MudCheckBox` dla Binary.
- Obslugiwane interakcje: zmiana wartosci, zapis, anuluj.
- Obslugiwana walidacja:
  - `actualValue` >= 0
  - `actualValue` <= `TargetValue` (dla Quantitative)
  - `actualValue` w {0,1} dla Binary
  - `localDate` ustawiane na `TodayResponse.Date`
- Typy: `CheckinDialogVm`, `CreateCheckinRequest`.
- Propsy: `TodayItemVm Item`, `DateOnly LocalDate`, `EventCallback<CheckinResult> OnSubmit`.

### EmptyStateCard
- Opis komponentu: karta stanu pustego, gdy brak zaplanowanych itemow.
- Glowne elementy: `MudPaper`, `MudText`, opcjonalnie link do tworzenia nawyku.
- Obslugiwane interakcje: opcjonalny klik (np. przejdz do /habits/new).
- Obslugiwana walidacja: brak.
- Typy: brak.
- Propsy: `string Message`, `string? ActionHref`.

## 5. Typy
- `TodayResponse` (API): `DateOnly Date`, `IReadOnlyList<TodayItem> Items`.
- `TodayItem` (API): `int HabitId`, `string Title`, `int Type`, `int CompletionMode`, `int TargetValue`, `string? TargetUnit`, `bool IsPlanned`, `bool HasCheckin`.
- `CreateCheckinRequest` (API): `DateOnly LocalDate`, `int ActualValue`.
- `CheckinResponse` (API): `long Id`, `int HabitId`, `string UserId`, `DateOnly LocalDate`, `int ActualValue`, `int TargetValueSnapshot`, `int CompletionModeSnapshot`, `int HabitTypeSnapshot`, `bool IsPlanned`, `DateTimeOffset CreatedAtUtc`.
- `TodayItemVm` (UI):
  - `int HabitId`
  - `string Title`
  - `int Type`
  - `int CompletionMode`
  - `int TargetValue`
  - `string? TargetUnit`
  - `bool IsPlanned`
  - `bool HasCheckin`
  - `bool IsSubmitting`
  - `string TargetLabel` (np. "10 pages")
- `TodayViewState` (UI):
  - `bool IsLoading`
  - `string? ErrorMessage`
  - `DateOnly? Date`
  - `IReadOnlyList<TodayItemVm> Items`

## 6. Zarzadzanie stanem
- Stan lokalny w `TodayPage`: `TodayViewState`, `TodayItemVm` (z `IsSubmitting`).
- Brak globalnego stanu; dane pobierane na wejscie i po refresh.
- Dialog otwierany z `MudDialogService`; wynik zapisu aktualizuje element lokalnie (ustawienie `HasCheckin = true`, aktualizacja licznika).

## 7. Integracja API
- GET `/api/v1/today` (opcjonalnie z `date`, domyslnie dzis w strefie uzytkownika) -> `TodayResponse`.
- POST `/api/v1/habits/{habitId}/checkins` z `CreateCheckinRequest` -> `CheckinResponse` (201).
- Warstwa klienta: `TodayApiClient` w `HabitFlow.Blazor/Services/` z metodami `GetTodayAsync(DateOnly? date, CancellationToken)` i `CreateCheckinAsync(int habitId, CreateCheckinRequest, CancellationToken)`.

## 8. Interakcje uzytkownika
- Wejscie na `/today` -> fetch danych -> pokazanie listy lub empty state.
- Klik "Check-in" -> otwarty `CheckinDialog`.
- Wprowadzenie wartosci -> walidacja lokalna -> zapis.
- Sukces -> zamkniecie dialogu, oznaczenie elementu jako wykonany, aktualizacja licznika X/Y.
- Klik "Refresh" -> ponowny fetch, aktualizacja stanu.

## 9. Warunki i walidacja
- `actualValue`:
  - Binary: tylko 0/1 (UI: checkbox albo toggle).
  - Quantitative: zakres 0..TargetValue.
- `localDate`: ustawiana z `TodayResponse.Date`.
- Blokada podwojnego wyslania: `IsSubmitting` per item i disabled w `TodayChecklistItem`.
- Ukrywanie akcji: `HasCheckin == true` -> disabled/etykieta "Zrobione".

## 10. Obsluga bledow
- 401: przekierowanie do logowania lub komunikat "Zaloguj sie".
- 403: komunikat "Brak dostepu do nawyku".
- 404: komunikat "Nawyk nie istnieje".
- 409: komunikat "Check-in dla tego dnia juz istnieje".
- 422: komunikat "Check-in niedozwolony (deadline/niezaplanowany/ponad 7 dni wstecz)".
- 400: komunikat walidacyjny "Niepoprawna wartosc".
- Siec/timeout: `MudAlert` z opcja ponownego sprobowania.

## 11. Kroki implementacji
1. Dodaj strone `HabitFlow.Blazor/Components/Pages/Today.razor` z routingiem `/today`.
2. Dodaj `TodayApiClient` w `HabitFlow.Blazor/Services/` i zarejestruj w DI.
3. Zaimplementuj `TodayProgressHeader`, `TodayChecklist`, `TodayChecklistItem`, `RefreshButton`, `EmptyStateCard`, `CheckinDialog` (MudBlazor).
4. W `TodayPage` dodaj pobieranie danych, mapowanie `TodayResponse` -> `TodayItemVm`, loading/error/empty states.
5. Dodaj obsluge dialogu check-in z walidacja lokalna i mapowaniem bledow API.
6. Dodaj blokade podwojnego wyslania i optymistyczne oznaczanie `HasCheckin` po 201.
7. Sprawdz UX na mobile (2-3 klikniecia do check-in) oraz komunikaty bledow 409/422.
