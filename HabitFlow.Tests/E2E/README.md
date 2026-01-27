# HabitFlow E2E (Playwright)

## Wymagania
- Docker Desktop uruchomiony (Testcontainers: SQL Server + MailHog)
- .NET 9 SDK

## Instalacja przegladarek Playwright
Po pierwszym buildzie testow uruchom:

```powershell
pwsh .\HabitFlow.Tests\bin\Debug\net9.0\playwright.ps1 install
```

Alternatywnie (jesli skrypt nie istnieje):

```powershell
dotnet build .\HabitFlow.Tests\HabitFlow.Tests.csproj
pwsh .\HabitFlow.Tests\bin\Debug\net9.0\playwright.ps1 install
```

## Uruchamianie testow E2E

```powershell
dotnet test --filter "FullyQualifiedName~HabitFlow.Tests.E2E"
```

## Szybkie uruchomienie (skrypt)

```powershell
.\run-e2e.ps1
```

Skrypt automatycznie instaluje przegladarki Playwright, jesli nie sa jeszcze zainstalowane.

Opcjonalne parametry:
- `-Headful` (uruchamia przegladarke z GUI)
- `-ApiBaseUrl http://localhost:5101`
- `-BlazorBaseUrl http://localhost:5102`
- `-StartupTimeoutSeconds 90`

## Zmienne srodowiskowe (opcjonalne)
- `E2E_API_BASE_URL` (domyslnie `http://localhost:5101`)
- `E2E_BLAZOR_BASE_URL` (domyslnie `http://localhost:5102`)
- `E2E_HEADFUL=1` (uruchamia przegladarke z GUI)
- `E2E_STARTUP_TIMEOUT_SECONDS` (domyslnie `60`)
