using Microsoft.Extensions.Options;
using AliveOnD_ID.Models;
using AliveOnD_ID.Services.Interfaces;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AliveOnD_ID.Services;

// Simplified base service without retry logic
public abstract class BaseHttpService
{
    protected readonly HttpClient _httpClient;
    protected readonly ILogger _logger;

    protected BaseHttpService(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    protected async Task<T?> PostJsonAsync<T>(string endpoint, object data, string? authHeader = null)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);

            if (!string.IsNullOrEmpty(authHeader))
            {
                request.Headers.Add("Authorization", authHeader);
            }

            if (data != null)
            {
                var jsonContent = JsonSerializer.Serialize(data);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogDebug("POST {Endpoint}: {Json}", endpoint, jsonContent);
            }

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("Response {StatusCode}: {Content}", response.StatusCode, responseContent);

            response.EnsureSuccessStatusCode();

            return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling API endpoint: {Endpoint}", endpoint);
            throw;
        }
    }

    protected async Task<bool> PostAsync(string endpoint, object data, string? authHeader = null)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);

            if (!string.IsNullOrEmpty(authHeader))
            {
                request.Headers.Add("Authorization", authHeader);
            }

            if (data != null)
            {
                var jsonContent = JsonSerializer.Serialize(data);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogDebug("POST {Endpoint}: {Json}", endpoint, jsonContent);
            }

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("Response {StatusCode}: {Content}", response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API call failed with status {StatusCode}: {Endpoint}, Response: {Response}",
                    response.StatusCode, endpoint, responseContent);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling API endpoint: {Endpoint}", endpoint);
            return false;
        }
    }
}

// Audio to Text Service - Fixed without retry policy
public class AudioToTextService : BaseHttpService, IAudioToTextService
{
    private readonly AudioToTextConfig _config;

    public AudioToTextService(
        HttpClient httpClient,
        IOptions<ServiceConfiguration> config,
        ILogger<AudioToTextService> logger) : base(httpClient, logger)
    {
        _config = config.Value.AudioToText;
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.Timeout);
    }

    public async Task<AudioToTextResponse> ConvertAudioToTextAsync(byte[] audioData, string fileName)
    {
        using var stream = new MemoryStream(audioData);
        return await ConvertAudioToTextAsync(stream, fileName);
    }

    public async Task<AudioToTextResponse> ConvertAudioToTextAsync(Stream audioStream, string fileName)
    {
        try
        {
            _logger.LogInformation("Converting audio to text, file: {FileName}", fileName);

            // TODO: Replace with your actual ASR API endpoint and format
            var endpoint = "/api/speech-to-text"; // Replace with actual endpoint

            using var form = new MultipartFormDataContent();
            using var audioContent = new StreamContent(audioStream);
            audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/mpeg");
            form.Add(audioContent, "audio", fileName);

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = form
            };

            if (!string.IsNullOrEmpty(_config.ApiKey))
            {
                request.Headers.Add("Authorization", $"Bearer {_config.ApiKey}");
            }

            // Direct HTTP call without retry policy
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();

            // TODO: Parse according to your ASR API response format
            // This is a mock response structure
            var result = JsonSerializer.Deserialize<AudioToTextResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogInformation("Audio to text conversion completed. Text length: {Length}",
                result?.Text?.Length ?? 0);

            return result ?? new AudioToTextResponse { Text = "", Confidence = 0.0f };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting audio to text");
            throw;
        }
    }
}

public class AvatarStreamService : BaseHttpService, IAvatarStreamService
{
    private readonly DIDConfig _config;

    public AvatarStreamService(
        HttpClient httpClient,
        IOptions<ServiceConfiguration> config,
        ILogger<AvatarStreamService> logger) : base(httpClient, logger)
    {
        _config = config.Value.DID;
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(60); // Longer timeout for streaming
    }

