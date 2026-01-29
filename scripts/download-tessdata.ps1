# Script to download Tesseract language data files
# Usage: .\scripts\download-tessdata.ps1

$ErrorActionPreference = "Stop"

Write-Host "Downloading Tesseract language data files..." -ForegroundColor Green

# Get the tessdata directory path
$tessdataPath = Join-Path $PSScriptRoot "..\backend\UGODY.API\tessdata" | Resolve-Path -ErrorAction SilentlyContinue

if (-not $tessdataPath) {
    $tessdataPath = Join-Path $PSScriptRoot "..\backend\UGODY.API\tessdata"
    New-Item -ItemType Directory -Path $tessdataPath -Force | Out-Null
    Write-Host "Created tessdata directory: $tessdataPath" -ForegroundColor Yellow
}

Set-Location $tessdataPath

# Base URL for Tesseract language data files
$baseUrl = "https://github.com/tesseract-ocr/tessdata/raw/main"

# Languages to download
$languages = @("pol", "eng")

foreach ($lang in $languages) {
    $fileName = "$lang.traineddata"
    $filePath = Join-Path $tessdataPath $fileName
    $url = "$baseUrl/$fileName"
    
    if (Test-Path $filePath) {
        Write-Host "File already exists: $fileName" -ForegroundColor Yellow
        $overwrite = Read-Host "Do you want to overwrite it? (y/N)"
        if ($overwrite -ne "y" -and $overwrite -ne "Y") {
            Write-Host "Skipping $fileName" -ForegroundColor Yellow
            continue
        }
    }
    
    Write-Host "Downloading $fileName..." -ForegroundColor Cyan
    try {
        Invoke-WebRequest -Uri $url -OutFile $filePath -UseBasicParsing
        Write-Host "Successfully downloaded $fileName" -ForegroundColor Green
    }
    catch {
        Write-Host "Error downloading $fileName : $_" -ForegroundColor Red
        Write-Host "You can manually download it from: $url" -ForegroundColor Yellow
    }
}

Write-Host "`nDownload complete!" -ForegroundColor Green
Write-Host "Language data files are located in: $tessdataPath" -ForegroundColor Cyan
