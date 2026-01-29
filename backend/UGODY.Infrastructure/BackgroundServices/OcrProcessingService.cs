using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UGODY.Application.Services;

namespace UGODY.Infrastructure.BackgroundServices;

public class OcrProcessingService : BackgroundService, IOcrQueueService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OcrProcessingService> _logger;
    private readonly ConcurrentQueue<int> _processingQueue = new();

    public OcrProcessingService(
        IServiceProvider serviceProvider,
        ILogger<OcrProcessingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void EnqueuePdfFile(int pdfFileId)
    {
        _processingQueue.Enqueue(pdfFileId);
        _logger.LogInformation("Enqueued PDF file ID: {PdfFileId} for OCR processing. Queue size: {QueueSize}", 
            pdfFileId, _processingQueue.Count);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OCR Processing Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_processingQueue.TryDequeue(out var pdfFileId))
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var ocrService = scope.ServiceProvider.GetRequiredService<IOcrService>();

                    _logger.LogInformation("Starting OCR processing for PDF file ID: {PdfFileId}. Remaining in queue: {QueueSize}", 
                        pdfFileId, _processingQueue.Count);
                    await ocrService.ProcessPdfAsync(pdfFileId, stoppingToken);
                    _logger.LogInformation("Successfully completed OCR processing for PDF file ID: {PdfFileId}", pdfFileId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing OCR for PDF file ID: {PdfFileId}. Error: {ErrorMessage}", 
                        pdfFileId, ex.Message);
                }
            }
            else
            {
                // Only log when queue is empty if it's been a while (to avoid spam)
                await Task.Delay(1000, stoppingToken); // Wait 1 second before checking again
            }
        }

        _logger.LogInformation("OCR Processing Service stopped");
    }
}
