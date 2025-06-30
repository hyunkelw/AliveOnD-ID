using AliveOnD_ID.Models.Configurations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AliveOnD_ID.Controllers;


[ApiController]
[Route("api/speech")]
public class SpeechController : ControllerBase
{
    private readonly ASRConfig _configuration;
    private readonly ILogger<SpeechController> _logger;

    public SpeechController(IOptions<ASRConfig> configuration, ILogger<SpeechController> logger)
    {
        _configuration = configuration.Value;
        _logger = logger;
    }
    
    [HttpGet("config")]
    public IActionResult GetSpeechConfig()
    {
       try
        {
            // Get from appsettings.json or environment variables
            var key = _configuration.ApiKey;
            var region = _configuration.Region;
            
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(region))
            {
                _logger.LogError("Azure Speech Services credentials not configured");
                return StatusCode(500, "Speech services not configured");
            }
            
            var config = new
            {
                key = key,
                region = region
            };
            
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving speech configuration");
            return StatusCode(500, "Failed to retrieve speech configuration");
        }
    }
}