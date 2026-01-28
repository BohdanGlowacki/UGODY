using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using UGODY.Application.Services;
using UGODY.Core.Entities;

namespace UGODY.Infrastructure.Services;

public class FileScannerService : IFileScannerService
{
    private readonly ILogger<FileScannerService> _logger;

    public FileScannerService(ILogger<FileScannerService> logger)
    {
        _logger = logger;
    }

    public async Task<List<PdfFile>> ScanDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        var pdfFiles = new List<PdfFile>();

        if (!Directory.Exists(directoryPath))
        {
            _logger.LogWarning("Directory does not exist: {DirectoryPath}", directoryPath);
            return pdfFiles;
        }

        try
        {
            var files = Directory.GetFiles(directoryPath, "*.pdf", SearchOption.TopDirectoryOnly);

            foreach (var filePath in files)
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    var fileContent = await File.ReadAllBytesAsync(filePath, cancellationToken);
                    var hash = ComputeHash(fileContent);

                    var pdfFile = new PdfFile
                    {
                        FileName = fileInfo.Name,
                        FilePath = filePath,
                        FileContent = fileContent,
                        FileSize = fileInfo.Length,
                        Hash = hash,
                        CreatedDate = fileInfo.CreationTime,
                        LastModifiedDate = fileInfo.LastWriteTime
                    };

                    pdfFiles.Add(pdfFile);
                    _logger.LogInformation("Scanned PDF file: {FileName}", pdfFile.FileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading file: {FilePath}", filePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning directory: {DirectoryPath}", directoryPath);
            throw;
        }

        return pdfFiles;
    }

    private static string ComputeHash(byte[] content)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(content);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}
