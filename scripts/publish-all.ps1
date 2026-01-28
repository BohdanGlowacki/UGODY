# Script to publish both backend and frontend for a specific environment
# Usage: .\scripts\publish-all.ps1 -Environment dev|test|prod

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("dev", "test", "prod")]
    [string]$Environment
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Publishing Full Application" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Publish backend
Write-Host "Publishing Backend..." -ForegroundColor Green
$backendScript = Join-Path $PSScriptRoot "publish.ps1"
& $backendScript -Environment $Environment

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Backend publish failed" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Publish frontend
Write-Host "Publishing Frontend..." -ForegroundColor Green
$frontendScript = Join-Path $PSScriptRoot "publish-frontend.ps1"
& $frontendScript -Environment $Environment

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Frontend publish failed" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Publish completed successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
