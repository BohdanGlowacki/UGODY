# Tesseract Language Data Files

This directory should contain Tesseract language data files (`.traineddata` files).

## Required Files

For the OCR service to work, you need to download the following language data files:

- `pol.traineddata` - Polish language data
- `eng.traineddata` - English language data

## How to Download

1. Go to: https://github.com/tesseract-ocr/tessdata
2. Download the following files:
   - `pol.traineddata` - for Polish language support
   - `eng.traineddata` - for English language support
3. Place the downloaded `.traineddata` files in this directory (`backend/UGODY.API/tessdata/`)

## Quick Download Links

- Polish: https://github.com/tesseract-ocr/tessdata/raw/main/pol.traineddata
- English: https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata

## PowerShell Download Command

You can use PowerShell to download the files:

```powershell
# Navigate to tessdata directory
cd backend\UGODY.API\tessdata

# Download Polish language data
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/pol.traineddata" -OutFile "pol.traineddata"

# Download English language data
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata" -OutFile "eng.traineddata"
```

## Note

These files are required for Tesseract OCR to work. Without them, OCR processing will fail with an error.
