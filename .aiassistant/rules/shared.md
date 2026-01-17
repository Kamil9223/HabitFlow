---
apply: always
---

## Struktura projektu

- `HabitFlow.sln` - korzen rozwiazania grupujacy Backend i Frontend
- `HabitFlow.Api/` - ASP.NET Core Minimal API (OpenAPI w trybie Development)
- `HabitFlow.Blazor/` - aplikacja Blazor Server (`Components/`, `wwwroot/`)
- `appsettings.json` oraz `appsettings.Development.json` istnieja w obu projektach do konfiguracji

## Polecenia budowania i uruchamiania

- `dotnet restore` - przywraca zaleznosci dla rozwiazania
- `dotnet build` - buduje wszystkie projekty w trybie Debug
- `dotnet run --project HabitFlow.Api` - uruchamia API
- `dotnet run --project HabitFlow.Blazor` - uruchamia aplikacje Blazor Server
- `dotnet watch run --project <ProjectDir>` - hot-reload podczas developmentu

## Styl kodowania i konwencje

- Jezyk: C# (net9.0), wlaczone `Nullable` i `ImplicitUsings`
- Wciecia: 4 spacje; przestrzenie nazw w stylu file-scoped; czlonkowie wyrazeniowi (expression-bodied), gdy poprawia to czytelnosc
- Nazewnictwo:
  - `PascalCase` dla typow/metod/wlasciwosci
  - `camelCase` dla zmiennych lokalnych/parametrow
  - `_camelCase` dla pol prywatnych
- Formatowanie: uruchom `dotnet format` przed commitowaniem

## Wytyczne commitow i pull requestow

- Commity: zwiezle, w trybie rozkazujacym. Preferowane Conventional Commits
- Przyklady: `feat(api): add habit endpoints`, `fix(blazor): correct nav styling`
- PR: dolacz cel, powiazane issue (np. `Closes #123`), kroki testowe i zrzuty ekranu dla UI
- Utrzymuj zmiany skupione; aktualizuj dokumentacje/konfiguracje, gdy zmienia sie zachowanie

## Bezpieczenstwo i konfiguracja

- Nie commituj sekretow. Preferuj zmienne srodowiskowe lub `dotnet user-secrets` w development
- Lokalnie uzywaj `ASPNETCORE_ENVIRONMENT=Development`. Zaufaj certyfikatom HTTPS przez `dotnet dev-certs https --trust`
- Umieszczaj niesekretne ustawienia w `appsettings.Development.json`; wartosci produkcyjne przez srodowisko lub magazyn sekretow

## Wytyczne testowania

- Projekt testowy: `HabitFlow.Tests`
- Framework: XUnit; mocking: NSubstitute
- **Szczegolowy plan testow**: `.ai/test-plan.md`

### Struktura testow
- **UnitTests/**: walidatory, handlery, logika biznesowa (≥80% pokrycia dla Core)
- **IntegrationTests/**: endpointy API z TestContainers + NSwag klient (100% pokrycia endpointow)
- **ComponentTests/**: komponenty Blazor z bUnit (≥70% kluczowych komponentow)
- **E2ETests/**: Playwright dla sciezki krytycznej

### Kluczowe zasady
- Testy jednostkowe: happy path + przypadki bledow
- Testy integracyjne: bez mockow, TestContainers (SQL Server), jedna baza, izolacja przez dane
- Testy komponentow: renderowanie, walidacja, interakcje
- Testy E2E: sciezka krytyczna z PRD (rejestracja → nawyk → check-in → kalendarz → notyfikacja)
- Nazwy plikow `*Tests.cs`; jedna klasa na jednostke testowana
- Uruchamiaj `dotnet test`

## Specyfikacja produktu

- Glowny dokument PRD: `.ai/prd.md`
- Aktualizuj przy zmianach zakresu/prioritetow i linkuj powiazane issue/PR

## Instrukcje dla agenta

- Ogranicz zmiany do dotknietego projektu (`Api` lub `Blazor`)
- Preferuj Minimal APIs i rekordy w API; w Blazor uzywaj komponentow Razor z trybem renderowania server
- Dodajac nowe projekty, umieszczaj je w istniejacych folderach rozwiazania (Backend/Frontend) w `HabitFlow.sln`
