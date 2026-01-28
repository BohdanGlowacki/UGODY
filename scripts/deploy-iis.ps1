# Script to deploy published application to IIS
# Usage: .\scripts\deploy-iis.ps1 -Environment dev|test|prod -SiteName <site-name> -AppPoolName <app-pool-name> [-PublishPath <path>]

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("dev", "test", "prod")]
    [string]$Environment,
    
    [Parameter(Mandatory=$true)]
    [string]$SiteName,
    
    [Parameter(Mandatory=$true)]
    [string]$AppPoolName,
    
    [Parameter(Mandatory=$false)]
    [string]$PublishPath
)

$ErrorActionPreference = "Stop"

# Check if running as administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "Error: This script must be run as Administrator" -ForegroundColor Red
    Write-Host "Please run PowerShell as Administrator and try again." -ForegroundColor Yellow
    exit 1
}

# Set default publish path if not provided
if ([string]::IsNullOrEmpty($PublishPath)) {
    $PublishPath = Join-Path $PSScriptRoot "..\publish\$Environment"
}

if (-not (Test-Path $PublishPath)) {
    Write-Host "Error: Publish path not found: $PublishPath" -ForegroundColor Red
    Write-Host "Please run publish.ps1 first to create the publish output." -ForegroundColor Yellow
    exit 1
}

Write-Host "Deploying to IIS..." -ForegroundColor Green
Write-Host "Environment: $Environment" -ForegroundColor Cyan
Write-Host "Site Name: $SiteName" -ForegroundColor Cyan
Write-Host "App Pool: $AppPoolName" -ForegroundColor Cyan
Write-Host "Source Path: $PublishPath" -ForegroundColor Cyan
Write-Host ""

# Import WebAdministration module
Import-Module WebAdministration -ErrorAction Stop

# Get site physical path
$sitePath = (Get-Website -Name $SiteName).physicalPath
if ([string]::IsNullOrEmpty($sitePath)) {
    Write-Host "Error: Site '$SiteName' not found in IIS" -ForegroundColor Red
    exit 1
}

# Convert to full path if relative
if (-not [System.IO.Path]::IsPathRooted($sitePath)) {
    $sitePath = Join-Path $env:SystemDrive $sitePath.TrimStart('\')
}

Write-Host "Target Path: $sitePath" -ForegroundColor Cyan

# Stop the application pool and site
Write-Host "Stopping application pool and site..." -ForegroundColor Yellow
Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
Stop-Website -Name $SiteName -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Backup existing files if directory exists
if (Test-Path $sitePath) {
    $backupPath = "$sitePath.backup.$(Get-Date -Format 'yyyyMMddHHmmss')"
    Write-Host "Creating backup at: $backupPath" -ForegroundColor Yellow
    Copy-Item -Path $sitePath -Destination $backupPath -Recurse -Force -ErrorAction SilentlyContinue
}

# Create directory if it doesn't exist
if (-not (Test-Path $sitePath)) {
    New-Item -ItemType Directory -Path $sitePath -Force | Out-Null
}

# Copy files
Write-Host "Copying files..." -ForegroundColor Yellow
Copy-Item -Path "$PublishPath\*" -Destination $sitePath -Recurse -Force

# Set environment variable in app pool
Write-Host "Setting environment variable..." -ForegroundColor Yellow
$envValue = switch ($Environment) {
    "dev" { "Development" }
    "test" { "Test" }
    "prod" { "Production" }
}

Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "environmentVariables" -Value @{ASPNETCORE_ENVIRONMENT = $envValue} -ErrorAction SilentlyContinue

# Start application pool and site
Write-Host "Starting application pool and site..." -ForegroundColor Yellow
Start-WebAppPool -Name $AppPoolName
Start-Sleep -Seconds 2
Start-Website -Name $SiteName

Write-Host ""
Write-Host "Deployment completed successfully!" -ForegroundColor Green
Write-Host "Site URL: http://localhost" -ForegroundColor Cyan
