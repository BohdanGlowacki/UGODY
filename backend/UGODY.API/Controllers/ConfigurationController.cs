using Microsoft.AspNetCore.Mvc;
using UGODY.Application.Services;

namespace UGODY.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<ConfigurationController> _logger;

    public ConfigurationController(
        IConfigurationService configurationService,
        ILogger<ConfigurationController> logger)
    {
        _configurationService = configurationService;
        _logger = logger;
    }

    [HttpGet("{key}")]
    public async Task<ActionResult<string?>> GetValue(string key, CancellationToken cancellationToken)
    {
        try
        {
            var value = await _configurationService.GetValueAsync(key, cancellationToken);
            if (value == null)
            {
                return NotFound($"Configuration key '{key}' not found.");
            }
            return Ok(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration value for key: {Key}", key);
            return StatusCode(500, "An error occurred while retrieving the configuration value.");
        }
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> SetValue(string key, [FromBody] string value, CancellationToken cancellationToken)
    {
        try
        {
            await _configurationService.SetValueAsync(key, value, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting configuration value for key: {Key}", key);
            return StatusCode(500, "An error occurred while setting the configuration value.");
        }
    }
}
