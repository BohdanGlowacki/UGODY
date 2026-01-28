# Script to build and publish the frontend React application for different environments
# Usage: .\scripts\publish-frontend.ps1 -Environment dev|test|prod [-OutputPath <path>]

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("dev", "test", "prod")]
    [string]$Environment,
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath
)

$ErrorActionPreference = "Stop"

Write-Host "Building frontend for $Environment environment..." -ForegroundColor Green

# Set default output path if not provided
if ([string]::IsNullOrEmpty($OutputPath)) {
    $OutputPath = Join-Path $PSScriptRoot "..\publish\frontend\$Environment"
}

# Navigate to frontend directory
$frontendPath = Join-Path $PSScriptRoot "..\frontend\react-app"
if (-not (Test-Path $frontendPath)) {
    Write-Host "Error: Frontend project not found at $frontendPath" -ForegroundColor Red
    exit 1
}

Set-Location $frontendPath

# Check if node_modules exists, if not install dependencies
if (-not (Test-Path "node_modules")) {
    Write-Host "Installing dependencies..." -ForegroundColor Yellow
    npm install
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error: npm install failed" -ForegroundColor Red
        exit 1
    }
}

# Copy appropriate .env file
$envFile = switch ($Environment) {
    "dev" { ".env.development" }
    "test" { ".env.test" }
    "prod" { ".env.production" }
}

if (Test-Path $envFile) {
    Copy-Item $envFile -Destination ".env" -Force
    Write-Host "Using $envFile for build" -ForegroundColor Green
} else {
    Write-Host "Warning: $envFile not found. Using default environment variables." -ForegroundColor Yellow
}

# Build the React application
Write-Host "Building React application..." -ForegroundColor Yellow
npm run build

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Build failed" -ForegroundColor Red
    exit 1
}

# Create output directory if it doesn't exist
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

# Copy build output
$buildPath = Join-Path $frontendPath "build"
if (Test-Path $buildPath) {
    Write-Host "Copying build output to $OutputPath..." -ForegroundColor Yellow
    Copy-Item -Path "$buildPath\*" -Destination $OutputPath -Recurse -Force
    Write-Host "Frontend build completed successfully!" -ForegroundColor Green
    Write-Host "Output directory: $OutputPath" -ForegroundColor Cyan
} else {
    Write-Host "Error: Build output not found at $buildPath" -ForegroundColor Red
    exit 1
}

Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Review API URL in .env file" -ForegroundColor White
Write-Host "2. Deploy to web server (IIS, nginx, Apache, etc.)" -ForegroundColor White
Write-Host "3. Configure reverse proxy if needed" -ForegroundColor White
