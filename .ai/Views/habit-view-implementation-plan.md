# Plan implementacji widoku Habit Details

## 1. Przeglad
Widok Habit Details prezentuje szczegoly pojedynczego nawyku wraz z kalendarzem readonly i wykresem rolling success rate (7/30). Celem jest szybki wglad w konfiguracje nawyku, harmonogram, postep oraz kontekstowe podpowiedzi dotyczace start/stop i progu 75%.

## 2. Routing widoku
Sciezka: `/habits/{id}` (strona routowana w `HabitFlow.Blazor/Components/Pages/Habits/HabitDetails.razor`).

## 3. Struktura komponentow
- HabitDetailsPage
  - HabitDetailsHeader
    - HabitSwitchDropdown
    - HeaderMetaBadges
  - HabitDetailsTabs
    - TabCalendar
      - CalendarView
      - CalendarLegend
      - DayDetailsTooltip (readonly)
    - TabProgress
      - RollingWindowSwitch
      - RollingSuccessChart
      - ProgressSummary
  - ErrorStateCard
  - LoadingSkeleton

## 4. Szczegoly komponentow
### HabitDetailsPage
- Opis komponentu: Strona routowana, pobiera dane nawyku, kalendarz i wykres, zarzadza stanem, obsluguje 404/401.
- Glowne elementy: `MudContainer`, `MudStack`, `MudProgressCircular` lub `MudSkeleton`, `MudAlert`.
- Obslugiwane interakcje: inicjalny fetch, zmiana nawyku z dropdown, przelaczanie zakladek, przelaczanie okna 7/30.
- Obslugiwana walidacja: brak bezposredniej, deleguje do CheckinDialog i walidacji API.
- Typy: `HabitResponse`, `HabitCalendarResponse`, `ProgressRollingResponse`, `HabitDetailsViewState`, `HabitDetailsVm`.
- Propsy: brak (komponent routowany).

### HabitDetailsHeader
- Opis komponentu: Naglowek widoku z tytulem, opisem i meta danymi (typ, completion mode, harmonogram, target, deadline, success rate).
- Glowne elementy: `MudText`, `MudChip`, `MudBadge`, `MudDivider`, `MudTooltip`.
- Obslugiwane interakcje: otwarcie dropdownu, hover tooltipow.
- Obslugiwana walidacja: brak.
- Typy: `HabitHeaderVm`.
- Propsy: `HabitHeaderVm Model`, `EventCallback<int> OnHabitChange`.

### HabitSwitchDropdown
- Opis komponentu: Szybkie przelaczenie na inny nawyk uzytkownika.
- Glowne elementy: `MudSelect<int>`, `MudSelectItem`.
- Obslugiwane interakcje: zmiana wybranego habitId.
- Obslugiwana walidacja: brak.
- Typy: `HabitListItemVm`.
- Propsy: `int SelectedHabitId`, `IReadOnlyList<HabitListItemVm> Items`, `EventCallback<int> OnChange`.

### HabitDetailsTabs
- Opis komponentu: Kontener zakladek "Calendar" i "Progress".
- Glowne elementy: `MudTabs`, `MudTabPanel`.
- Obslugiwane interakcje: zmiana zakladki.
- Obslugiwana walidacja: brak.
- Typy: brak.
- Propsy: `int ActiveTab`, `EventCallback<int> OnTabChange`.

### TabCalendar
- Opis komponentu: Zakladka z kalendarzem readonly.
- Glowne elementy: `MudPaper`, `CalendarView`, `CalendarLegend`.
- Obslugiwane interakcje: klik dnia (readonly, pokazuje tooltip z danymi dnia).
- Obslugiwana walidacja: brak.
- Typy: `HabitCalendarVm`, `CalendarDayVm`.
- Propsy: `HabitCalendarVm Model`, `EventCallback<CalendarDayVm> OnDaySelect`.

### CalendarView
- Opis komponentu: Siatka dni w zakresie `from/to` z kolorami dla plan/done/miss/partial.
- Glowne elementy: `MudGrid`, `MudTooltip`, `MudPaper`.
- Obslugiwane interakcje: klik dnia -> pokaz szczegolow (readonly).
- Obslugiwana walidacja: brak.
- Typy: `CalendarDayVm`.
- Propsy: `IReadOnlyList<CalendarDayVm> Days`, `DateOnly From`, `DateOnly To`.

