using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tesseract;
using UGODY.Application.Services;
using UGODY.Core.Entities;
using UGODY.Infrastructure.Data;

namespace UGODY.Infrastructure.Services;

public class OcrService : IOcrService
{
    private readonly ApplicationDbContext _context;
    private readonly IPdfStorageService _pdfStorageService;
    private readonly ILogger<OcrService> _logger;
    private const string TesseractDataPath = @"./tessdata";

    public OcrService(
        ApplicationDbContext context,
        IPdfStorageService pdfStorageService,
        ILogger<OcrService> logger)
    {
        _context = context;
        _pdfStorageService = pdfStorageService;
        _logger = logger;
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
        // For now, we'll use a simple approach with Tesseract
        // Note: Tesseract works with images, so we need to convert PDF pages to images first
        // This is a simplified implementation - in production, you might want to use PdfPig or similar
        
        // Create a temporary file for the PDF
        var tempPdfPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".pdf");
        await File.WriteAllBytesAsync(tempPdfPath, pdfContent, cancellationToken);

        try
        {
            // For this implementation, we'll extract text from the first page only
            // In production, you should iterate through all pages
            // var allText = new List<string>();
            // double totalConfidence = 0;
            // int pageCount = 0;

            // Note: This is a simplified implementation
            // In production, you would need to:
            // 1. Convert PDF pages to images (using PdfPig, PdfSharp, or similar)
            // 2. Process each image with Tesseract
            // 3. Combine the results

            using var engine = new TesseractEngine(TesseractDataPath, "pol+eng", EngineMode.Default);
            // For now, return empty text - full implementation would require PDF to image conversion
            // This is a placeholder that needs to be completed with actual PDF processing

            return ("", 0.0);
        }
        finally
        {
            if (File.Exists(tempPdfPath))
            {
                File.Delete(tempPdfPath);
            }
        }
    }
}
