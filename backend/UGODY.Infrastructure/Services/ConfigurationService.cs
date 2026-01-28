using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UGODY.Application.Services;
using UGODY.Core.Entities;
using UGODY.Infrastructure.Data;

namespace UGODY.Infrastructure.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(ApplicationDbContext context, ILogger<ConfigurationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        var config = await _context.Configurations
            .FirstOrDefaultAsync(c => c.Key == key, cancellationToken);

        return config?.Value;
    }

    public async Task SetValueAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var existingConfig = await _context.Configurations
            .FirstOrDefaultAsync(c => c.Key == key, cancellationToken);

        if (existingConfig != null)
        {
            existingConfig.Value = value;
            existingConfig.UpdatedDate = DateTime.UtcNow;
            _logger.LogInformation("Updated configuration key: {Key}", key);
        }
        else
        {
            var newConfig = new Configuration
            {
                Key = key,
                Value = value,
                UpdatedDate = DateTime.UtcNow
            };
            _context.Configurations.Add(newConfig);
            _logger.LogInformation("Created configuration key: {Key}", key);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