    public async Task<DIDStreamResponse> CreateStreamAsync(string? presenterId = null, string? driverId = null)
    {
        try
        {
            _logger.LogInformation("Creating D-ID Clips stream with presenter: {PresenterId}, driver: {DriverId}",
                presenterId ?? _config.PresenterId, driverId ?? _config.DriverId);

            var endpoint = "/clips/streams";
            var requestData = new
            {
                presenter_id = presenterId ?? _config.PresenterId,
                driver_id = driverId ?? _config.DriverId
            };

            var authHeader = $"Basic {_config.ApiKey}";

            // Let's get the raw response first
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("Authorization", authHeader);
            request.Content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

            // _logger.LogDebug("Making request to D-ID: {Endpoint}", endpoint);
            // var result = await PostJsonAsync<DIDStreamCreateResponse>(endpoint, requestData, authHeader);

            // if (result == null)
            // {
            //     throw new InvalidOperationException("D-ID API returned null response");
            // }
            // _logger.LogDebug("Raw D-ID response: {Response}", JsonSerializer.Serialize(result));

            // // Convert to our response model
            // var response = new DIDStreamResponse
            // {
            //     Id = result.Id ?? throw new InvalidOperationException("Stream ID is null"),
            //     SessionId = result.SessionId ?? throw new InvalidOperationException("Session ID is null"),
            //     Offer = result.Offer ?? new object(),
            //     IceServers = result.IceServers?.Select(ice => new IceServer
            //     {
            //         Urls = ice.Urls,  // Now uses the helper property
            //         Username = ice.Username,
            //         Credential = ice.Credential
            //     }).ToList() ?? new List<IceServer>()
            // };

            var response = await _httpClient.SendAsync(request);
            var rawJson = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("Raw D-ID response: {RawResponse}", rawJson);
            // Now parse it
            var result = JsonSerializer.Deserialize<DIDStreamCreateResponse>(rawJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                _logger.LogDebug("Set-Cookie headers: {Cookies}", string.Join("; ", cookies));
            }

            var response_parsed = new DIDStreamResponse
            {
                Id = result.Id ?? throw new InvalidOperationException("Stream ID is null"),
                SessionId = result.SessionId ?? throw new InvalidOperationException("Session ID is null"),
                Offer = result.Offer ?? new object(),
                IceServers = result.IceServers?.Select(ice => new IceServer
                {
                    Urls = ice.Urls,  // Now uses the helper property
                    Username = ice.Username,
                    Credential = ice.Credential
                }).ToList() ?? new List<IceServer>()
            };

            _logger.LogInformation("D-ID stream created successfully. StreamId: {StreamId}, SessionId: {SessionId}",
                response_parsed.Id, response_parsed.SessionId);

            return response_parsed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating D-ID stream");
            throw;
        }
    }

    public async Task<bool> StartStreamAsync(string streamId, string sessionId, object sdpAnswer)
{
    try
    {
        _logger.LogInformation("Starting D-ID stream: {StreamId}", streamId);
        _logger.LogDebug("Using sessionId as-is: {SessionId}", sessionId);

        var endpoint = $"/clips/streams/{streamId}/sdp";
        var requestData = new
        {
            answer = sdpAnswer,
            session_id = sessionId  // Send exactly as received from D-ID
        };

        var authHeader = $"Basic {_config.ApiKey}";
        var success = await PostAsync(endpoint, requestData, authHeader);

        if (success)
        {
            _logger.LogInformation("D-ID stream started successfully: {StreamId}", streamId);
        }
        else
        {
            _logger.LogWarning("Failed to start D-ID stream: {StreamId}", streamId);
        }

        return success;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error starting D-ID stream {StreamId}", streamId);
        return false;
    }
}

