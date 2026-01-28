using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using UGODY.Application.Services;
using UGODY.Core.Entities;
using UGODY.Infrastructure.Data;
using UGODY.Infrastructure.Services;
using Xunit;

namespace UGODY.Tests.Services;

public class ConfigurationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IConfigurationService _service;
    private readonly string _testDbName;

    public ConfigurationServiceTests()
    {
        _testDbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(_testDbName)
            .Options;
        _context = new ApplicationDbContext(options);

        var loggerMock = new Mock<ILogger<ConfigurationService>>();
        _service = new ConfigurationService(_context, loggerMock.Object);
    }

    [Fact]
    public async Task GetValueAsync_WhenKeyExists_ReturnsValue()
    {
        // Arrange
        var config = new Configuration
        {
            Key = "TestKey",
            Value = "TestValue",
            UpdatedDate = DateTime.UtcNow
        };
        _context.Configurations.Add(config);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetValueAsync("TestKey");

        // Assert
        result.Should().Be("TestValue");
    }

    [Fact]
    public async Task GetValueAsync_WhenKeyDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _service.GetValueAsync("NonExistentKey");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetValueAsync_WhenKeyDoesNotExist_CreatesNewConfiguration()
    {
        // Act
        await _service.SetValueAsync("NewKey", "NewValue");

        // Assert
        var config = await _context.Configurations.FirstOrDefaultAsync(c => c.Key == "NewKey");
        config.Should().NotBeNull();
        config!.Value.Should().Be("NewValue");
    }

    [Fact]
    public async Task SetValueAsync_WhenKeyExists_UpdatesExistingConfiguration()
    {
        // Arrange
        var config = new Configuration
        {
            Key = "ExistingKey",
            Value = "OldValue",
            UpdatedDate = DateTime.UtcNow
        };
        _context.Configurations.Add(config);
        await _context.SaveChangesAsync();

        // Act
        await _service.SetValueAsync("ExistingKey", "NewValue");

        // Assert
        var updatedConfig = await _context.Configurations.FirstOrDefaultAsync(c => c.Key == "ExistingKey");
        updatedConfig.Should().NotBeNull();
        updatedConfig!.Value.Should().Be("NewValue");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
