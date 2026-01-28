# Script to publish the application for different environments
# Usage: .\scripts\publish.ps1 -Environment dev|test|prod [-OutputPath <path>]

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("dev", "test", "prod")]
    [string]$Environment,
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath
)

$ErrorActionPreference = "Stop"

Write-Host "Publishing application for $Environment environment..." -ForegroundColor Green

# Set default output path if not provided
if ([string]::IsNullOrEmpty($OutputPath)) {
    $OutputPath = Join-Path $PSScriptRoot "..\publish\$Environment"
}

# Create output directory if it doesn't exist
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

# Navigate to backend API directory
$apiPath = Join-Path $PSScriptRoot "..\backend\UGODY.API"
if (-not (Test-Path $apiPath)) {
    Write-Host "Error: API project not found at $apiPath" -ForegroundColor Red
    exit 1
}

Set-Location $apiPath

# Set environment variable
$env:ASPNETCORE_ENVIRONMENT = switch ($Environment) {
    "dev" { "Development" }
    "test" { "Test" }
    "prod" { "Production" }
}

# Build the solution first
Write-Host "Building solution..." -ForegroundColor Yellow
$solutionPath = Join-Path $PSScriptRoot "..\backend\UGODY.sln"
dotnet build $solutionPath -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Build failed" -ForegroundColor Red
    exit 1
}

# Publish the API project
Write-Host "Publishing API project..." -ForegroundColor Yellow
$apiProjectPath = Join-Path $apiPath "UGODY.API.csproj"

$publishArgs = @(
    "publish",
    $apiProjectPath,
    "-c", "Release",
    "-o", $OutputPath,
    "--self-contained", "false",
    "/p:PublishReadyToRun=true"
)

# Add environment-specific settings
switch ($Environment) {
    "test" {
        $publishArgs += "/p:EnvironmentName=Test"
    }
    "prod" {
        $publishArgs += "/p:EnvironmentName=Production"
    }
}

dotnet $publishArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Publish failed" -ForegroundColor Red
    exit 1
}

# Copy web.config if it exists
$webConfigPath = Join-Path $apiPath "web.config"
if (Test-Path $webConfigPath) {
    Copy-Item $webConfigPath -Destination $OutputPath -Force
    Write-Host "Copied web.config" -ForegroundColor Green
}

# Copy environment-specific appsettings
$appsettingsFile = switch ($Environment) {
    "dev" { "appsettings.Development.json" }
    "test" { "appsettings.Test.json" }
    "prod" { "appsettings.Production.json" }
}

$appsettingsPath = Join-Path $apiPath $appsettingsFile
if (Test-Path $appsettingsPath) {
    Copy-Item $appsettingsPath -Destination (Join-Path $OutputPath "appsettings.$Environment.json") -Force
    Write-Host "Copied $appsettingsFile" -ForegroundColor Green
}

Write-Host "`nPublish completed successfully!" -ForegroundColor Green
Write-Host "Output directory: $OutputPath" -ForegroundColor Cyan
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Review and update connection strings in appsettings files" -ForegroundColor White
Write-Host "2. Ensure IIS is configured with the correct Application Pool" -ForegroundColor White
Write-Host "3. Set ASPNETCORE_ENVIRONMENT environment variable in IIS" -ForegroundColor White
Write-Host "4. Deploy to IIS or Azure Web App" -ForegroundColor White
