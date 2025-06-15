using Microsoft.AspNetCore.Mvc;
using AliveOnD_ID.Models;
using AliveOnD_ID.Services.Interfaces;

namespace AliveOnD_ID.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AvatarController : ControllerBase
{
    private readonly IAvatarStreamService _avatarService;
    private readonly ILogger<AvatarController> _logger;

    public AvatarController(IAvatarStreamService avatarService, ILogger<AvatarController> logger)
    {
        _avatarService = avatarService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new D-ID stream
    /// </summary>
    [HttpPost("stream/create")]
    public async Task<ActionResult<DIDStreamResponse>> CreateStream([FromBody] CreateStreamRequest? request = null)
    {
        try
        {
            var response = await _avatarService.CreateStreamAsync(
                request?.PresenterId,
                request?.DriverId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating D-ID stream");
            return StatusCode(500, new { error = "Avatar service error", details = ex.Message });
        }
    }

    /// <summary>
    /// Send script to avatar stream (for making avatar speak)
    /// </summary>
    [HttpPost("stream/{streamId}")]
    public async Task<ActionResult> SendScript(string streamId, [FromBody] SendScriptRequest request)
    {
        try
        {
            _logger.LogDebug("Received script request for stream {StreamId}", streamId);

            if (string.IsNullOrEmpty(request.SessionId))
            {
                return BadRequest("Missing required field: session_id");
            }

            if (request.Script == null || string.IsNullOrEmpty(request.Script.Input))
            {
                return BadRequest("Missing required field: script.input");
            }

            var success = await _avatarService.SendTextToAvatarAsync(
                streamId,
                request.SessionId,
                request.Script.Input,
                null); // emotion can be extracted from provider if needed

            if (!success)
            {
                return BadRequest("Failed to send text to avatar");
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending script to avatar stream {StreamId}", streamId);
            return StatusCode(500, new { error = "Avatar service error", details = ex.Message });
        }
    }

    /// <summary>
    /// Start the stream with SDP answer
    /// </summary>
    [HttpPost("stream/{streamId}/start")]
    public async Task<ActionResult> StartStream(string streamId, [FromBody] StartStreamRequest request)
    {
        try
        {
            _logger.LogDebug("Starting stream {StreamId}", streamId);

            if (string.IsNullOrEmpty(request.SessionId))
            {
                return BadRequest("Missing required field: sessionId");
            }

            if (request.SdpAnswer == null)
            {
                return BadRequest("Missing required field: sdpAnswer");
            }

            var success = await _avatarService.StartStreamAsync(streamId, request.SessionId, request.SdpAnswer);

            if (!success)
            {
                return BadRequest("Failed to start stream");
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting stream {StreamId}", streamId);
            return StatusCode(500, new { error = "Avatar service error", details = ex.Message });
        }
    }

    /// <summary>
    /// Send ICE candidate
    /// </summary>
    [HttpPost("stream/{streamId}/ice")]
    public async Task<ActionResult> SendIceCandidate(string streamId, [FromBody] SendIceCandidateRequest request)
    {
        try
        {
            _logger.LogDebug("Sending ICE candidate for stream {StreamId}", streamId);

            if (string.IsNullOrEmpty(request.SessionId))
            {
                return BadRequest("Missing required field: sessionId");
            }

            if (request.Candidate == null)
            {
                return BadRequest("Missing required field: candidate");
            }

            var success = await _avatarService.SendIceCandidateAsync(streamId, request.SessionId, request.Candidate);

            if (!success)
            {
                return BadRequest("Failed to send ICE candidate");
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending ICE candidate for stream {StreamId}", streamId);
            return StatusCode(500, new { error = "Avatar service error", details = ex.Message });
        }
    }

    /// <summary>
    /// Close avatar stream
    /// </summary>
    [HttpDelete("stream/{streamId}")]
    public async Task<ActionResult> CloseStream(string streamId, [FromBody] CloseStreamRequest request)
    {
        try
        {
            _logger.LogDebug("Closing stream {StreamId}", streamId);

            if (string.IsNullOrEmpty(request.SessionId))
            {
                return BadRequest("Missing required field: session_id");
            }

            var success = await _avatarService.CloseStreamAsync(streamId, request.SessionId);

            if (!success)
            {
                return BadRequest("Failed to close stream");
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing stream {StreamId}", streamId);
            return StatusCode(500, new { error = "Avatar service error", details = ex.Message });
        }
    }
}