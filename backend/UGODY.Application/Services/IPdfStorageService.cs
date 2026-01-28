using UGODY.Core.Entities;

namespace UGODY.Application.Services;

public interface IPdfStorageService
{
    Task<PdfFile?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<byte[]> GetFileContentAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByHashAsync(string hash, CancellationToken cancellationToken = default);
    Task<PdfFile> SaveAsync(PdfFile pdfFile, CancellationToken cancellationToken = default);
    Task<List<PdfFile>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
}
