using UGODY.Core.Entities;

namespace UGODY.Application.Services;

public interface IOcrService
{
    Task<OcrResult> ProcessPdfAsync(int pdfFileId, CancellationToken cancellationToken = default);
    Task<OcrResult?> GetOcrResultAsync(int pdfFileId, CancellationToken cancellationToken = default);
}
