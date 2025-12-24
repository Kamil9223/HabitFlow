---
apply: by file patterns
patterns: HabitFlow.Api/**
---

ckend Rules - HabitFlow.Api

## Struktura projektu

- `HabitFlow.sln` — korzeń rozwiązania grupujący Backend i Frontend
- `HabitFlow.Api/` — ASP.NET Core Minimal API (OpenAPI w trybie Development)
- `HabitFlow.Blazor/` — aplikacja Blazor Server (`Components/`, `wwwroot/`)
- `appsettings.json` oraz `appsettings.Development.json` istnieją w obu projektach do konfiguracji

## Polecenia budowania i uruchamiania

- `dotnet restore` — przywraca zależności dla rozwiązania
- `dotnet build` — buduje wszystkie projekty w trybie Debug
- `dotnet run --project HabitFlow.Api` — uruchamia API
- `dotnet run --project HabitFlow.Blazor` — uruchamia aplikację Blazor Server
- `dotnet watch run --project <ProjectDir>` — hot‑reload podczas developmentu

## Styl kodowania i konwencje

- Język: C# (net9.0), włączone `Nullable` i `ImplicitUsings`
- Wcięcia: 4 spacje; przestrzenie nazw w stylu file‑scoped; członkowie wyrażeniowi (expression‑bodied), gdy poprawia to czytelność
- Nazewnictwo:
  - `PascalCase` dla typów/metod/właściwości
  - `camelCase` dla zmiennych lokalnych/parametrów
  - `_camelCase` dla pól prywatnych
- Formatowanie: uruchom `dotnet format` przed commitowaniem

## Wytyczne commitów i pull requestów

- Commity: zwięzłe, w trybie rozkazującym. Preferowane Conventional Commits
- Przykłady: `feat(api): add habit endpoints`, `fix(blazor): correct nav styling`
- PR: dołącz cel, powiązane issue (np. `Closes #123`), kroki testowe i zrzuty ekranu dla UI
- Utrzymuj zmiany skupione; aktualizuj dokumentację/konfigurację, gdy zmienia się zachowanie

## Bezpieczeństwo i konfiguracja

- Nie commituj sekretów. Preferuj zmienne środowiskowe lub `dotnet user-secrets` w development
- Lokalnie używaj `ASPNETCORE_ENVIRONMENT=Development`. Zaufaj certyfikatom HTTPS przez `dotnet dev-certs https --trust`
- Umieszczaj niesekretne ustawienia w `appsettings.Development.json`; wartości produkcyjne przez środowisko lub magazyn sekretów

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

## Specyfikacja produktu

- Główny dokument PRD: `.ai/prd.md`
- Aktualizuj przy zmianach zakresu/prioritetów i linkuj powiązane issue/PR

## Instrukcje dla agenta

- Ogranicz zmiany do dotkniętego projektu (`Api` lub `Blazor`)
- Preferuj Minimal APIs i rekordy w API; w Blazor używaj komponentów Razor z trybem renderowania server
- Dodając nowe projekty, umieszczaj je w istniejących folderach rozwiązania (Backend/Frontend) w `HabitFlow.sln`

## Architektura

- Stosuj Clean Architecture: Domain → Application → Infrastructure → Api
- Stosuj CQS: rozdzielaj ścieżki poleceń (Command) i zapytań (Query)
- Bez MediatR. Użyj lekkiego wewnętrznego dyspozytora i kontraktów:
  - `ICommand`, `ICommandHandler<TCommand, TResult>`
  - `IQuery<TResult>`, `IQueryHandler<TQuery, TResult>`
  - `ICommandDispatcher`, `IQueryDispatcher` (zarejestrowane w DI)
- Walidacja powinna zachodzić przed wykonaniem handlera (np. zachowanie pipeline w dyspozytorach)
- Wszystkie handlery obsługują `CancellationToken`

## Entity Framework Core (SQL Server)

- Code First z migracjami jako źródłem prawdy
- Zapobiegaj N+1 przez `Include/ThenInclude` tylko tam, gdzie konieczne
- Zapytania: zawsze `AsNoTracking` i projekcja przez `Select` do DTO/rekordów, aby ograniczyć kolumny
- Optymalizuj gorące ścieżki skompilowanymi zapytaniami i dbaj o odpowiednie indeksy w bazie
- Używaj konwerterów wartości dla własnych obiektów wartości i wyspecjalizowanych identyfikatorów

## Repozytorium i Unit of Work

- Repozytoria używane wyłącznie dla poleceń (zmiany stanu) w warstwie Infrastructure; `DbContext` pełni rolę Unit of Work
- Handlery zapytań nie używają repozytoriów:
  - Opcja A: wstrzykuj `DbContext` bezpośrednio do handlerów zapytań (polityka tylko‑do‑odczytu, `AsNoTracking`)
  - Opcja B: zdefiniuj interfejsy read‑side w Application (np. `IFooReadStore`), implementowane w Infrastructure na EF Core
  - Opcja C: udostępniaj wyspecjalizowane usługi odczytu w Infrastructure, które zwracają wstępnie ukształtowane DTO lub `IAsyncEnumerable`
- Transakcje: opieraj się na `DbContext.SaveChanges/SaveChangesAsync`; używaj jawnych transakcji/`TransactionScope` tylko, gdy jedna jednostka pracy obejmuje wiele agregatów/zasobów

## ASP.NET API

- Używaj Minimal APIs do endpointów
- Konwencje mapowania endpointów:
  - POST/PUT/PATCH/DELETE → wysyłaj Command przez `ICommandDispatcher`
  - GET → wysyłaj Query przez `IQueryDispatcher`
- Błędy i wyniki:
  - Preferuj `Result/Result<T>` dla błędów domenowych/walidacyjnych/oczekiwanych; nie używaj wyjątków do sterowania przepływem
  - Handlery zwracają `Result<T>`; endpointy tłumaczą `Result` na odpowiedzi HTTP
  - Globalny mapper ProblemDetails konwertuje niepowodzenia na `ProblemDetails` zgodne z RFC 7807 z konsekwentnymi kodami błędów, tytułami i szczegółami
  - Wyjątki zarezerwuj tylko dla nieoczekiwanych usterek; globalny handler mapuje je na `500 ProblemDetails`
- DI: `DbContext` ma zasięg scoped; zarejestruj dyspozytory i przeskanuj/zarejestruj handlery (lub zarejestruj je jawnie)

## Baza danych i SQL Server

- Zapytania parametryzowane (domyślnie w EF Core), aby zapobiegać SQL injection
- Indeksuj zgodnie z wzorcami odczytu/zapisu oraz typowymi filtrami/sortowaniem
- Rozważ widoki lub wstępnie obliczone modele odczytu dla ciężkiego raportowania, jeśli potrzebne

