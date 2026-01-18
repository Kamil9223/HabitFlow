# Plan implementacji widoku Habits (lista)

## 1. Przeglad
Widok /habits sluzy do przegladu i zarzadzania nawykami uzytkownika: lista z filtrami i paginacja, akcje create/edit/delete, szybki check-in oraz pokazanie kluczowych danych (tytul, typ, harmonogram, krotki success_rate, deadline, licznik N/20).

## 2. Routing widoku
- Sciezka: `/habits`
- Renderowanie: Blazor Server (`@rendermode InteractiveServer`)

## 3. Struktura komponentow
```
Habits (page)
├── HabitFilters
├── HabitList
│   ├── HabitItem (row/card)
│   └── EmptyState
├── HabitFormDialog
├── ConfirmDialog
└── CheckinDialog
```

## 4. Szczegoly komponentow
### Habits (page)
- Opis: Kontener widoku, orkiestruje pobieranie danych, filtry, paginacje, dialogi oraz obsluge bledow/limitow.
- Glowne elementy: `MudContainer`, pasek filtrow (`HabitFilters`), `MudPaper` z `HabitList`, `MudPagination`/`MudTable` (server data), `MudAlert` na bledy.
- Obslugiwane interakcje: zmiana filtrow, wyszukiwanie, sortowanie, stronicowanie, otwarcie dialogow (create/edit/delete/check-in), odswiezanie listy.
- Walidacja: blokada przycisku "Dodaj nawyk" gdy limit 20 osiagniety; kontrola wartosci page/pageSize; debounce dla wyszukiwania.
- Typy: `HabitListState`, `HabitListFilterVm`, `HabitListItemVm`, `PagedResponseOfHabitResponse`.
- Propsy: brak (strona routowana).

### HabitFilters
- Opis: Pasek filtrow i sortowania listy (typ, completionMode, aktywne/archiwalne, wyszukiwanie po tytule, sort).
- Glowne elementy: `MudSelect` (typ, completionMode, sort), `MudSwitch`/`MudToggleGroup` (aktywne), `MudTextField` (search), `MudButton` (clear), ewentualnie `MudChipSet` dla aktywnych filtrow.
- Obslugiwane interakcje: zmiana pola filtra, klikniecie "Wyczysc".
- Walidacja: none, ale trimowanie search, min 2 znaki przed wyszukaniem.
- Typy: `HabitListFilterVm`, `HabitSortOptionVm`.
- Propsy: `Filters`, `OnFiltersChanged`, `IsBusy`.

### HabitList
- Opis: Renderuje liste nawykow i obsluguje paginacje/sortowanie po stronie serwera.
- Glowne elementy: `MudTable` z `ServerData` albo `MudList` + `MudPagination`, `MudProgressLinear` dla loadingu.
- Obslugiwane interakcje: klik na wiersz (opcjonalnie przejscie do szczegolow), akcje w `HabitItem`.
- Walidacja: none.
- Typy: `HabitListItemVm`, `PagedResponseOfHabitResponse`.
- Propsy: `Items`, `TotalCount`, `Page`, `PageSize`, `OnPageChanged`, `OnSortChanged`, `OnEdit`, `OnDelete`, `OnCheckin`.

### HabitItem
- Opis: Pojedynczy wiersz/karta nawyku z kluczowymi danymi i akcjami.
- Glowne elementy: `MudPaper`/`MudCard`, `MudText`, `MudChip` (typ, completionMode), `MudBadge` (success_rate), `MudIconButton` (edit/delete/check-in), mini etykiety harmonogramu i deadline.
- Obslugiwane interakcje: edit, delete, check-in, opcjonalnie otwarcie szczegolow.
- Walidacja: blokada check-in gdy trwa wysylka lub gdy API zwraca 409/422.
- Typy: `HabitListItemVm`.
- Propsy: `Item`, `OnEdit`, `OnDelete`, `OnCheckin`.

### HabitFormDialog
- Opis: Dialog do tworzenia i edycji nawyku (zgodny z US-005/006/007).
- Glowne elementy: `MudDialog`, `EditForm`, `MudTextField` (title, description), `MudSelect` (type, completionMode), `MudCheckBox`/`MudToggleGroup` (dni tygodnia), `MudNumericField` (targetValue), `MudTextField` (targetUnit), `MudDatePicker` (deadline), `MudButton` (save/cancel).
- Obslugiwane interakcje: zapis, anulowanie, przelaczenie trybu binary/quantitative (ukrywanie targetValue dla binary).
- Walidacja:
  - title wymagany, <= 80 znakow
  - description <= 280 znakow
  - daysOfWeekMask musi miec min. 1 dzien
  - targetValue 1..100 (dla quantitative)
  - deadline opcjonalny; przy edycji mozliwosc "czyszczenia" deadline
