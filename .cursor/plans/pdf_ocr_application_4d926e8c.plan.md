---
name: PDF OCR Application
overview: Aplikacja do skanowania katalogu PDF, automatycznego OCR przy użyciu Tesseract i przeglądania wyników. Backend .NET 8.0 z SQL Server, frontend React.
todos:
  - id: setup-backend
    content: "Utworzenie rozwiązania .NET z projektami: API, Core, Infrastructure, Application, Tests"
    status: completed
  - id: setup-frontend
    content: Inicjalizacja projektu React z TypeScript i konfiguracja podstawowej struktury
    status: completed
  - id: database-entities
    content: "Utworzenie encji: PdfFile, OcrResult, Configuration w projekcie Core"
    status: completed
    dependencies:
      - setup-backend
  - id: database-migrations
    content: Utworzenie migracji bazy danych z tabelami i relacjami (defensywnie)
    status: in_progress
    dependencies:
      - database-entities
  - id: ocr-service
    content: Implementacja serwisu OCR z użyciem Tesseract.NET
    status: pending
    dependencies:
      - database-entities
  - id: file-scanner-service
    content: Implementacja serwisu skanowania katalogu z deduplikacją plików
    status: pending
    dependencies:
      - database-entities
  - id: api-controllers
    content: "Utworzenie kontrolerów API: Configuration, Files, Ocr"
    status: completed
    dependencies:
      - ocr-service
      - file-scanner-service
  - id: background-ocr
    content: Implementacja background service do przetwarzania OCR w tle
    status: completed
    dependencies:
      - ocr-service
  - id: frontend-api-services
    content: Utworzenie serwisów API w React (filesApi, configApi, ocrApi)
    status: completed
    dependencies:
      - setup-frontend
      - api-controllers
  - id: frontend-pages
    content: "Implementacja stron: Configuration, FilesList, FileDetails"
    status: completed
    dependencies:
      - frontend-api-services
  - id: frontend-components
    content: "Utworzenie komponentów: PdfViewer, OcrTextViewer, FileList"
    status: completed
    dependencies:
      - frontend-pages
  - id: unit-tests
    content: Napisanie testów jednostkowych dla serwisów i logiki biznesowej
    status: completed
    dependencies:
      - ocr-service
      - file-scanner-service
  - id: documentation
    content: Utworzenie dokumentacji z założeniami, aktorami, przepływem, encjami i diagramem ERD
    status: completed
  - id: iis-configuration
    content: Konfiguracja dla środowisk Test i Production pod IIS (web.config, appsettings, publish profiles)
    status: completed
    dependencies:
      - setup-backend
---

# Aplikacja PDF OCR - Plan Implementacji

## Architektura

Aplikacja składa się z:

- **Backend**: ASP.NET Core 8.0 Web API
- **Frontend**: React z TypeScript
- **Baza danych**: SQL Server (lokalna w dev, Azure SQL w prod)
- **OCR**: Tesseract.NET (wrapper dla Tesseract OCR)

## Struktura Projektu

```
UGODY/
├── backend/
│   ├── UGODY.API/              # Główny projekt API
│   ├── UGODY.Core/              # Modele domenowe, encje
│   ├── UGODY.Infrastructure/   # Implementacja (DB, OCR, FileSystem)
│   ├── UGODY.Application/      # Logika biznesowa, serwisy
│   └── UGODY.Tests/            # Testy jednostkowe
├── frontend/
│   └── react-app/               # Aplikacja React
└── docs/
    └── documentation.md         # Dokumentacja systemu
```

## Backend - Komponenty

### 1. Modele i Encje ([backend/UGODY.Core/Entities/](backend/UGODY.Core/Entities/))

- **PdfFile**: Id, FileName, FilePath, FileContent (BLOB), FileSize, CreatedDate, LastModifiedDate, Hash (SHA256 dla deduplikacji)
- **OcrResult**: Id, PdfFileId (FK), ExtractedText, Confidence, ProcessedDate, ProcessingStatus
- **Configuration**: Id, Key (np. "ScanDirectory"), Value, UpdatedDate