### TabProgress
- Opis komponentu: Zakladka z wykresem rolling success rate i podsumowaniem.
- Glowne elementy: `MudPaper`, `RollingWindowSwitch`, `RollingSuccessChart`, `MudText`.
- Obslugiwane interakcje: przelaczenie 7/30.
- Obslugiwana walidacja: brak.
- Typy: `ProgressRollingVm`, `ProgressPointVm`.
- Propsy: `ProgressRollingVm Model`, `EventCallback<int> OnWindowChange`.

### RollingWindowSwitch
- Opis komponentu: Przelacznik okna 7/30 dni.
- Glowne elementy: `MudToggleGroup`, `MudToggleItem` lub `MudSelect<int>`.
- Obslugiwane interakcje: zmiana windowDays.
- Obslugiwana walidacja: tylko 7 lub 30.
- Typy: brak.
- Propsy: `int WindowDays`, `EventCallback<int> OnChange`, `bool IsBusy`.

### RollingSuccessChart
- Opis komponentu: Wykres trendu rolling success rate (7/30) z tooltipem.
- Glowne elementy: komponent wykresu (Chart.js przez interop albo biblioteczny komponent).
- Obslugiwane interakcje: hover tooltip.
- Obslugiwana walidacja: brak.
- Typy: `ProgressPointVm`.
- Propsy: `IReadOnlyList<ProgressPointVm> Points`, `int WindowDays`.

### ErrorStateCard
- Opis komponentu: Blok bledu (401/404/500) z przyciskiem ponowienia.
- Glowne elementy: `MudAlert`, `MudButton`.
- Obslugiwane interakcje: retry.
- Obslugiwana walidacja: brak.
- Typy: brak.
- Propsy: `string Message`, `EventCallback OnRetry`.

## 5. Typy
- `HabitResponse` (API):
  - `int Id`
  - `string Title`
  - `string? Description`
  - `HabitType Type`
  - `CompletionMode CompletionMode`
  - `int DaysOfWeekMask`
  - `int TargetValue`
  - `string? TargetUnit`
  - `DateOnly? DeadlineDate`
  - `DateTimeOffset CreatedAtUtc`
- `HabitCalendarResponse` (API):
  - `int HabitId`
  - `DateOnly From`
  - `DateOnly To`
  - `IReadOnlyList<HabitCalendarDay> Days`
- `HabitCalendarDay` (API):
  - `DateOnly Date`
  - `bool IsPlanned`
  - `int ActualValue`
  - `int? TargetValueSnapshot`
  - `CompletionMode? CompletionModeSnapshot`
  - `HabitType? HabitTypeSnapshot`
  - `double DailyScore`
- `ProgressRollingResponse` (API):
  - `int HabitId`
  - `int WindowDays`
  - `DateOnly Until`
  - `IReadOnlyList<ProgressRollingPoint> Points`
- `ProgressRollingPoint` (API):
  - `DateOnly Date`
  - `int PlannedDays`
  - `double SumDailyScore`
  - `double SuccessRate`
- `HabitDetailsViewState` (UI):
  - `bool IsLoading`
  - `string? ErrorMessage`
  - `HabitDetailsVm? Habit`
  - `HabitCalendarVm? Calendar`
  - `ProgressRollingVm? Progress`
  - `int WindowDays`
  - `DateOnly? CalendarFrom`
  - `DateOnly? CalendarTo`
- `HabitDetailsVm` (UI):
  - `int Id`
  - `string Title`
  - `string? Description`
  - `string TypeLabel`
  - `string CompletionModeLabel`
  - `string ScheduleLabel` (np. "Mon, Wed, Fri")
  - `string TargetLabel` (np. "10 pages")
  - `DateOnly? DeadlineDate`
  - `double SuccessRate`
- `HabitHeaderVm` (UI):
  - `HabitDetailsVm Details`
  - `string SuccessRateLabel`
  - `string DeadlineLabel`
- `HabitCalendarVm` (UI):
  - `DateOnly From`
  - `DateOnly To`
  - `IReadOnlyList<CalendarDayVm> Days`
