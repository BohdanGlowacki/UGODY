using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using UGODY.Application.Services;
using UGODY.Core.Entities;
using UGODY.Infrastructure.Services;
using Xunit;

namespace UGODY.Tests.Services;

public class FileScannerServiceTests
{
    private readonly Mock<ILogger<FileScannerService>> _loggerMock;
    private readonly IFileScannerService _service;

    public FileScannerServiceTests()
    {
        _loggerMock = new Mock<ILogger<FileScannerService>>();
        _service = new FileScannerService(_loggerMock.Object);
    }

    [Fact]
    public async Task ScanDirectoryAsync_WhenDirectoryDoesNotExist_ReturnsEmptyList()
    {
        // Arrange
        var nonExistentPath = @"C:\NonExistent\Directory\Path";

        // Act
        var result = await _service.ScanDirectoryAsync(nonExistentPath);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ScanDirectoryAsync_WhenDirectoryExists_ReturnsListOfPdfFiles()
    {
        // Arrange
        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDirectory);

        try
        {
            // Create a test PDF file (empty file with .pdf extension)
            var testPdfPath = Path.Combine(tempDirectory, "test.pdf");
            File.WriteAllBytes(testPdfPath, new byte[] { 0x25, 0x50, 0x44, 0x46 }); // PDF header

            // Act
            var result = await _service.ScanDirectoryAsync(tempDirectory);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].FileName.Should().Be("test.pdf");
            result[0].FilePath.Should().Be(testPdfPath);
            result[0].Hash.Should().NotBeNullOrEmpty();
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }
    }
}
