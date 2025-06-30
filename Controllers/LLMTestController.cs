using Microsoft.AspNetCore.Mvc;

using AliveOnD_ID.Services.Interfaces;
using System.Net.WebSockets;
using AliveOnD_ID.Controllers.Requests;

namespace AliveOnD_ID.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebSocketTestController : ControllerBase
{
    private readonly ILLMService _llmService;
    private readonly ILogger<WebSocketTestController> _logger;

    public WebSocketTestController(ILLMService llmService, ILogger<WebSocketTestController> logger)
    {
        _llmService = llmService;
        _logger = logger;
    }

    /// <summary>
    /// Test basic WebSocket connectivity to EVE endpoint
    /// </summary>
    [HttpPost("test-connection")]
    public async Task<IActionResult> TestConnection([FromBody] ConnectionTestRequest request)
    {
        try
        {
            _logger.LogInformation("Testing WebSocket connection to: {Url}", request.Url);

            using var ws = new ClientWebSocket();

            // Add subprotocols if API key provided
            if (!string.IsNullOrEmpty(request.ApiKey))
            {
                ws.Options.AddSubProtocol("chat");
                ws.Options.AddSubProtocol(request.ApiKey);
            }

            // Try to connect
            var uri = new Uri(request.Url);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            await ws.ConnectAsync(uri, cts.Token);

            var result = new
            {
                success = true,
                state = ws.State.ToString(),
                subProtocol = ws.SubProtocol,
                closeStatus = ws.CloseStatus?.ToString(),
                closeDescription = ws.CloseStatusDescription
            };

            // Clean close
            if (ws.State == WebSocketState.Open)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket connection test failed");
            return Ok(new
            {
                success = false,
                error = ex.Message,
                type = ex.GetType().Name,
                suggestion = GetConnectionSuggestion(ex)
            });
        }
    }

    /// <summary>
    /// Test EVE API with a simple message
    /// </summary>
    [HttpPost("test-eve")]
    public async Task<IActionResult> TestEVE([FromBody] TestRequest request)
    {
        try
        {
            _logger.LogInformation("Testing EVE with message: {Message}", request.Message);
            var response = await _llmService.GetResponseAsync(request.Message, null);

            return Ok(new
            {
                success = true,
                response = response.Text,
                emotion = response.Emotion,
                conversationId = response.Metadata?["conversation_id"]
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EVE test failed");
            return StatusCode(500, new
            {
                error = ex.Message,
                type = ex.GetType().Name,
                suggestion = GetConnectionSuggestion(ex)
            });
        }
    }

    private string GetConnectionSuggestion(Exception ex)
    {
        return ex switch
        {
            WebSocketException wse when wse.Message.Contains("401") =>
                "Authentication failed. Check your API key.",
            WebSocketException wse when wse.Message.Contains("404") =>
                "Endpoint not found. Verify the WebSocket URL.",
            WebSocketException wse when wse.Message.Contains("SSL") || wse.Message.Contains("TLS") =>
                "SSL/TLS error. Try using ws:// instead of wss:// or vice versa.",
            TaskCanceledException or OperationCanceledException =>
                "Connection timeout. Check if the server is accessible and the URL is correct.",
            _ => "Check the WebSocket URL format and ensure the server is running."
        };
    }
}

