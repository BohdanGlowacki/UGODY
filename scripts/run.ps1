# Script to run the application in different environments
# Usage: .\scripts\run.ps1 -Environment dev|test|prod

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("dev", "test", "prod")]
    [string]$Environment
)

$ErrorActionPreference = "Stop"

Write-Host "Starting application in $Environment environment..." -ForegroundColor Green

# Set environment variable
$env:ASPNETCORE_ENVIRONMENT = switch ($Environment) {
    "dev" { "Development" }
    "test" { "Test" }
    "prod" { "Production" }
}

# Navigate to backend API directory
$apiPath = Join-Path $PSScriptRoot "..\backend\UGODY.API"
if (-not (Test-Path $apiPath)) {
    Write-Host "Error: API project not found at $apiPath" -ForegroundColor Red
    exit 1
}

Set-Location $apiPath

# Check if database migration is needed
Write-Host "Checking database migrations..." -ForegroundColor Yellow
try {
    dotnet ef database update --project ..\UGODY.Infrastructure --startup-project UGODY.API --context ApplicationDbContext --no-build
    Write-Host "Database migrations applied successfully." -ForegroundColor Green
} catch {
    Write-Host "Warning: Could not apply migrations. Make sure database is accessible." -ForegroundColor Yellow
}

# Run the application
Write-Host "Starting API server..." -ForegroundColor Green
Write-Host "Environment: $env:ASPNETCORE_ENVIRONMENT" -ForegroundColor Cyan
Write-Host "API will be available at: https://localhost:5001 and http://localhost:5000" -ForegroundColor Cyan

dotnet run
