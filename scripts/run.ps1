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


# Get absolute paths
$backendPath = Join-Path $PSScriptRoot "..\backend" | Resolve-Path
$apiPath = Join-Path $backendPath "UGODY.API"
$infrastructurePath = Join-Path $backendPath "UGODY.Infrastructure"
$frontendPath = Join-Path $PSScriptRoot "..\frontend\react-app"


if (-not (Test-Path $apiPath)) {
    Write-Host "Error: API project not found at $apiPath" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $infrastructurePath)) {
    Write-Host "Error: Infrastructure project not found at $infrastructurePath" -ForegroundColor Red
    exit 1
}

# Navigate to backend directory (root of all projects)
Set-Location $backendPath

# Check if dotnet-ef tool is installed
Write-Host "Checking Entity Framework tools..." -ForegroundColor Yellow
dotnet ef --version | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Warning: Entity Framework tools not found. Installing..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error: Failed to install Entity Framework tools" -ForegroundColor Red
        Write-Host "Please install manually: dotnet tool install --global dotnet-ef" -ForegroundColor Yellow
        exit 1
    }
}

# Restore and build projects first to ensure metadata is available
Write-Host "Restoring packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Package restore failed" -ForegroundColor Red
    exit 1
}

Write-Host "Building projects..." -ForegroundColor Yellow
dotnet build
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Build failed" -ForegroundColor Red
    exit 1
}

