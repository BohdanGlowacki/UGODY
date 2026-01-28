# Deployment Scripts

This directory contains PowerShell scripts for running and publishing the application for different environments.

## Scripts

### run.ps1
Runs the application in the specified environment.

**Usage:**
```powershell
.\scripts\run.ps1 -Environment dev|test|prod
```

**Examples:**
```powershell
# Run in development environment
.\scripts\run.ps1 -Environment dev

# Run in test environment
.\scripts\run.ps1 -Environment test

# Run in production environment
.\scripts\run.ps1 -Environment prod
```

### publish.ps1
Publishes the backend API for the specified environment.

**Usage:**
```powershell
.\scripts\publish.ps1 -Environment dev|test|prod [-OutputPath <path>]
```

**Examples:**
```powershell
# Publish to default location (publish\{environment})
.\scripts\publish.ps1 -Environment test

# Publish to custom location
.\scripts\publish.ps1 -Environment prod -OutputPath "C:\Deploy\Production"
```

### publish-frontend.ps1
Builds and publishes the frontend React application for the specified environment.

**Usage:**
```powershell
.\scripts\publish-frontend.ps1 -Environment dev|test|prod [-OutputPath <path>]
```

**Examples:**
```powershell
# Publish frontend to default location
.\scripts\publish-frontend.ps1 -Environment prod

# Publish frontend to custom location
.\scripts\publish-frontend.ps1 -Environment test -OutputPath "C:\Deploy\Frontend"
```

### publish-all.ps1
Publishes both backend and frontend for the specified environment.

**Usage:**
```powershell
.\scripts\publish-all.ps1 -Environment dev|test|prod
```

**Example:**
```powershell
.\scripts\publish-all.ps1 -Environment prod
```

### deploy-iis.ps1
Deploys the published application to IIS. **Requires Administrator privileges.**

**Usage:**
```powershell
.\scripts\deploy-iis.ps1 -Environment dev|test|prod -SiteName <site-name> -AppPoolName <app-pool-name> [-PublishPath <path>]
```

**Example:**
```powershell
# Deploy to IIS (run as Administrator)
.\scripts\deploy-iis.ps1 -Environment prod -SiteName "UGODY-API" -AppPoolName "UGODY-AppPool"
```

## Environment Configuration

### Development
- Environment: `Development`
- Database: Local SQL Server
- API URL: `http://localhost:5000` / `https://localhost:5001`
- Frontend URL: `http://localhost:3000`

### Test
- Environment: `Test`
- Database: Test SQL Server
- API URL: Configured in `appsettings.Test.json`
- Frontend URL: Configured in `.env.test`

### Production
- Environment: `Production`
- Database: Azure SQL Database
- API URL: Azure Web App URL
- Frontend URL: Production domain

## Prerequisites

### Backend
- .NET 8.0 SDK
- SQL Server (local or remote)
- Entity Framework Core tools: `dotnet tool install --global dotnet-ef`

### Frontend
- Node.js 16+ and npm
- All dependencies installed (`npm install` in `frontend/react-app`)

### IIS Deployment
- IIS with ASP.NET Core Module installed
- WebAdministration PowerShell module
- Administrator privileges

## Typical Workflow

### Development
1. Run the application:
   ```powershell
   .\scripts\run.ps1 -Environment dev
   ```

### Test Deployment
1. Publish backend and frontend:
   ```powershell
   .\scripts\publish-all.ps1 -Environment test
   ```
2. Deploy to IIS (as Administrator):
   ```powershell
   .\scripts\deploy-iis.ps1 -Environment test -SiteName "UGODY-Test" -AppPoolName "UGODY-Test-Pool"
   ```

### Production Deployment
1. Publish backend and frontend:
   ```powershell
   .\scripts\publish-all.ps1 -Environment prod
   ```
2. Review and update connection strings in `appsettings.Production.json`
3. Deploy to IIS or Azure Web App:
   ```powershell
   .\scripts\deploy-iis.ps1 -Environment prod -SiteName "UGODY-Prod" -AppPoolName "UGODY-Prod-Pool"
   ```

## Notes

- All scripts use English comments as per project standards
- Scripts include error handling and validation
- Publish scripts create backups before deployment (IIS deployment)
- Environment variables are set automatically based on the environment parameter
- Database migrations are applied automatically when running the application
