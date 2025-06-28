using Microsoft.AspNetCore.Mvc;

namespace AliveOnD_ID.Controllers;


[ApiController]
[Route("api/speech")]
public class SpeechController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SpeechController> _logger;

    public SpeechController(IConfiguration configuration, ILogger<SpeechController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }
    
    [HttpGet("config")]
    public IActionResult GetSpeechConfig()
    {
       try
        {
            // Get from appsettings.json or environment variables
            var key = _configuration["Services:AzureSpeechServices:Key"];
            var region = _configuration["Services:AzureSpeechServices:Region"];
            
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