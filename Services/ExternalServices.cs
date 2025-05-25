using Microsoft.Extensions.Options;
using AliveOnD_ID.Models;
using AliveOnD_ID.Services.Interfaces;
using Polly;
using System.Text;
using System.Text.Json;

namespace AliveOnD_ID.Services;

// Base service with retry logic
public abstract class BaseHttpService
{
    protected readonly HttpClient _httpClient;
    protected readonly ILogger _logger;
    protected readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

    protected BaseHttpService(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Configure retry policy
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Retry {RetryCount} for {RequestUri} in {Delay}ms. Reason: {Reason}",
                        retryCount, 
                        context.GetValueOrDefault("RequestUri", "Unknown"), 
                        timespan.TotalMilliseconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                });
    }

    protected async Task<T?> PostJsonAsync<T>(string endpoint, object data, string? authHeader = null)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            
            if (!string.IsNullOrEmpty(authHeader))
            {
                request.Headers.Add("Authorization", authHeader);
            }

            if (data != null)
            {
                var jsonContent = JsonSerializer.Serialize(data);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            }

            var context = new Context { ["RequestUri"] = endpoint };
            var response = await _retryPolicy.ExecuteAsync(async (ctx) =>
            {
                var httpResponse = await _httpClient.SendAsync(request);
                return httpResponse;
            }, context);

            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync();
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
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            
            if (!string.IsNullOrEmpty(authHeader))
            {
                request.Headers.Add("Authorization", authHeader);
            }

            if (data != null)
            {
                var jsonContent = JsonSerializer.Serialize(data);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            }

            var context = new Context { ["RequestUri"] = endpoint };
            var response = await _retryPolicy.ExecuteAsync(async (ctx) =>
            {
                return await _httpClient.SendAsync(request);
            }, context);

            var success = response.IsSuccessStatusCode;
            if (!success)
            {
                _logger.LogWarning("API call failed with status {StatusCode}: {Endpoint}", 
                    response.StatusCode, endpoint);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling API endpoint: {Endpoint}", endpoint);
            return false;
        }
    }
}

// Audio to Text Service
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

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = form
            };
            
            if (!string.IsNullOrEmpty(_config.ApiKey))
            {
                request.Headers.Add("Authorization", $"Bearer {_config.ApiKey}");
            }

            var context = new Context { ["RequestUri"] = endpoint };
            var response = await _retryPolicy.ExecuteAsync(async (ctx) =>
            {
                return await _httpClient.SendAsync(request);
            }, context);

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

// LLM Service
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

// D-ID Avatar Stream Service
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
            _logger.LogInformation("Creating D-ID stream");

            var endpoint = "/clips/streams";
            var requestData = new
            {
                presenter_id = presenterId ?? _config.PresenterId,
                driver_id = driverId ?? _config.DriverId
            };

            var authHeader = $"Basic {_config.ApiKey}";
            var result = await PostJsonAsync<DIDStreamResponse>(endpoint, requestData, authHeader);

            _logger.LogInformation("D-ID stream created. StreamId: {StreamId}", result?.Id);

            return result ?? throw new InvalidOperationException("Failed to create D-ID stream");
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
            var endpoint = $"/clips/streams/{streamId}/sdp";
            var requestData = new
            {
                answer = sdpAnswer,
                session_id = sessionId
            };

            var authHeader = $"Basic {_config.ApiKey}";
            return await PostAsync(endpoint, requestData, authHeader);
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
            var endpoint = $"/clips/streams/{streamId}/ice";
            var requestData = new
            {
                candidate = iceCandidate,
                session_id = sessionId
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
            var endpoint = $"/clips/streams/{streamId}";
            var requestData = new
            {
                script = new
                {
                    type = "text",
                    input = text
                },
                config = new
                {
                    stitch = true
                },
                session_id = sessionId
            };

            var authHeader = $"Basic {_config.ApiKey}";
            return await PostAsync(endpoint, requestData, authHeader);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending text to avatar stream {StreamId}", streamId);
            return false;
        }
    }

    public async Task<bool> SendAudioToAvatarAsync(string streamId, string sessionId, string audioUrl)
    {
        try
        {
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
                session_id = sessionId
            };

            var authHeader = $"Basic {_config.ApiKey}";
            return await PostAsync(endpoint, requestData, authHeader);
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
            var endpoint = $"/clips/streams/{streamId}";
            var requestData = new
            {
                session_id = sessionId
            };

            var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
            request.Headers.Add("Authorization", $"Basic {_config.ApiKey}");

            if (requestData != null)
            {
                var jsonContent = JsonSerializer.Serialize(requestData);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing avatar stream {StreamId}", streamId);
            return false;
        }
    }
}