### 2. Migracje Bazy Danych ([backend/UGODY.Infrastructure/Data/Migrations/](backend/UGODY.Infrastructure/Data/Migrations/))

- Tabela `PdfFiles` z kolumną `FileContent` typu `varbinary(max)`
- Tabela `OcrResults` z relacją do `PdfFiles`
- Tabela `Configurations` dla ustawień
- Indeksy na `Hash` (dla szybkiej deduplikacji) i `FileName`
- Migracje defensywne z sprawdzaniem istnienia obiektów

### 3. Serwisy ([backend/UGODY.Application/Services/](backend/UGODY.Application/Services/))

- **IFileScannerService**: Skanowanie katalogu, wykrywanie nowych plików PDF
- **IPdfStorageService**: Zapis/odczyt plików PDF z bazy
- **IOcrService**: Wykonanie OCR przy użyciu Tesseract
- **IConfigurationService**: Zarządzanie konfiguracją (katalog skanowania)

### 4. Kontrolery API ([backend/UGODY.API/Controllers/](backend/UGODY.API/Controllers/))

- **ConfigurationController**: GET/PUT konfiguracji katalogu
- **FilesController**: 
  - GET /api/files - lista plików z paginacją
  - GET /api/files/{id} - szczegóły pliku
  - GET /api/files/{id}/pdf - pobranie PDF
  - GET /api/files/{id}/ocr - pobranie tekstu OCR
  - POST /api/files/scan - skanowanie katalogu (uruchamia proces)
- **OcrController**: 
  - POST /api/ocr/process/{fileId} - ręczne uruchomienie OCR

### 5. Background Service ([backend/UGODY.Application/BackgroundServices/](backend/UGODY.Application/BackgroundServices/))

- **OcrProcessingService**: Przetwarzanie OCR w tle po skanowaniu katalogu
- Kolejka zadań OCR (może użyć Hangfire lub prostego BackgroundService)

## Frontend - Komponenty

### 1. Strony ([frontend/react-app/src/pages/](frontend/react-app/src/pages/))

- **ConfigurationPage**: Formularz konfiguracji katalogu
- **FilesListPage**: Lista plików z filtrowaniem i paginacją
- **FileDetailsPage**: Szczegóły pliku z podglądem PDF i tekstu OCR

### 2. Komponenty ([frontend/react-app/src/components/](frontend/react-app/src/components/))

- **PdfViewer**: Komponent do wyświetlania PDF (react-pdf lub iframe)
- **OcrTextViewer**: Wyświetlanie tekstu OCR z możliwością kopiowania
- **FileList**: Tabela/list z plikami
- **ScanButton**: Przycisk do uruchomienia skanowania

### 3. Serwisy API ([frontend/react-app/src/services/](frontend/react-app/src/services/))

- **api.ts**: Konfiguracja Axios
- **filesApi.ts**: Wywołania API dla plików
- **configApi.ts**: Wywołania API dla konfiguracji
- **ocrApi.ts**: Wywołania API dla OCR

## Przepływ Danych

1. **Konfiguracja katalogu**: Użytkownik ustawia ścieżkę → zapis w bazie
2. **Skanowanie**: Użytkownik klika "Skanuj" → API skanuje katalog → wykrywa nowe pliki → zapisuje do bazy (jeśli nie istnieją) → dodaje zadania OCR do kolejki
3. **Przetwarzanie OCR**: Background service pobiera zadania → wykonuje OCR → zapisuje wyniki do bazy
4. **Przeglądanie**: Użytkownik wybiera plik → frontend pobiera PDF i tekst OCR z API

## Technologie

- **Backend**: 
  - .NET 8.0
  - Entity Framework Core
  - Tesseract.NET
  - Swagger/OpenAPI
