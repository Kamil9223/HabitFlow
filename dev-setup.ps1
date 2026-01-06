param(
    [string]$ConnectionString = "Server=localhost,1433;Database=HabitFlowDb;User Id=sa;Password=HabitFlow2024!;TrustServerCertificate=True;MultipleActiveResultSets=true",
    [string]$EmailUsername = "",
    [string]$EmailPassword = ""
)

$projectDir = Join-Path $PSScriptRoot "HabitFlow.Api"

if (-not (Test-Path $projectDir)) {
    Write-Error "HabitFlow.Api not found under $PSScriptRoot."
    exit 1
}

Push-Location $projectDir
try {
    dotnet user-secrets init | Out-Null
    dotnet user-secrets set "ConnectionStrings:DefaultConnection" $ConnectionString | Out-Null

    if ($EmailUsername -ne "") {
        dotnet user-secrets set "Email:Smtp:Username" $EmailUsername | Out-Null
    }

    if ($EmailPassword -ne "") {
        dotnet user-secrets set "Email:Smtp:Password" $EmailPassword | Out-Null
    }

    Write-Host "User-secrets configured for HabitFlow.Api."
}
finally {
    Pop-Location
}