# Check if database migration is needed
Write-Host "Checking database migrations..." -ForegroundColor Yellow
try {
    # Apply migrations using absolute paths
    $infrastructureProject = Join-Path $infrastructurePath "UGODY.Infrastructure.csproj"
    $apiProject = Join-Path $apiPath "UGODY.API.csproj"
    
    # Verify project files exist
    if (-not (Test-Path $infrastructureProject)) {
        Write-Host "Error: Infrastructure project file not found at $infrastructureProject" -ForegroundColor Red
        exit 1
    }
    
    if (-not (Test-Path $apiProject)) {
        Write-Host "Error: API project file not found at $apiProject" -ForegroundColor Red
        exit 1
    }
    
    dotnet ef database update `
        --project $infrastructureProject `
        --startup-project $apiProject `
        --context ApplicationDbContext
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Database migrations applied successfully." -ForegroundColor Green
    } else {
        Write-Host "Warning: Migration command returned non-zero exit code." -ForegroundColor Yellow
        Write-Host "Make sure database is accessible and connection string is correct in appsettings.json" -ForegroundColor Yellow
    }
} catch {
    Write-Host "Warning: Could not apply migrations. Error: $_" -ForegroundColor Yellow
    Write-Host "Make sure database is accessible and connection string is correct in appsettings.json" -ForegroundColor Yellow
}

# Determine ports based on environment
$apiPort = switch ($Environment) {
    "dev" { "8881" }
    "test" { "8881" }
    "prod" { "8881" }
    default { "8881" }
}

$frontendPort = switch ($Environment) {
    "dev" { "8882" }
    "test" { "8882" }
    "prod" { "8882" }
    default { "8882" }
}

# Check if API port is already in use and stop the process
Write-Host "Checking if port $apiPort is available..." -ForegroundColor Yellow
try {
    $connection = Get-NetTCPConnection -LocalPort $apiPort -ErrorAction SilentlyContinue
    if ($connection) {
        $processId = $connection.OwningProcess | Select-Object -First 1
        $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
        if ($process) {
            Write-Host "Port $apiPort is already in use by process $($process.ProcessName) (PID: $processId). Stopping it..." -ForegroundColor Yellow
            Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 2
            Write-Host "Process stopped." -ForegroundColor Green
        }
    }
} catch {
    Write-Host "Warning: Could not check port status: $_" -ForegroundColor Yellow
}

# Start frontend for dev environment only
if ($Environment -eq "dev" -and (Test-Path $frontendPath)) {
    Write-Host "Starting frontend..." -ForegroundColor Green
    
    # Check if frontend port is already in use
    try {
        $frontendConnection = Get-NetTCPConnection -LocalPort $frontendPort -ErrorAction SilentlyContinue
        if ($frontendConnection) {
            $frontendProcessId = $frontendConnection.OwningProcess | Select-Object -First 1
            $frontendProcess = Get-Process -Id $frontendProcessId -ErrorAction SilentlyContinue
            if ($frontendProcess) {
                Write-Host "Port $frontendPort is already in use by process $($frontendProcess.ProcessName) (PID: $frontendProcessId). Stopping it..." -ForegroundColor Yellow
                Stop-Process -Id $frontendProcessId -Force -ErrorAction SilentlyContinue
                Start-Sleep -Seconds 2
                Write-Host "Frontend process stopped." -ForegroundColor Green
            }
        }
    } catch {
        Write-Host "Warning: Could not check frontend port status: $_" -ForegroundColor Yellow
    }
    
    # Check if node_modules exists, if not install dependencies
    $nodeModulesPath = Join-Path $frontendPath "node_modules"
    if (-not (Test-Path $nodeModulesPath)) {
        Write-Host "Installing frontend dependencies..." -ForegroundColor Yellow
        Set-Location $frontendPath
        npm install
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Warning: npm install failed. Frontend may not start correctly." -ForegroundColor Yellow
        }
        Set-Location $backendPath
    }
    
    # Start frontend in background using Start-Process
    Write-Host "Starting frontend on port $frontendPort..." -ForegroundColor Cyan
    
    # Create a temporary script to run npm start
    $tempScript = Join-Path $env:TEMP "start-frontend-$frontendPort.ps1"
    $scriptContent = @"
Set-Location '$frontendPath'
`$env:PORT = '$frontendPort'
npm start
"@
    $scriptContent | Out-File -FilePath $tempScript -Encoding UTF8
    
    # Start frontend process in background
    $frontendProcess = Start-Process powershell -ArgumentList "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "`"$tempScript`"" -WindowStyle Hidden -PassThru
    
    Write-Host "Frontend started in background (Process ID: $($frontendProcess.Id))" -ForegroundColor Green
    Write-Host "Frontend will be available at: http://localhost:$frontendPort" -ForegroundColor Cyan
    Write-Host "Waiting for frontend to start (this may take 10-15 seconds)..." -ForegroundColor Yellow
    
    # Wait for frontend to start and check if port is listening
    $maxWait = 30
    $waited = 0
    $started = $false
    while ($waited -lt $maxWait -and -not $started) {
        Start-Sleep -Seconds 2
        $waited += 2
        $connection = Get-NetTCPConnection -LocalPort $frontendPort -ErrorAction SilentlyContinue
        if ($connection) {
            $started = $true
            Write-Host "Frontend is ready!" -ForegroundColor Green
        } else {
            Write-Host "." -NoNewline -ForegroundColor Yellow
        }
    }
    if (-not $started) {
        Write-Host "`nWarning: Frontend may not have started correctly. Check the process manually." -ForegroundColor Yellow
    }
    Write-Host ""
    
    # Return to backend directory
    Set-Location $backendPath
}

# Navigate to API directory for running
Set-Location $apiPath

# Run the application
Write-Host "Starting API server..." -ForegroundColor Green
Write-Host "Environment: $env:ASPNETCORE_ENVIRONMENT" -ForegroundColor Cyan
Write-Host "API will be available at: http://localhost:$apiPort" -ForegroundColor Cyan

if ($Environment -eq "dev" -and (Test-Path $frontendPath)) {
    Write-Host "`nBoth API and Frontend are running:" -ForegroundColor Green
    Write-Host "  API:      http://localhost:$apiPort" -ForegroundColor Cyan
    Write-Host "  Frontend: http://localhost:$frontendPort" -ForegroundColor Cyan
    Write-Host "  Swagger:  http://localhost:$apiPort/swagger" -ForegroundColor Cyan
    Write-Host "`nPress Ctrl+C to stop API." -ForegroundColor Yellow
    Write-Host "Note: Frontend is running in a separate process. To stop it, find and kill the process using port $frontendPort." -ForegroundColor Yellow
}

dotnet run --urls "http://localhost:$apiPort"
