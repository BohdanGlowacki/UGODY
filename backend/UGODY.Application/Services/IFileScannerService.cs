using UGODY.Core.Entities;

namespace UGODY.Application.Services;

public interface IFileScannerService
{
    Task<List<PdfFile>> ScanDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default);
}
