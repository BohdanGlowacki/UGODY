using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tesseract;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using PdfiumViewer;
using System.Drawing;
using DrawingImaging = System.Drawing.Imaging;
using UGODY.Application.Services;
using UGODY.Core.Entities;
using UGODY.Infrastructure.Data;

namespace UGODY.Infrastructure.Services;

public class OcrService : IOcrService
{
    private readonly ApplicationDbContext _context;
    private readonly IPdfStorageService _pdfStorageService;
    private readonly ILogger<OcrService> _logger;
    private readonly string _tesseractDataPath;

    public OcrService(
        ApplicationDbContext context,
        IPdfStorageService pdfStorageService,
        ILogger<OcrService> logger)
    {
        _context = context;
        _pdfStorageService = pdfStorageService;
        _logger = logger;
        
        // Get the base directory of the executing assembly (API project)
        var baseDirectory = AppContext.BaseDirectory;
        _tesseractDataPath = Path.Combine(baseDirectory, "tessdata");
        
        // Log the path being used
        _logger.LogInformation("Tesseract data path: {TesseractDataPath}", _tesseractDataPath);
        
        // Check if tessdata directory exists
        if (!Directory.Exists(_tesseractDataPath))
        {
            _logger.LogWarning("Tesseract data directory does not exist: {TesseractDataPath}. " +
                "Please download language data files from https://github.com/tesseract-ocr/tessdata " +
                "and place them in this directory.", _tesseractDataPath);
        }
    }

    public async Task<OcrResult> ProcessPdfAsync(int pdfFileId, CancellationToken cancellationToken = default)
    {
        var pdfFile = await _pdfStorageService.GetByIdAsync(pdfFileId, cancellationToken);
        if (pdfFile == null)
        {
            throw new ArgumentException($"PDF file with ID {pdfFileId} not found.", nameof(pdfFileId));
        }

        // Check if OCR result already exists
        var existingResult = await GetOcrResultAsync(pdfFileId, cancellationToken);
        if (existingResult != null && existingResult.ProcessingStatus == ProcessingStatus.Completed)
        {
            _logger.LogInformation("OCR result already exists for PDF file ID: {PdfFileId}", pdfFileId);
            return existingResult;
        }

        var ocrResult = new OcrResult
        {
            PdfFileId = pdfFileId,
            ProcessingStatus = ProcessingStatus.Processing,
            ProcessedDate = DateTime.UtcNow
        };

        try
        {
            // Convert PDF to images and perform OCR
            var extractedText = await ExtractTextFromPdfAsync(pdfFile.FileContent, cancellationToken);

            ocrResult.ExtractedText = extractedText.Text;
            ocrResult.Confidence = extractedText.Confidence;
            ocrResult.ProcessingStatus = ProcessingStatus.Completed;
            ocrResult.ProcessedDate = DateTime.UtcNow;

            _logger.LogInformation("OCR completed for PDF file ID: {PdfFileId}, Confidence: {Confidence}", 
                pdfFileId, ocrResult.Confidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OCR for PDF file ID: {PdfFileId}", pdfFileId);
            ocrResult.ProcessingStatus = ProcessingStatus.Failed;
            ocrResult.ErrorMessage = ex.Message;
        }

        // Save or update OCR result
        var existingDbResult = await _context.OcrResults
            .FirstOrDefaultAsync(r => r.PdfFileId == pdfFileId, cancellationToken);

        if (existingDbResult != null)
        {
            existingDbResult.ExtractedText = ocrResult.ExtractedText;
            existingDbResult.Confidence = ocrResult.Confidence;
            existingDbResult.ProcessingStatus = ocrResult.ProcessingStatus;
            existingDbResult.ErrorMessage = ocrResult.ErrorMessage;
            existingDbResult.ProcessedDate = ocrResult.ProcessedDate;
            await _context.SaveChangesAsync(cancellationToken);
            return existingDbResult;
        }
        else
        {
            _context.OcrResults.Add(ocrResult);
            await _context.SaveChangesAsync(cancellationToken);
            return ocrResult;
        }
    }

    public async Task<OcrResult?> GetOcrResultAsync(int pdfFileId, CancellationToken cancellationToken = default)
    {
        return await _context.OcrResults
            .FirstOrDefaultAsync(r => r.PdfFileId == pdfFileId, cancellationToken);
    }

    private async Task<(string Text, double Confidence)> ExtractTextFromPdfAsync(byte[] pdfContent, CancellationToken cancellationToken)
    {
        // Create a temporary file for the PDF
        var tempPdfPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".pdf");
        await File.WriteAllBytesAsync(tempPdfPath, pdfContent, cancellationToken);

        try
        {
            // First, try to extract text directly from PDF using PdfPig
            var extractedText = await TryExtractTextDirectlyFromPdfAsync(pdfContent, cancellationToken);
            
            // If we got meaningful text, return it
            if (!string.IsNullOrWhiteSpace(extractedText.Text) && extractedText.Text.Length > 50)
            {
                _logger.LogInformation("Successfully extracted text directly from PDF. Text length: {Length}", extractedText.Text.Length);
                return extractedText;
            }

            // If PDF doesn't contain text (is a scan), use OCR with Tesseract
            _logger.LogInformation("PDF appears to be a scan or contains little text. Using OCR with Tesseract.");
            return await ExtractTextUsingOcrAsync(tempPdfPath, cancellationToken);
        }
        finally
        {
            if (File.Exists(tempPdfPath))
            {
                try
                {
                    File.Delete(tempPdfPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary PDF file: {TempPdfPath}", tempPdfPath);
                }
            }
        }
    }

    private Task<(string Text, double Confidence)> TryExtractTextDirectlyFromPdfAsync(byte[] pdfContent, CancellationToken cancellationToken)
    {
        try
        {
            using var document = UglyToad.PdfPig.PdfDocument.Open(pdfContent);
            var allText = new System.Text.StringBuilder();
            
            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var pageText = page.Text;
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    allText.AppendLine(pageText);
                }
            }

            var extractedText = allText.ToString();
            
            // Return with high confidence if we got text
            if (!string.IsNullOrWhiteSpace(extractedText))
            {
                return Task.FromResult((extractedText, 95.0)); // High confidence for direct text extraction
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract text directly from PDF. Will try OCR instead.");
        }

        return Task.FromResult(("", 0.0));
    }