- Typy: `HabitFormModel`, `CreateHabitRequest`, `UpdateHabitRequest`.
- Propsy: `Mode` (Create/Edit), `InitialModel`, `OnSubmit`, `OnCancel`.

### ConfirmDialog
- Opis: Modal potwierdzajacy usuniecie nawyku (US-008).
- Glowne elementy: `MudDialog`, `MudText`, `MudButton` (confirm/cancel).
- Obslugiwane interakcje: potwierdz, anuluj.
- Walidacja: none (jedynie disable podczas submit).
- Typy: `ConfirmDialogOptions`.
- Propsy: `Title`, `Message`, `OnConfirm`, `OnCancel`.

### CheckinDialog
- Opis: Dialog do check-in z lista (ponowne uzycie komponentu z Today lub przeniesienie do Shared).
- Glowne elementy: `MudDialog`, `MudDatePicker` (ostatnie 7 dni), `MudNumericField` lub `MudCheckBox` (binary), `MudAlert` na blad.
- Obslugiwane interakcje: zapis check-in, anulowanie.
- Walidacja:
  - data w zakresie ostatnich 7 dni
  - actualValue 0..targetValue
- Typy: `CheckinTargetVm`, `CreateCheckinRequest`.
- Propsy: `Item`, `LocalDate`, `OnSubmit`, `OnCancel`.

## 5. Typy
- `HabitListFilterVm`
  - `int Page` (>=1)
  - `int PageSize` (np. 10/20/50)
  - `HabitType? Type`
  - `CompletionMode? CompletionMode`
  - `bool? Active` (true = aktywne, false = archiwalne, null = wszystkie)
  - `string? Search`
  - `HabitSortField SortField`
  - `SortDirection SortDirection`
- `HabitListState`
  - `bool IsLoading`
  - `string? ErrorMessage`
  - `List<HabitListItemVm> Items`
  - `int TotalCountFiltered`
  - `int TotalCountAll`
  - `HabitListFilterVm Filters`
- `HabitListItemVm`
  - `int Id`
  - `string Title`
  - `string? Description`
  - `HabitType Type`
  - `CompletionMode CompletionMode`
  - `int DaysOfWeekMask`
  - `string ScheduleLabel` (np. "Pn, Sr, Pt")
  - `int TargetValue`
  - `string? TargetUnit`
  - `DateOnly? DeadlineDate`
  - `DateTimeOffset CreatedAtUtc`
  - `string? SuccessRateShort` (np. "74% / 7d" lub "-" dopoki nie zaladowane)
  - `bool IsBusy`
- `HabitFormModel` (z atrybutami DataAnnotations)
  - `string Title`
  - `string? Description`
  - `HabitType Type`
  - `CompletionMode CompletionMode`
  - `byte DaysOfWeekMask`
  - `int? TargetValue`
  - `string? TargetUnit`
  - `DateOnly? DeadlineDate`
  - `bool ClearDeadline`
- `CheckinTargetVm`
  - `int HabitId`
  - `string Title`
  - `HabitType Type`
  - `CompletionMode CompletionMode`
  - `int TargetValue`
  - `string? TargetUnit`
- DTO z klienta NSwag:
  - `PagedResponseOfHabitResponse`, `HabitResponse`
  - `CreateHabitRequest`, `UpdateHabitRequest`
  - `CreateCheckinRequest`
  - enumy: `HabitType`, `CompletionMode`, `HabitSortField`, `SortDirection`

## 6. Zarzadzanie stanem
- Stan strony trzymany lokalnie w `Habits.razor` (`HabitListState`).
- `CancellationTokenSource` na kazde ladowanie listy i na wywolania akcji (create/update/delete/check-in); anulowanie w `Dispose`.
- Debounce dla `Search` (np. 300-500 ms) aby nie odpalac nadmiarowych requestow.
- `SuccessRateShort` lazily: osobne wywolania `GetProgressRollingAsync` dla widocznych elementow (max 20), z cache na czas sesji strony.
- Stan dialogow: `SelectedHabit` i `DialogMode` + flagi `IsSubmitting`.