    public async Task<bool> SendIceCandidateAsync(string streamId, string sessionId, object iceCandidate)
{
    try
    {
        _logger.LogDebug("Sending ICE candidate for stream: {StreamId}", streamId);

        var endpoint = $"/clips/streams/{streamId}/ice";
        var requestData = new
        {
            candidate = iceCandidate,
            session_id = sessionId  // Use as-is
        };

        var authHeader = $"Basic {_config.ApiKey}";
        return await PostAsync(endpoint, requestData, authHeader);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error sending ICE candidate for stream {StreamId}", streamId);
        return false;
    }
}

public async Task<bool> SendTextToAvatarAsync(string streamId, string sessionId, string text, string? emotion = null)
{
    try
    {
        _logger.LogInformation("Sending text to D-ID avatar stream {StreamId}: {Text}", streamId, text);

        var endpoint = $"/clips/streams/{streamId}";
        
        // Construct script data with proper TTS provider configuration
        var scriptData = new 
        {
            type = "text",
            input = text,
            provider = new 
            { 
                type = "microsoft",
                voice_id = "en-US-JennyNeural",
                voice_config = new
                {
                    rate = "+0%",  // Normal speaking rate
                    pitch = "+0%"  // Normal pitch
                }
            }
        };

        // Build configuration with detailed driver settings
        var configDict = new Dictionary<string, object>
        {
            ["stitch"] = true,
            ["driver"] = new 
            { 
                loop = false,
                enable_audio_normalization = true,
                motion_speed = 0.7,    // Slightly slower for more natural movement
                silence_padding = 0.2   // Add slight pause between sentences
            }
        };

        if (!string.IsNullOrEmpty(emotion))
        {
            configDict["driver_expressions"] = new { expression = emotion };
        }

        // Only strip the session ID if it's a cookie string
        var cleanSessionId = sessionId.Contains("AWSALB=") ? 
            ExtractSessionIdFromCookie(sessionId) : 
            sessionId;

        var requestData = new
        {
            script = scriptData,
            config = configDict,
            session_id = cleanSessionId
        };

        var authHeader = $"Basic {_config.ApiKey}";
        var success = await PostAsync(endpoint, requestData, authHeader);

        if (success)
        {
            _logger.LogInformation("Text sent successfully to D-ID avatar: {StreamId}", streamId);
        }
        else 
        {
            _logger.LogWarning("Failed to send text to D-ID avatar: {StreamId}", streamId);
        }

        return success;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error sending text to avatar stream {StreamId}: {Text}", streamId, text);
        return false;
    }
}

    // Helper method to extract session ID from AWS cookie
    private string ExtractSessionIdFromCookie(string cookieString)
    {
        try
        {
            _logger.LogDebug("Extracting session ID from: {CookieString}", cookieString);

            // If it's already a simple session ID, return as-is
            if (!cookieString.Contains("AWSALB="))
            {
                _logger.LogDebug("No AWSALB found, returning as-is: {SessionId}", cookieString);
                return cookieString;
            }

            // Extract the AWSALB value from the cookie string
            var awsAlbStart = cookieString.IndexOf("AWSALB=") + 7;
            var awsAlbEnd = cookieString.IndexOf(";", awsAlbStart);

            if (awsAlbEnd == -1)
            {
                awsAlbEnd = cookieString.Length;
            }

            var sessionValue = cookieString.Substring(awsAlbStart, awsAlbEnd - awsAlbStart);
            _logger.LogInformation("Extracted session ID: '{ExtractedSessionId}' from cookie", sessionValue);
            return sessionValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract session ID from cookie: {CookieString}", cookieString);
            return cookieString;
        }
    }