    private async Task<(string Text, double Confidence)> ExtractTextUsingOcrAsync(string pdfPath, CancellationToken cancellationToken)
    {
        // Check if tessdata directory exists before trying to use Tesseract
        if (!Directory.Exists(_tesseractDataPath))
        {
            var errorMessage = $"Tesseract data directory not found: {_tesseractDataPath}. " +
                "Please download language data files (pol.traineddata, eng.traineddata) from " +
                "https://github.com/tesseract-ocr/tessdata and place them in the tessdata directory.";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        // Check if required language files exist
        var polFile = Path.Combine(_tesseractDataPath, "pol.traineddata");
        var engFile = Path.Combine(_tesseractDataPath, "eng.traineddata");
        
        if (!File.Exists(polFile) || !File.Exists(engFile))
        {
            var errorMessage = $"Required Tesseract language files not found. " +
                $"Expected files: {polFile}, {engFile}. " +
                "Please download them from https://github.com/tesseract-ocr/tessdata";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        // Convert PDF pages to images and process with Tesseract
        var allText = new System.Text.StringBuilder();
        var totalConfidence = 0.0;
        var pageCount = 0;

        try
        {
            // Render PDF pages to images using PdfiumViewer
            using var pdfDocument = PdfiumViewer.PdfDocument.Load(pdfPath);
            var pageCountInPdf = pdfDocument.PageCount;

            _logger.LogInformation("Processing {PageCount} pages from PDF using OCR", pageCountInPdf);

            using var engine = new TesseractEngine(_tesseractDataPath, "pol+eng", EngineMode.Default);
            
            for (int pageIndex = 0; pageIndex < pageCountInPdf; pageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    // Render page to image
                    var image = pdfDocument.Render(pageIndex, 300, 300, true);
                    if (image == null)
                    {
                        _logger.LogWarning("Failed to render page {PageIndex} to image", pageIndex);
                        continue;
                    }

                    // Convert to format suitable for Tesseract
                    var imageBytes = ImageToByteArray(image);
                    
                    // Process with Tesseract
                    using var img = Pix.LoadFromMemory(imageBytes);
                    using var page = engine.Process(img);
                    
                    var pageText = page.GetText();
                    var confidence = page.GetMeanConfidence();

                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        allText.AppendLine($"--- Page {pageIndex + 1} ---");
                        allText.AppendLine(pageText);
                        totalConfidence += confidence;
                        pageCount++;
                    }

                    _logger.LogInformation("Processed page {PageIndex}/{PageCount}, Confidence: {Confidence}", 
                        pageIndex + 1, pageCountInPdf, confidence);

                    // Dispose image
                    image.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing page {PageIndex} with OCR", pageIndex);
                    // Continue with next page
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering PDF to images for OCR");
            throw;
        }

        var finalText = allText.ToString();
        var averageConfidence = pageCount > 0 ? totalConfidence / pageCount : 0.0;

        _logger.LogInformation("OCR completed. Extracted {TextLength} characters from {PageCount} pages. Average confidence: {Confidence}", 
            finalText.Length, pageCount, averageConfidence);

        return (finalText, averageConfidence);
    }

    private byte[] ImageToByteArray(Image image)
    {
        using var ms = new MemoryStream();
        image.Save(ms, DrawingImaging.ImageFormat.Png);
        return ms.ToArray();
    }
}
