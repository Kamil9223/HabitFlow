---
apply: always
---

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

- Brak projektów testowych na ten moment. Preferowane xUnit:
  - Utwórz `HabitFlow.Api.Tests/` i `HabitFlow.Blazor.Tests/`
  - Nazwy plików `*Tests.cs`; jedna klasa na jednostkę testowaną
  - Uruchamiaj `dotnet test` (dodaj do rozwiązania po utworzeniu)

## Specyfikacja produktu

- Główny dokument PRD: `.ai/prd.md`
- Aktualizuj przy zmianach zakresu/prioritetów i linkuj powiązane issue/PR

## Instrukcje dla agenta

- Ogranicz zmiany do dotkniętego projektu (`Api` lub `Blazor`)
- Preferuj Minimal APIs i rekordy w API; w Blazor używaj komponentów Razor z trybem renderowania server
- Dodając nowe projekty, umieszczaj je w istniejących folderach rozwiązania (Backend/Frontend) w `HabitFlow.sln`
