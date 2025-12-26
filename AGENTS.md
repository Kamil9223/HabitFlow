# Wytyczne Repozytorium

## Struktura projektu i organizacja modułów
- `HabitFlow.sln` — korzeń rozwiązania grupujący Backend i Frontend.
- `HabitFlow.Api/` — ASP.NET Core Minimal API (OpenAPI w trybie Development).
- `HabitFlow.Blazor/` — aplikacja Blazor Server (`Components/`, `wwwroot/`).
- `appsettings.json` oraz `appsettings.Development.json` istnieją w obu projektach do konfiguracji.

## Polecenia budowania, testów i uruchamiania
- `dotnet restore` — przywraca zależności dla rozwiązania.
- `dotnet build` — buduje wszystkie projekty w trybie Debug.
- `dotnet run --project HabitFlow.Api` — uruchamia API (np. `/weatherforecast`).
- `dotnet run --project HabitFlow.Blazor` — uruchamia aplikację Blazor Server.
- `dotnet watch run --project <ProjectDir>` — hot‑reload podczas developmentu.

## Styl kodowania i konwencje nazewnicze
- Język: C# (net9.0), włączone `Nullable` i `ImplicitUsings`.
- Wcięcia: 4 spacje; przestrzenie nazw w stylu file‑scoped; członkowie wyrażeniowi (expression‑bodied), gdy poprawia to czytelność.
- Nazewnictwo: `PascalCase` dla typów/metod/właściwości; `camelCase` dla zmiennych lokalnych/parametrów; `_camelCase` dla pól prywatnych.
- Formatowanie: uruchom `dotnet format` przed commitowaniem.

## Backend

### Architektura

- Stosuj Clean Architecture: Domain → Application → Infrastructure → Api.
- Stosuj CQS: rozdzielaj ścieżki poleceń (Command) i zapytań (Query).
- Bez MediatR. Użyj lekkiego wewnętrznego dyspozytora i kontraktów:
  - `ICommand`, `ICommandHandler<TCommand, TResult>`
  - `IQuery<TResult>`, `IQueryHandler<TQuery, TResult>`
  - `ICommandDispatcher`, `IQueryDispatcher` (zarejestrowane w DI)
- Walidacja powinna zachodzić przed wykonaniem handlera (np. zachowanie pipeline w dyspozytorach).
- Wszystkie handlery obsługują `CancellationToken`.

### Entity Framework Core (SQL Server)

- Code First z migracjami jako źródłem prawdy.
- Zapobiegaj N+1 przez `Include/ThenInclude` tylko tam, gdzie konieczne.
- Zapytania: zawsze `AsNoTracking` i projekcja przez `Select` do DTO/rekordów, aby ograniczyć kolumny.
- Optymalizuj gorące ścieżki skompilowanymi zapytaniami i dbaj o odpowiednie indeksy w bazie.
- Używaj konwerterów wartości dla własnych obiektów wartości i wyspecjalizowanych identyfikatorów.

### Repozytorium i Unit of Work

- Repozytoria używane wyłącznie dla poleceń (zmiany stanu) w warstwie Infrastructure; `DbContext` pełni rolę Unit of Work.
- Handlery zapytań nie używają repozytoriów:
  - Opcja A: wstrzykuj `DbContext` bezpośrednio do handlerów zapytań (polityka tylko‑do‑odczytu, `AsNoTracking`).
  - Opcja B: zdefiniuj interfejsy read‑side w Application (np. `IFooReadStore`), implementowane w Infrastructure na EF Core, aby nie wypychać typów EF do Application.
  - Opcja C: udostępniaj wyspecjalizowane usługi odczytu w Infrastructure, które zwracają wstępnie ukształtowane DTO lub `IAsyncEnumerable` dla scenariuszy strumieniowania.
- Transakcje: opieraj się na `DbContext.SaveChanges/SaveChangesAsync`; używaj jawnych transakcji/`TransactionScope` tylko, gdy jedna jednostka pracy obejmuje wiele agregatów/zasobów.

### ASP.NET API

