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
- Do testow uzywaj XUnit.
- Testy jednostkowe:
  - Testy jednostkowe powinny znajdowac sie w podfolderze UnitTests.
  - Do mockow jesli potrzebne uzywaj biblioteke NSubstitute.
- Testy integracyjne:
  - Testy integracyjne powinny znajdowac sie w podfolderze IntegrationTests.
  - Testy integracyjne polegaja na testowaniu flow logiki calych endpointow bez mockow.
  - Nalezy korzystac z TestContainers aby zasetupowac baze danych, oraz generowanego klienta http, dzieki ktoremu bedzie mozna w testach odpytywac endpointy.
  - Baza danych powinna byc jedna dla wszystkich uruchamianych testow.
  - Testy uruchamiaj rownolegle.
- Nazwy plikow `*Tests.cs`; jedna klasa na jednostke testowana.
- Uruchamiaj `dotnet test` (dodaj do rozwiazania po utworzeniu).

## Specyfikacja produktu

- Glowny dokument PRD: `.ai/prd.md`
- Aktualizuj przy zmianach zakresu/prioritetow i linkuj powiazane issue/PR

## Instrukcje dla agenta

- Ogranicz zmiany do dotknietego projektu (`Api` lub `Blazor`)
- Preferuj Minimal APIs i rekordy w API; w Blazor uzywaj komponentow Razor z trybem renderowania server
- Dodajac nowe projekty, umieszczaj je w istniejacych folderach rozwiazania (Backend/Frontend) w `HabitFlow.sln`
