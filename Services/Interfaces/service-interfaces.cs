using AliveOnD_ID.Models;

namespace AliveOnD_ID.Services.Interfaces;

public interface IAudioToTextService
{
    Task<AudioToTextResponse> ConvertAudioToTextAsync(byte[] audioData, string fileName);
    Task<AudioToTextResponse> ConvertAudioToTextAsync(Stream audioStream, string fileName);
}

public interface ILLMService
{
    Task<LLMResponse> GetResponseAsync(string userMessage, List<ChatMessage>? conversationHistory = null);
    Task<LLMResponse> GetResponseAsync(string userMessage, string? userId = null, string? sessionId = null);
}

public interface IAvatarStreamService
{
    Task<DIDStreamResponse> CreateStreamAsync(string? presenterId = null, string? driverId = null);
    Task<bool> StartStreamAsync(string streamId, string sessionId, object sdpAnswer);
    Task<bool> SendIceCandidateAsync(string streamId, string sessionId, object iceCandidate);
    Task<bool> SendTextToAvatarAsync(string streamId, string sessionId, string text, string? emotion = null);
    Task<bool> SendAudioToAvatarAsync(string streamId, string sessionId, string audioUrl);
    Task<bool> CloseStreamAsync(string streamId, string sessionId);
}

public interface IChatSessionService
{
    Task<ChatSession> CreateSessionAsync(string userId);
    Task<ChatSession?> GetSessionAsync(string sessionId);
    Task<bool> UpdateSessionAsync(ChatSession session);
    Task<bool> DeleteSessionAsync(string sessionId);
    Task<List<ChatSession>> GetUserSessionsAsync(string userId);
    Task<bool> UserExistsAsync(string userId);
    Task<bool> AddMessageAsync(string sessionId, ChatMessage message);
    Task<bool> UpdateMessageAsync(string sessionId, ChatMessage message);
}

public interface IStorageService
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
    Task<bool> DeleteAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task<List<string>> GetKeysAsync(string pattern);
}

public interface IAudioProcessingService
{
    Task<byte[]> ConvertToMp3Async(byte[] audioData, string originalFormat);
    Task<bool> ValidateAudioAsync(byte[] audioData, int maxDurationSeconds);
    Task<string> SaveAudioFileAsync(byte[] audioData, string sessionId);

    Task<int> CleanupOldFilesAsync(TimeSpan maxAge);
}