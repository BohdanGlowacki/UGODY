using Microsoft.AspNetCore.Mvc;
using UGODY.Application.Services;

namespace UGODY.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OcrController : ControllerBase
{
    private readonly IOcrService _ocrService;
    private readonly IPdfStorageService _pdfStorageService;
    private readonly IOcrQueueService _ocrQueueService;
    private readonly ILogger<OcrController> _logger;

    public OcrController(
        IOcrService ocrService,
        IPdfStorageService pdfStorageService,
        IOcrQueueService ocrQueueService,
        ILogger<OcrController> logger)
    {
        _ocrService = ocrService;
        _pdfStorageService = pdfStorageService;
        _ocrQueueService = ocrQueueService;
        _logger = logger;
    }

    [HttpPost("process/{fileId}")]
    public async Task<ActionResult> ProcessOcr(int fileId, CancellationToken cancellationToken)
    {
        try
        {
            var file = await _pdfStorageService.GetByIdAsync(fileId, cancellationToken);
            if (file == null)
            {
                return NotFound($"File with ID {fileId} not found.");
            }

            _ocrQueueService.EnqueuePdfFile(fileId);
            _logger.LogInformation("Enqueued OCR processing for file ID: {FileId}", fileId);

            return Accepted(new { Message = $"OCR processing queued for file ID {fileId}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queuing OCR processing for file ID: {FileId}", fileId);
            return StatusCode(500, "An error occurred while queuing OCR processing.");
        }
    }
}