- `CalendarDayVm` (UI):
  - `DateOnly Date`
  - `bool IsPlanned`
  - `string Status` (Plan/Done/Miss/Partial)
  - `int ActualValue`
  - `int TargetValue`
  - `double DailyScore`
- `ProgressRollingVm` (UI):
  - `int WindowDays`
  - `IReadOnlyList<ProgressPointVm> Points`
- `ProgressPointVm` (UI):
  - `DateOnly Date`
  - `double SuccessRate`
  - `string TooltipLabel` (np. "5/7 wykonane")
- `HabitListItemVm` (UI):
  - `int Id`
  - `string Title`
## 6. Zarzadzanie stanem
- Stan lokalny w `HabitDetailsPage`: `HabitDetailsViewState`.
- Dane pobierane w `OnParametersSetAsync` (zmiana `id`).
- `WindowDays` domyslnie 7; przelacznik odswieza tylko dane progress.
- `CalendarFrom/CalendarTo` ustalane na widoczne okno (np. biezacy miesiac +/- 15 dni), aby nie pobierac nadmiaru.
- Zawsze przekazuj `CancellationToken` i anuluj przy zmianie routingu.

## 7. Integracja API
- GET `/api/v1/habits/{id}` -> `HabitResponse` (detale nawyku).
- GET `/api/v1/habits/{id}/calendar?from=YYYY-MM-DD&to=YYYY-MM-DD` -> `HabitCalendarResponse`.
- GET `/api/v1/habits/{id}/progress/rolling?windowDays=7|30&until=YYYY-MM-DD` -> `ProgressRollingResponse`.
- Warstwa klienta: `HabitDetailsApiClient` w `HabitFlow.Blazor/Services/` z metodami:
  - `GetHabitAsync(int id, CancellationToken)`
  - `GetCalendarAsync(int id, DateOnly from, DateOnly to, CancellationToken)`
  - `GetProgressRollingAsync(int id, int windowDays, DateOnly? until, CancellationToken)`

## 8. Interakcje uzytkownika
- Wejscie na `/habits/{id}` -> fetch detali, kalendarza, progress.
- Zmiana nawyku w dropdown -> nawigacja do `/habits/{id}` i ponowny fetch.
- Przelaczenie tabow -> bez nowego fetch (dane juz w stanie) albo lazy-load jesli preferowane.
- Przelaczenie 7/30 -> fetch progress, odswiezenie wykresu.
- Klik dnia w kalendarzu -> tooltip z danymi dnia (readonly).

## 9. Warunki i walidacja
- `windowDays` musi byc 7 lub 30 (UI ogranicza do tych wartosci).
- `from/to` dla kalendarza musza miec sensowny zakres (from <= to).
- Wskaznik success_rate pokazany jako procent z progami (75% do deadline'u lub rolling).

## 10. Obsluga bledow
- 401: komunikat "Zaloguj sie" i przekierowanie do logowania.
- 404: komunikat "Nawyk nie istnieje lub nie masz dostepu".
- 400: komunikat walidacyjny dla blednych parametrow (np. windowDays, from/to).
- 500/timeout: `ErrorStateCard` z akcja ponowienia.
- Brak danych w progress: pokaz pusty wykres z komunikatem "Brak danych w oknie".

## 11. Kroki implementacji
1. Dodaj strone `HabitFlow.Blazor/Components/Pages/Habits/HabitDetails.razor` z routingiem `/habits/{id:int}`.
2. Dodaj `HabitDetailsApiClient` w `HabitFlow.Blazor/Services/` i zarejestruj w DI.
3. Zaimplementuj `HabitDetailsHeader`, `HabitSwitchDropdown`, `HabitDetailsTabs`, `TabCalendar`, `TabProgress`.
4. Zaimplementuj `CalendarView` z kolorami stanu dnia i tooltipem readonly.
5. Zaimplementuj `RollingSuccessChart` i `RollingWindowSwitch` (wybor 7/30).
6. Dodaj mapowanie DTO -> VM, loading/error/empty states oraz obsluge 401/404.
7. Sprawdz UX na mobile i kontrast kolorow kalendarza oraz tooltipy wyjasniajace start/stop.
