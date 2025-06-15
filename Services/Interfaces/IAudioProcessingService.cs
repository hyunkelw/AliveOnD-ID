namespace AliveOnD_ID.Services.Interfaces;

public interface IAudioProcessingService
{
    Task<byte[]> ConvertToMp3Async(byte[] audioData, string originalFormat);
    Task<bool> ValidateAudioAsync(byte[] audioData, int maxDurationSeconds);
    Task<string> SaveAudioFileAsync(byte[] audioData, string sessionId);

    Task<int> CleanupOldFilesAsync(TimeSpan maxAge);
}