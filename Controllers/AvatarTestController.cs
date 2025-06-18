using Microsoft.AspNetCore.Mvc;
using AliveOnD_ID.Services.Interfaces;

namespace AliveOnD_ID.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AvatarTestController : ControllerBase
{
    private readonly IAvatarStreamService _avatarService;
    private readonly ILogger<AvatarTestController> _logger;
    private static readonly Dictionary<string, string> _activeStreams = new();

    public AvatarTestController(IAvatarStreamService avatarService, ILogger<AvatarTestController> logger)
    {
        _avatarService = avatarService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new D-ID stream for testing
    /// </summary>
    [HttpPost("create-stream")]
    public async Task<ActionResult<AvatarTestResponse>> CreateStream([FromBody] CreateAvatarStreamRequest? request = null)
    {
        try
        {
            _logger.LogInformation("Creating test avatar stream");

            var response = await _avatarService.CreateStreamAsync(
                request?.PresenterId, 
                request?.DriverId);

            // Store the stream info for later use
            var testSessionId = Guid.NewGuid().ToString();
            _activeStreams[testSessionId] = response.Id;

            var result = new AvatarTestResponse
            {
                StreamId = response.Id,
                SessionId = response.SessionId,
                TestSessionId = testSessionId,
                Message = "Stream created successfully. Use the TestSessionId for further operations.",
                Offer = response.Offer,
                IceServers = response.IceServers
            };

            _logger.LogInformation("Test avatar stream created: {StreamId}", response.Id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating test avatar stream");
            return StatusCode(500, new { error = "Failed to create avatar stream", details = ex.Message });
        }
    }

    /// <summary>
    /// Start the WebRTC connection for the stream
    /// </summary>
    [HttpPost("start-stream/{testSessionId}")]
    public async Task<ActionResult> StartStream(string testSessionId, [FromBody] StartStreamRequest request)
    {
        try
        {
            if (!_activeStreams.TryGetValue(testSessionId, out var streamId))
            {
                return NotFound("Test session not found");
            }

            var success = await _avatarService.StartStreamAsync(streamId, request.SessionId, request.SdpAnswer);
            
            if (!success)
            {
                return BadRequest("Failed to start stream");
            }

            return Ok(new { message = "Stream started successfully", streamId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting stream for test session {TestSessionId}", testSessionId);
            return StatusCode(500, new { error = "Failed to start stream", details = ex.Message });
        }
    }

    /// <summary>
    /// Send ICE candidate for the stream
    /// </summary>
    [HttpPost("ice-candidate/{testSessionId}")]
    public async Task<ActionResult> SendIceCandidate(string testSessionId, [FromBody] IceCandidateRequest request)
    {
        try
        {
            if (!_activeStreams.TryGetValue(testSessionId, out var streamId))
            {
                return NotFound("Test session not found");
            }

            var success = await _avatarService.SendIceCandidateAsync(
                streamId, 
                request.SessionId, 
                request.Candidate,
                request.Mid,
                request.LineIndex);
            
            if (!success)
            {
                return BadRequest("Failed to send ICE candidate");
            }

            return Ok(new { message = "ICE candidate sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending ICE candidate for test session {TestSessionId}", testSessionId);
            return StatusCode(500, new { error = "Failed to send ICE candidate", details = ex.Message });
        }
    }

    /// <summary>
    /// Make the avatar speak text (main testing function)
    /// </summary>
    [HttpPost("speak/{testSessionId}")]
    public async Task<ActionResult> MakeAvatarSpeak(string testSessionId, [FromBody] SpeakRequest request)
    {
        try
        {
            if (!_activeStreams.TryGetValue(testSessionId, out var streamId))
            {
                return NotFound("Test session not found");
            }

            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest("Text is required");
            }

            _logger.LogInformation("Making avatar speak: {Text}", request.Text);

            var success = await _avatarService.SendTextToAvatarAsync(
                streamId, 
                request.SessionId, 
                request.Text, 
                request.Emotion);
            
            if (!success)
            {
                return BadRequest("Failed to send text to avatar");
            }

            return Ok(new { 
                message = "Avatar speech request sent successfully", 
                text = request.Text,
                emotion = request.Emotion 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making avatar speak for test session {TestSessionId}", testSessionId);
            return StatusCode(500, new { error = "Failed to make avatar speak", details = ex.Message });
        }
    }

    /// <summary>
    /// Close the avatar stream
    /// </summary>
    [HttpDelete("close-stream/{testSessionId}")]
    public async Task<ActionResult> CloseStream(string testSessionId, [FromBody] CloseStreamRequest request)
    {
        try
        {
            if (!_activeStreams.TryGetValue(testSessionId, out var streamId))
            {
                return NotFound("Test session not found");
            }

            var success = await _avatarService.CloseStreamAsync(streamId, request.SessionId);
            
            if (success)
            {
                _activeStreams.Remove(testSessionId);
            }

            return Ok(new { message = "Stream closed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing stream for test session {TestSessionId}", testSessionId);
            return StatusCode(500, new { error = "Failed to close stream", details = ex.Message });
        }
    }

    /// <summary>
    /// Get list of active test streams
    /// </summary>
    [HttpGet("active-streams")]
    public ActionResult GetActiveStreams()
    {
        var streams = _activeStreams.Select(kvp => new
        {
            TestSessionId = kvp.Key,
            StreamId = kvp.Value
        }).ToList();

        return Ok(new { activeStreams = streams, count = streams.Count });
    }
}
