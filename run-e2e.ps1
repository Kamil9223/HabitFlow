param(
    [switch]$Headful,
    [string]$ApiBaseUrl,
    [string]$BlazorBaseUrl,
    [int]$StartupTimeoutSeconds = 60
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

dotnet build .\HabitFlow.Tests\HabitFlow.Tests.csproj

$playwrightRoot = Join-Path $env:USERPROFILE "AppData\\Local\\ms-playwright"
$browserDirs = @()
if (Test-Path $playwrightRoot)
{
    $browserDirs = Get-ChildItem -Path $playwrightRoot -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match "chromium|firefox|webkit" }
}

$needsInstall = $browserDirs.Count -eq 0
if ($needsInstall)
{
    $playwrightScript = Join-Path $root "HabitFlow.Tests\\bin\\Debug\\net9.0\\playwright.ps1"
    if (!(Test-Path $playwrightScript))
    {
        throw "Playwright script not found. Run 'dotnet build' first."
    }

    & pwsh $playwrightScript install
}

if ($Headful)
{
    $env:E2E_HEADFUL = "1"
}
else
{
    Remove-Item Env:E2E_HEADFUL -ErrorAction SilentlyContinue
}

if ($ApiBaseUrl)
{
    $env:E2E_API_BASE_URL = $ApiBaseUrl
}
else
{
    Remove-Item Env:E2E_API_BASE_URL -ErrorAction SilentlyContinue
}

if ($BlazorBaseUrl)
{
    $env:E2E_BLAZOR_BASE_URL = $BlazorBaseUrl
}
else
{
    Remove-Item Env:E2E_BLAZOR_BASE_URL -ErrorAction SilentlyContinue
}

$env:E2E_STARTUP_TIMEOUT_SECONDS = $StartupTimeoutSeconds.ToString()

dotnet test --filter "FullyQualifiedName~HabitFlow.Tests.E2E"
