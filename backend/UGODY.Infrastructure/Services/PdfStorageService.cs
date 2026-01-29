using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UGODY.Application.Services;
using UGODY.Core.Entities;
using UGODY.Infrastructure.Data;

namespace UGODY.Infrastructure.Services;

public class PdfStorageService : IPdfStorageService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PdfStorageService> _logger;

    public PdfStorageService(ApplicationDbContext context, ILogger<PdfStorageService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PdfFile?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.PdfFiles
            .Include(p => p.OcrResults)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<PdfFile?> GetByHashAsync(string hash, CancellationToken cancellationToken = default)
    {
        return await _context.PdfFiles
            .Include(p => p.OcrResults)
            .FirstOrDefaultAsync(p => p.Hash == hash, cancellationToken);
    }

    public async Task<byte[]> GetFileContentAsync(int id, CancellationToken cancellationToken = default)
    {
        var pdfFile = await _context.PdfFiles
            .Where(p => p.Id == id)
            .Select(p => p.FileContent)
            .FirstOrDefaultAsync(cancellationToken);

        return pdfFile ?? Array.Empty<byte>();
    }

    public async Task<bool> ExistsByHashAsync(string hash, CancellationToken cancellationToken = default)
    {
        return await _context.PdfFiles
            .AnyAsync(p => p.Hash == hash, cancellationToken);
    }

    public async Task<PdfFile> SaveAsync(PdfFile pdfFile, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.PdfFiles.Add(pdfFile);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Saved PDF file: {FileName} with ID: {Id}", pdfFile.FileName, pdfFile.Id);
            return pdfFile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving PDF file: {FileName}", pdfFile.FileName);
            throw;
        }
    }

    public async Task<List<PdfFile>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting files list: skip={Skip}, take={Take}", skip, take);
        
        // Load files without FileContent to improve performance - it's large and not needed for list view
        var files = await _context.PdfFiles
            .AsNoTracking() // Don't track entities for better performance
            .OrderByDescending(p => p.CreatedDate)
            .Skip(skip)
            .Take(take)
            .Select(p => new PdfFile
            {
                Id = p.Id,
                FileName = p.FileName,
                FilePath = p.FilePath,
                FileSize = p.FileSize,
                Hash = p.Hash,
                CreatedDate = p.CreatedDate,
                LastModifiedDate = p.LastModifiedDate,
                FileContent = Array.Empty<byte>(), // Don't load content for list view - saves memory
                OcrResults = p.OcrResults // Include OCR results if needed
            })
            .ToListAsync(cancellationToken);
        
        _logger.LogInformation("Retrieved {Count} files", files.Count);
        return files;
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PdfFiles.CountAsync(cancellationToken);
    }
}