- **Frontend**: 
  - React 18+
  - TypeScript
  - Axios
  - React Router
  - Material-UI lub podobny
- **Baza**: SQL Server (lokalna/Azure)

## Testy

- Testy jednostkowe dla serwisów (OCR, skanowanie, deduplikacja)
- Testy integracyjne dla API
- Testy komponentów React

## Konfiguracja Środowisk (Test i Production pod IIS)

### 1. Pliki Konfiguracyjne Backend ([backend/UGODY.API/](backend/UGODY.API/))

- **appsettings.json**: Konfiguracja bazowa (Development)
- **appsettings.Test.json**: Konfiguracja dla środowiska testowego
  - Connection string do SQL Server testowego
  - Logging poziom Info
  - CORS dla domeny testowej
  - Swagger włączony
- **appsettings.Production.json**: Konfiguracja dla środowiska produkcyjnego
  - Connection string do Azure SQL Database
  - Logging poziom Warning
  - CORS dla domeny produkcyjnej
  - Swagger wyłączony
  - Wszystkie wrażliwe dane przez zmienne środowiskowe/App Settings w Azure

### 2. web.config dla IIS ([backend/UGODY.API/](backend/UGODY.API/))

- Konfiguracja ASP.NET Core Module
- Ustawienia dla różnych środowisk (ASPNETCORE_ENVIRONMENT)
- Konfiguracja request timeout dla dużych plików PDF
- Konfiguracja max request length
- Ustawienia dla background services

### 3. Publish Profiles ([backend/UGODY.API/Properties/PublishProfiles/](backend/UGODY.API/Properties/PublishProfiles/))

- **Test.pubxml**: 
  - Publikacja do folderu lokalnego lub serwera testowego
  - Konfiguracja dla środowiska Test
  - Target: Folder lub IIS
- **Production.pubxml**: 
  - Publikacja do Azure Web App (lub serwera produkcyjnego)
  - Konfiguracja dla środowiska Production
  - Target: Azure lub IIS
  - Ustawienia pre-compilation

### 4. Connection Strings

- **Development**: `Server=localhost;Database=UgodyDev;Integrated Security=true;TrustServerCertificate=true`
- **Test**: `Server=test-server;Database=UgodyTest;User Id=...;Password=...;TrustServerCertificate=true`
- **Production**: Connection string z Azure App Settings (szyfrowany)

### 5. Zmienne Środowiskowe IIS

- **Test**: ASPNETCORE_ENVIRONMENT=Test
- **Production**: ASPNETCORE_ENVIRONMENT=Production
- Connection strings w App Settings IIS (dla Production)

### 6. Konfiguracja Frontend ([frontend/react-app/](frontend/react-app/))

- **.env.development**: API URL dla development
- **.env.test**: API URL dla środowiska testowego
- **.env.production**: API URL dla środowiska produkcyjnego
- Build scripts dla różnych środowisk w package.json

### 7. Azure Web App Configuration (Production)

- App Settings w Azure Portal:
  - ASPNETCORE_ENVIRONMENT=Production
  - ConnectionStrings__DefaultConnection (Azure SQL)
  - Wszystkie wrażliwe konfiguracje
- Application Settings dla IIS
- Konfiguracja Always On
- Konfiguracja Application Insights (opcjonalnie)

### 8. IIS Setup Requirements

- ASP.NET Core 8.0 Runtime zainstalowany
- ASP.NET Core Module v2
- Application Pool skonfigurowany (No Managed Code, .NET CLR Version: No Managed Code)
- Uprawnienia do katalogu aplikacji
- Uprawnienia do bazy danych SQL Server

## Dokumentacja

Dokumentacja w `docs/documentation.md` zawiera:

- Założenia systemu
- Aktorzy (użytkownik końcowy)
- Przepływ informacji (diagramy)
- Model danych (encje, relacje)
- Diagram ERD (Mermaid)