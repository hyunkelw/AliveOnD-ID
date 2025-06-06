using Microsoft.AspNetCore.Mvc;
using AliveOnD_ID.Models;
using AliveOnD_ID.Services.Interfaces;

namespace AliveOnD_ID.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;

    public TestController(ILogger<TestController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Test endpoint to verify API is working
    /// </summary>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}

[ApiController]
[Route("api/[controller]")]
public class SessionController : ControllerBase
{
    private readonly IChatSessionService _sessionService;
    private readonly ILogger<SessionController> _logger;

    public SessionController(IChatSessionService sessionService, ILogger<SessionController> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new chat session
    /// </summary>
    [HttpPost("create")]
    public async Task<ActionResult<ChatSession>> CreateSession([FromBody] CreateSessionRequest request)
    {
        try
        {
            // Validate userId
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return BadRequest("UserId is required and cannot be empty");
            }

            if (request.UserId.Length > 100) // Reasonable limit
            {
                return BadRequest("UserId cannot exceed 100 characters");
            }

            var session = await _sessionService.CreateSessionAsync(request.UserId);
            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session for user {UserId}", request.UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get an existing chat session
    /// </summary>
    [HttpGet("{sessionId}")]
    public async Task<ActionResult<ChatSession>> GetSession(string sessionId)
    {
        try
        {
            var session = await _sessionService.GetSessionAsync(sessionId);
            if (session == null)
            {
                return NotFound($"Session {sessionId} not found");
            }
            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session {SessionId}", sessionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Add a message to a session
    /// </summary>
    [HttpPost("{sessionId}/messages")]
    public async Task<ActionResult> AddMessage(string sessionId, [FromBody] AddMessageRequest request)
    {
        try
        {
            var message = new ChatMessage
            {
                Type = request.Type,
                Content = request.Content,
                AudioUrl = request.AudioUrl
            };

            var success = await _sessionService.AddMessageAsync(sessionId, message);
            if (!success)
            {
                return BadRequest("Failed to add message");
            }

            return Ok(new { messageId = message.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding message to session {SessionId}", sessionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get all sessions for a user
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<ChatSession>>> GetUserSessions(string userId)
    {
        try
        {
            // Validate userId format
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest("UserId cannot be empty");
            }

            // Check if user exists
            var userExists = await _sessionService.UserExistsAsync(userId);
            if (!userExists)
            {
                return NotFound($"User '{userId}' does not exist");
            }

            // Get user sessions
            var sessions = await _sessionService.GetUserSessionsAsync(userId);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sessions for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }
}

[ApiController]
[Route("api/[controller]")]
public class LLMController : ControllerBase
{
    private readonly ILLMService _llmService;
    private readonly ILogger<LLMController> _logger;

    public LLMController(ILLMService llmService, ILogger<LLMController> logger)
    {
        _llmService = llmService;
        _logger = logger;
    }

    /// <summary>
    /// Test LLM service with a simple message
    /// </summary>
    [HttpPost("test")]
    public async Task<ActionResult<LLMResponse>> TestLLM([FromBody] TestLLMRequest request)
    {
        try
        {
            // Explicitly call the overload with conversation history
            var response = await _llmService.GetResponseAsync(request.Message, (List<ChatMessage>?)null);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing LLM service");
            return StatusCode(500, new { error = "LLM service error", details = ex.Message });
        }
    }

    /// <summary>
    /// Test LLM service with session context
    /// </summary>
    [HttpPost("test-with-session")]
    public async Task<ActionResult<LLMResponse>> TestLLMWithSession([FromBody] TestLLMWithSessionRequest request)
    {
        try
        {
            // Use the overload with userId and sessionId
            var response = await _llmService.GetResponseAsync(request.Message, request.UserId, request.SessionId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing LLM service with session");
            return StatusCode(500, new { error = "LLM service error", details = ex.Message });
        }
    }
}

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
    /// Send text to avatar stream
    /// </summary>
    [HttpPost("stream/{streamId}/text")]
    public async Task<ActionResult> SendText(string streamId, [FromBody] SendTextRequest request)
    {
        try
        {
            var success = await _avatarService.SendTextToAvatarAsync(
                streamId,
                request.SessionId,
                request.Text,
                request.Emotion);

            if (!success)
            {
                return BadRequest("Failed to send text to avatar");
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending text to avatar stream {StreamId}", streamId);
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
}

// Request/Response Models for API
public class CreateSessionRequest
{
    public string UserId { get; set; } = string.Empty;
}

public class AddMessageRequest
{
    public MessageType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }
}

public class TestLLMRequest
{
    public string Message { get; set; } = string.Empty;
}

public class TestLLMWithSessionRequest
{
    public string Message { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? SessionId { get; set; }
}

public class CreateStreamRequest
{
    public string? PresenterId { get; set; }
    public string? DriverId { get; set; }
}

public class SendTextRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string? Emotion { get; set; }
}

public class StartStreamRequest
{
    public string SessionId { get; set; } = string.Empty;
    public object SdpAnswer { get; set; } = new();
}

public class SendIceCandidateRequest
{
    public string SessionId { get; set; } = string.Empty;
    public object Candidate { get; set; } = new();
}