    public async Task<bool> SendAudioToAvatarAsync(string streamId, string sessionId, string audioUrl)
    {
        try
        {
            _logger.LogInformation("Sending audio to D-ID avatar stream {StreamId}: {AudioUrl}", streamId, audioUrl);

            // Extract just the session ID value from the cookie string
            var cleanSessionId = ExtractSessionIdFromCookie(sessionId);

            var endpoint = $"/clips/streams/{streamId}";
            var requestData = new
            {
                script = new
                {
                    type = "audio",
                    audio_url = audioUrl
                },
                config = new
                {
                    stitch = true
                },
                session_id = cleanSessionId
            };

            var authHeader = $"Basic {_config.ApiKey}";
            var success = await PostAsync(endpoint, requestData, authHeader);

            if (success)
            {
                _logger.LogInformation("Audio sent successfully to D-ID avatar: {StreamId}", streamId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending audio to avatar stream {StreamId}", streamId);
            return false;
        }
    }

    public async Task<bool> CloseStreamAsync(string streamId, string sessionId)
    {
        try
        {
            _logger.LogInformation("Closing D-ID stream: {StreamId}", streamId);

            // Extract just the session ID value from the cookie string
            var cleanSessionId = ExtractSessionIdFromCookie(sessionId);

            var endpoint = $"/clips/streams/{streamId}";
            var requestData = new
            {
                session_id = cleanSessionId
            };

            var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
            request.Headers.Add("Authorization", $"Basic {_config.ApiKey}");

            if (requestData != null)
            {
                var jsonContent = JsonSerializer.Serialize(requestData);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(request);
            var success = response.IsSuccessStatusCode;

            if (success)
            {
                _logger.LogInformation("D-ID stream closed successfully: {StreamId}", streamId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing avatar stream {StreamId}", streamId);
            return false;
        }
    }
}

// D-ID API Response Models
public class DIDStreamCreateResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("session_id")]
    public string? SessionId { get; set; }

    [JsonPropertyName("offer")]
    public object? Offer { get; set; }

    [JsonPropertyName("ice_servers")]
    public List<DIDIceServer>? IceServers { get; set; }
}

public class DIDIceServer
{
    [JsonPropertyName("urls")]
    public object? UrlsRaw { get; set; }  // Handle both string and array

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("credential")]
    public string? Credential { get; set; }

    // Helper property to get URLs as List<string> - ignored during JSON serialization
    [JsonIgnore]
    public List<string> Urls
    {
        get
        {
            if (UrlsRaw == null) return new List<string>();

            // If it's already a JsonElement array
            if (UrlsRaw is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Array)
                {
                    return element.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToList();
                }
                else if (element.ValueKind == JsonValueKind.String)
                {
                    return new List<string> { element.GetString() ?? string.Empty };
                }
            }

            // If it's a string
            if (UrlsRaw is string singleUrl)
            {
                return new List<string> { singleUrl };
            }

            return new List<string>();
        }
    }
}

// LLM Service - Already clean, no retry policy needed
public class LLMService : BaseHttpService, ILLMService
{
    private readonly LLMConfig _config;

    public LLMService(
        HttpClient httpClient,
        IOptions<ServiceConfiguration> config,
        ILogger<LLMService> logger) : base(httpClient, logger)
    {
        _config = config.Value.LLM;
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.Timeout);
    }

    public async Task<LLMResponse> GetResponseAsync(string userMessage, List<ChatMessage>? conversationHistory = null)
    {
        try
        {
            _logger.LogInformation("Getting LLM response for message length: {Length}", userMessage.Length);

            // TODO: Replace with your actual LLM API endpoint and format
            var endpoint = "/api/chat/completions"; // Replace with actual endpoint


            var requestData = new
            {
                model = _config.Model,
                messages = BuildMessageHistory(userMessage, conversationHistory),
                max_tokens = 1000,
                temperature = 0.7
            };

            var authHeader = !string.IsNullOrEmpty(_config.ApiKey) ? $"Bearer {_config.ApiKey}" : null;
            var result = await PostJsonAsync<LLMResponse>(endpoint, requestData, authHeader);

            _logger.LogInformation("LLM response received. Response length: {Length}",
                result?.Text?.Length ?? 0);

            return result ?? new LLMResponse { Text = "I'm sorry, I couldn't process your request right now." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting LLM response");
            throw;
        }
    }

    public async Task<LLMResponse> GetResponseAsync(string userMessage, string? userId = null, string? sessionId = null)
    {
        // For now, this just calls the main method without additional context
        // You can extend this to fetch conversation history based on sessionId
        return await GetResponseAsync(userMessage, (List<ChatMessage>?)null);
    }

    private object[] BuildMessageHistory(string userMessage, List<ChatMessage>? conversationHistory)
    {
        var messages = new List<object>();

        // Add system message
        messages.Add(new { role = "system", content = "You are a helpful AI assistant with a friendly personality." });

        // Add conversation history
        if (conversationHistory != null)
        {
            foreach (var message in conversationHistory.TakeLast(10)) // Limit to last 10 messages
            {
                if (message.Type == MessageType.UserText || message.Type == MessageType.UserAudio)
                {
                    messages.Add(new { role = "user", content = message.Content });
                }
                else if (message.Type == MessageType.AssistantText)
                {
                    messages.Add(new { role = "assistant", content = message.Content });
                }
            }
        }

        // Add current user message
        messages.Add(new { role = "user", content = userMessage });

        return messages.ToArray();
    }
}