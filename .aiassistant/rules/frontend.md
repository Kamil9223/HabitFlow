---
apply: by file patterns
patterns: HabitFlow.Blazor/**
---

# Frontend Rules - HabitFlow.Blazor

## Model renderowania

- Blazor Server z połączeniem SignalR (circuit)
- Dostęp do danych:
  - Opcja MVP: bezpośredni dostęp do warstwy Application/Dispatcher z `HabitFlow.Blazor` (bez HTTP), aby uniknąć dublowania kontraktów
  - Alternatywa: typowany `HttpClient` do Minimal API i wymiana Command/Query DTO; mapowanie `ProblemDetails` na komunikaty UI

## Struktura projektu

- `Components/` (np. `Habits/`, `Today/`, `Calendar/`, `Charts/`, `Shared/`), małe komponenty, jeden publiczny komponent na plik
- `Pages/` (strony routowane łączące komponenty domenowe)
- `Services/` (np. `TimeZoneService`, `NotificationService`, adapter API/Dispatcher)
- `wwwroot/` (style, JS)

## Komponenty MVP (sugestie)

- `Today/TodayChecklist.razor` (lista kroków na dziś, szybki check‑in, optymistyczny update, blokada podwójnego wysłania)
- `Habits/HabitList.razor` + `Habits/HabitItem.razor` (lista/wiersz nawyku), `Habits/HabitForm.razor` (`EditForm` + `DataAnnotationsValidator`)
- `Calendar/CalendarView.razor` (readonly, podświetlenie „done/miss"), `Charts/RollingSuccessChart.razor` (Chart.js lub biblioteka komponentów)
- `Shared/Notifications.razor` (notyfikacje motywacyjne in‑app), `Shared/ErrorBoundary.razor` (przyjazne błędy + logowanie)

## Stan i komunikacja

- Usługi `Scoped` do stanu UI (preferencje, filtry); minimalizacja dużych obiektów w circuit
- Zawsze przekazuj `CancellationToken` w operacjach async; anuluj przy nawigacji
- `AuthenticationStateProvider`, `AuthorizeView`, `[Authorize]` na stronach i sekcjach wrażliwych

## Walidacja, czas i DTO

- `EditForm` + `DataAnnotations`; serwerowa walidacja zwraca `ProblemDetails`, UI mapuje błędy na pola
- Czas lokalny użytkownika: render i obliczenia success_rate według strefy z profilu
- Smukłe DTO do list/kalendarza; bez nadmiarowych kolumn

## Wydajność i UX

- Minimalizuj re‑render (wydzielanie komponentów, uważne parametry, `ShouldRender` w hot‑pathach)
- `Virtualize` dla długich list; wskaźniki ładowania; empty states; brak `async void` w eventach
- Reconnect UI: informacja o utraconym połączeniu SignalR i automatyczne ponowienie

## Autoryzacja i bezpieczeństwo

- Nawigacja chroniona `[Authorize]`; spójne przekierowania; brak wrażliwych danych w storage przeglądarki
- Backend egzekwuje idempotencję i rate‑limiting (np. check‑in); UI obsługuje 409/422

## Styl i biblioteki UI

- Bazowo Bootstrap 5 + własne zmienne lub jedna biblioteka (np. MudBlazor) – bez mieszania wielu
- Listy/tabele: `QuickGrid`/komponent biblioteczny; wykres: Chart.js przez mały interop lub komponent biblioteczny (jeden wybór)

## JS interop

- Oszczędnie (gł. wykresy, drobne efekty); opakować w usługę/komponent; dbać o lifecycle i rozłączenia

## Testy

- bUnit (render, walidacja, interakcje) i E2E (Playwright) dla ścieżki z PRD: „Rejestracja → nawyk → check‑in → kalendarz/wykres → notyfikacja (miss)"

## Dostępność i i18n

- Role ARIA, kontrast, focus management, klawiatura; `RequestLocalizationOptions` i `CultureInfo` per użytkownik

## Logowanie i diagnostyka

- `ILogger` w komponentach brzegowych; centralne logowanie błędów z `ErrorBoundary`; akcje UI idempotentne