## 7. Integracja API
- Lista nawykow:
  - `GetHabitsAsync(page, pageSize, type, completionMode, active, search, sortField, sortDirection)`
  - Odpowiedz: `PagedResponseOfHabitResponse` -> mapowanie na `HabitListItemVm`.
- Tworzenie nawyku:
  - `CreateHabitAsync(CreateHabitRequest)` -> 201; w UI po sukcesie: odswiez liste i licznik N/20.
  - 409: limit 20 nawykow (pokaz komunikat i zablokuj przycisk tworzenia).
- Edycja nawyku:
  - `UpdateHabitAsync(id, UpdateHabitRequest)` -> 200; odswiez liste.
  - Przy czyszczeniu deadline: `ClearDeadline = true`.
- Usuwanie nawyku:
  - `DeleteHabitAsync(id)` -> 204; odswiez liste i licznik.
- Check-in z listy:
  - `CreateCheckinAsync(habitId, CreateCheckinRequest)` -> 201; pokaz toast i opcjonalnie odswiez wiersz.
- Bledy: `ApiException` z kodami 400/401/403/404/409/422 i mapowanie na komunikaty.

## 8. Interakcje uzytkownika
- Wejscie na /habits -> ladowanie listy, pokazanie loadera.
- Uzycie filtrow/szukajki -> odswieza liste (server side).
- Klik "Dodaj nawyk" -> otwiera `HabitFormDialog` w trybie Create.
- Klik "Edytuj" przy nawyku -> otwiera `HabitFormDialog` w trybie Edit z wypelnionymi polami.
- Klik "Usun" -> `ConfirmDialog`, po potwierdzeniu usuniecie i refresh.
- Klik "Check-in" -> `CheckinDialog`, zapis i komunikat sukcesu.
- Wyswietlenie licznika `N/20` i blokada tworzenia po osiagnieciu limitu.
- Mapowanie historyjek:
  - US-005/US-006: create w `HabitFormDialog` + walidacje
  - US-007: update w `HabitFormDialog`
  - US-008: delete w `ConfirmDialog`
  - US-009: `HabitList` z filtrowaniem i paginacja
  - US-021: komunikaty bledow i limit 20

## 9. Warunki i walidacja
- Tytul: wymagany, max 80 znakow.
- Opis: max 280 znakow.
- Powtorzenia (targetValue): 1..100 dla `CompletionMode.Quantitative`.
- Dni tygodnia: min. jeden dzien (mask != 0).
- Deadline: opcjonalny; przy edycji obsluga `ClearDeadline`.
- Check-in: data w zakresie ostatnich 7 dni i wartosc 0..targetValue.
- Limit 20 nawykow: blokada UI + komunikat przy 409.

## 10. Obsluga bledow
- 401: przekierowanie do `/auth/login?returnUrl=/habits`.
- 403/404: komunikat "Brak dostepu" / "Nawyk nie istnieje".
- 400: walidacja formularza (wyswietlenie bledow na polach).
- 409: limit nawykow albo duplikat check-inu -> toast/alert.
- 422: check-in poza zakresem/niezaplanowany -> komunikat w dialogu.
- Brak sieci: ogolny alert i mozliwosc ponowienia ladowania.

## 11. Kroki implementacji
1. Przygotuj strukture komponentow w `Components/Pages/Habits` oraz ewentualnie przenies `CheckinDialog` do `Components/Shared` (jezeli ma byc uzywany w Habits i Today).
2. Zbuduj `HabitFormDialog` z `EditForm` i walidacja DataAnnotations.
3. Dodaj `HabitFilters` i obsluge stanu filtrow (debounce search, reset strony po zmianie filtrow).
4. Zaimplementuj `HabitList` z paginacja i sortowaniem po stronie serwera.
5. Zaimplementuj `HabitItem` z akcjami edit/delete/check-in oraz prezentacja pol (harmonogram, deadline, success_rate).
6. Dodaj integracje API: `GetHabits`, `CreateHabit`, `UpdateHabit`, `DeleteHabit`, `CreateCheckin` + mapowanie bledow.
7. Dodaj licznik `N/20` i logike blokady tworzenia wraz z obsluga 409.
8. Dodaj lazy loading `SuccessRateShort` (opcjonalnie przez `GetProgressRollingAsync`), z cache i anulowaniem.
9. Dodaj komunikaty UX (snackbary/alerty) oraz obsluge retry.
10. Upewnij sie, ze `_Imports.razor` zawiera `@using HabitFlow.Blazor.Components.Pages.Habits` (i `Shared` po przeniesieniu dialogu).
