using Microsoft.AspNetCore.Mvc;
using UGODY.Application.Services;

namespace UGODY.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IPdfStorageService _pdfStorageService;
    private readonly IFileScannerService _fileScannerService;
    private readonly IConfigurationService _configurationService;
    private readonly IOcrQueueService _ocrQueueService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(
        IPdfStorageService pdfStorageService,
        IFileScannerService fileScannerService,
        IConfigurationService configurationService,
        IOcrQueueService ocrQueueService,
        ILogger<FilesController> logger)
    {
        _pdfStorageService = pdfStorageService;
        _fileScannerService = fileScannerService;
        _configurationService = configurationService;
        _ocrQueueService = ocrQueueService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<FilesListResponse>> GetFiles(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var files = await _pdfStorageService.GetAllAsync(skip, take, cancellationToken);
            var totalCount = await _pdfStorageService.GetTotalCountAsync(cancellationToken);

            return Ok(new FilesListResponse
            {
                Files = files.Select(f => new FileDto
                {
                    Id = f.Id,
                    FileName = f.FileName,
                    FilePath = f.FilePath,
                    FileSize = f.FileSize,
                    CreatedDate = f.CreatedDate,
                    LastModifiedDate = f.LastModifiedDate
                }).ToList(),
                TotalCount = totalCount,
                Skip = skip,
                Take = take
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving files list");
            return StatusCode(500, "An error occurred while retrieving the files list.");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FileDto>> GetFile(int id, CancellationToken cancellationToken)
    {
        try
        {
            var file = await _pdfStorageService.GetByIdAsync(id, cancellationToken);
            if (file == null)
            {
                return NotFound($"File with ID {id} not found.");
            }

            return Ok(new FileDto
            {
                Id = file.Id,
                FileName = file.FileName,
                FilePath = file.FilePath,
                FileSize = file.FileSize,
                CreatedDate = file.CreatedDate,
                LastModifiedDate = file.LastModifiedDate
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file with ID: {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the file.");
        }
    }

    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> GetPdf(int id, CancellationToken cancellationToken)
    {
        try
        {
            var file = await _pdfStorageService.GetByIdAsync(id, cancellationToken);
            if (file == null)
            {
                return NotFound($"File with ID {id} not found.");
            }

            var content = await _pdfStorageService.GetFileContentAsync(id, cancellationToken);
            return File(content, "application/pdf", file.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving PDF file with ID: {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the PDF file.");
        }
    }

    [HttpGet("{id}/ocr")]
    public async Task<ActionResult<OcrResultDto>> GetOcr(int id, CancellationToken cancellationToken)
    {
        try
        {
            var file = await _pdfStorageService.GetByIdAsync(id, cancellationToken);
            if (file == null)
            {
                return NotFound($"File with ID {id} not found.");
            }

            var ocrResult = file.OcrResults.FirstOrDefault();
            if (ocrResult == null)
            {
                return NotFound($"OCR result for file ID {id} not found.");
            }

            return Ok(new OcrResultDto
            {
                Id = ocrResult.Id,
                PdfFileId = ocrResult.PdfFileId,
                ExtractedText = ocrResult.ExtractedText,
                Confidence = ocrResult.Confidence,
                ProcessedDate = ocrResult.ProcessedDate,
                ProcessingStatus = ocrResult.ProcessingStatus.ToString(),
                ErrorMessage = ocrResult.ErrorMessage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving OCR result for file ID: {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the OCR result.");
        }
    }

    [HttpPost("scan")]
    public async Task<ActionResult<ScanResponse>> ScanDirectory(CancellationToken cancellationToken)
    {
        try
        {
            var scanDirectory = await _configurationService.GetValueAsync("ScanDirectory", cancellationToken);
            if (string.IsNullOrEmpty(scanDirectory))
            {
                return BadRequest("Scan directory is not configured. Please set the 'ScanDirectory' configuration key.");
            }

            _logger.LogInformation("Starting directory scan: {ScanDirectory}", scanDirectory);
            var scannedFiles = await _fileScannerService.ScanDirectoryAsync(scanDirectory, cancellationToken);

            var newFilesCount = 0;
            foreach (var scannedFile in scannedFiles)
            {
                var exists = await _pdfStorageService.ExistsByHashAsync(scannedFile.Hash, cancellationToken);
                if (!exists)
                {
                    var savedFile = await _pdfStorageService.SaveAsync(scannedFile, cancellationToken);
                    _ocrQueueService.EnqueuePdfFile(savedFile.Id);
                    newFilesCount++;
                    _logger.LogInformation("Added new file: {FileName} (ID: {Id})", savedFile.FileName, savedFile.Id);
                }
            }

            return Ok(new ScanResponse
            {
                ScannedFilesCount = scannedFiles.Count,
                NewFilesCount = newFilesCount,
                Message = $"Scan completed. Found {scannedFiles.Count} files, {newFilesCount} new files added."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning directory");
            return StatusCode(500, "An error occurred while scanning the directory.");
        }
    }
}

public class FileDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }
}

public class OcrResultDto
{
    public int Id { get; set; }
    public int PdfFileId { get; set; }
    public string ExtractedText { get; set; } = string.Empty;
    public double? Confidence { get; set; }
    public DateTime ProcessedDate { get; set; }
    public string ProcessingStatus { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

public class FilesListResponse
{
    public List<FileDto> Files { get; set; } = new();
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
}

public class ScanResponse
{
    public int ScannedFilesCount { get; set; }
    public int NewFilesCount { get; set; }
    public string Message { get; set; } = string.Empty;
}