- Używaj Minimal APIs do endpointów.
- Konwencje mapowania endpointów:
  - POST/PUT/PATCH/DELETE → wysyłaj Command przez `ICommandDispatcher`.
  - GET → wysyłaj Query przez `IQueryDispatcher`.
- Błędy i wyniki:
  - Preferuj `Result/Result<T>` dla błędów domenowych/walidacyjnych/oczekiwanych; nie używaj wyjątków do sterowania przepływem.
  - Handlery zwracają `Result<T>`; endpointy tłumaczą `Result` na odpowiedzi HTTP.
  - Globalny mapper ProblemDetails konwertuje niepowodzenia na `ProblemDetails` zgodne z RFC 7807 z konsekwentnymi kodami błędów, tytułami i szczegółami.
  - Wyjątki zarezerwuj tylko dla nieoczekiwanych usterek; globalny handler mapuje je na `500 ProblemDetails`.
- DI: `DbContext` ma zasięg scoped; zarejestruj dyspozytory i przeskanuj/zarejestruj handlery (lub zarejestruj je jawnie).

### Baza danych i SQL Server

- Zapytania parametryzowane (domyślnie w EF Core), aby zapobiegać SQL injection.
- Indeksuj zgodnie z wzorcami odczytu/zapisu oraz typowymi filtrami/sortowaniem.
- Rozważ widoki lub wstępnie obliczone modele odczytu dla ciężkiego raportowania, jeśli potrzebne.

## Frontend

- Model: renderowanie po stronie serwera z połączeniem SignalR (circuit).
- Dostęp do danych:
  - Opcja MVP: bezpośredni dostęp do warstwy Application/Dispatcher z `HabitFlow.Blazor` (bez HTTP), aby uniknąć dublowania kontraktów.
  - Alternatywa: typowany `HttpClient` do Minimal API i wymiana Command/Query DTO; mapowanie `ProblemDetails` na komunikaty UI.
- Struktura `HabitFlow.Blazor/`:
  - `Components/` (np. `Habits/`, `Today/`, `Calendar/`, `Charts/`, `Shared/`), małe komponenty, jeden publiczny komponent na plik.
  - `Pages/` (strony routowane łączące komponenty domenowe), `Services/` (np. `TimeZoneService`, `NotificationService`, adapter API/Dispatcher), `wwwroot/` (style, JS).
- Komponenty MVP (sugestie):
  - `Today/TodayChecklist.razor` (lista kroków na dziś, szybki check‑in, optymistyczny update, blokada podwójnego wysłania).
  - `Habits/HabitList.razor` + `Habits/HabitItem.razor` (lista/wiersz nawyku), `Habits/HabitForm.razor` (`EditForm` + `DataAnnotationsValidator`).
  - `Calendar/CalendarView.razor` (readonly, podświetlenie „done/miss”), `Charts/RollingSuccessChart.razor` (Chart.js lub biblioteka komponentów).
  - `Shared/Notifications.razor` (notyfikacje motywacyjne in‑app), `Shared/ErrorBoundary.razor` (przyjazne błędy + logowanie).
- Stan i komunikacja:
  - Usługi `Scoped` do stanu UI (preferencje, filtry); minimalizacja dużych obiektów w circuit.
  - Zawsze przekazuj `CancellationToken` w operacjach async; anuluj przy nawigacji.
  - `AuthenticationStateProvider`, `AuthorizeView`, `[Authorize]` na stronach i sekcjach wrażliwych.
- Walidacja, czas i DTO:
  - `EditForm` + `DataAnnotations`; serwerowa walidacja zwraca `ProblemDetails`, UI mapuje błędy na pola.
  - Czas lokalny użytkownika: render i obliczenia success_rate według strefy z profilu.
  - Smukłe DTO do list/kalendarza; bez nadmiarowych kolumn.
- Wydajność i UX:
  - Minimalizuj re‑render (wydzielanie komponentów, uważne parametry, `ShouldRender` w hot‑pathach).
  - `Virtualize` dla długich list; wskaźniki ładowania; empty states; brak `async void` w eventach.
  - Reconnect UI: informacja o utraconym połączeniu SignalR i automatyczne ponowienie.
- Autoryzacja i bezpieczeństwo:
  - Nawigacja chroniona `[Authorize]`; spójne przekierowania; brak wrażliwych danych w storage przeglądarki.
  - Backend egzekwuje idempotencję i rate‑limiting (np. check‑in); UI obsługuje 409/422.
- Styl i biblioteki UI:
  - MudBlazor jako jedyna biblioteka UI; nie mieszaj z Bootstrap/Tailwind ani innymi frameworkami.
  - Listy/tabele: komponenty MudBlazor; wykres: Chart.js przez maly interop lub komponent biblioteczny (jeden wybor).
- JS interop:
  - Oszczędnie (gł. wykresy, drobne efekty); opakować w usługę/komponent; dbać o lifecycle i rozłączenia.
- Testy:
  - bUnit (render, walidacja, interakcje) i E2E (Playwright) dla ścieżki z PRD: „Rejestracja → nawyk → check‑in → kalendarz/wykres → notyfikacja (miss)”.
- Dostępność i i18n:
  - Role ARIA, kontrast, focus management, klawiatura; `RequestLocalizationOptions` i `CultureInfo` per użytkownik.
- Logowanie i diagnostyka:
  - `ILogger` w komponentach brzegowych; centralne logowanie błędów z `ErrorBoundary`; akcje UI idempotentne.

## Wytyczne testowania
  - Projekt testowy: `HabitFlow.Tests`
  - Do testów używaj XUnit.
  - Testy jednostkowe:
    - Testy jednostkowe powinny znajdować się w podfolderze UnitTests.
    - Do mocków jeśli potrzebne używaj bibliotekę NSubstitute.
  - Testy integracyjne:
    - Testy integracyjne powinny znajdować się w podfolderze IntegrationTests.
    - Testy integracyjne polegają na testowaniu flow logiki całych endpointów 0 bez mocków.
    - Należy korzystać z TestContainers aby zasetupować bazę danych, oraz generowanego klienta http, dzięki któremu będzie można w testach odpytywać endpointy.
    - Baza danych powinna być jedna dla wszystkich uruchamianych testów.
    - Testy uruchamiaj równolegle.
  - Nazwy plików `*Tests.cs`; jedna klasa na jednostkę testowaną.
  - Uruchamiaj `dotnet test` (dodaj do rozwiązania po utworzeniu).

## Wytyczne commitów i pull requestów
- Commity: zwięzłe, w trybie rozkazującym. Preferowane Conventional Commits:
- Przykłady: `feat(api): add habit endpoints`, `fix(blazor): correct nav styling`.
- PR: dołącz cel, powiązane issue (np. `Closes #123`), kroki testowe i zrzuty ekranu dla UI.
- Utrzymuj zmiany skupione; aktualizuj dokumentację/konfigurację, gdy zmienia się zachowanie.

## Bezpieczeństwo i konfiguracja
- Nie commituj sekretów. Preferuj zmienne środowiskowe lub `dotnet user-secrets` w development.
- Lokalnie używaj `ASPNETCORE_ENVIRONMENT=Development`. Zaufaj certyfikatom HTTPS przez `dotnet dev-certs https --trust`.
- Umieszczaj niesekretne ustawienia w `appsettings.Development.json`; wartości produkcyjne przez środowisko lub magazyn sekretów.

## Instrukcje specyficzne dla agenta
- Ogranicz zmiany do dotkniętego projektu (`Api` lub `Blazor`).
- Preferuj Minimal APIs i rekordy w API; w Blazor używaj komponentów Razor z trybem renderowania server.
- Dodając nowe projekty, umieszczaj je w istniejących folderach rozwiązania (Backend/Frontend) w `HabitFlow.sln`.

## Specyfikacja produktu (PRD)
- Główny dokument PRD: `.ai/prd.md`.
- Aktualizuj przy zmianach zakresu/prioritetów i linkuj powiązane issue/PR.
