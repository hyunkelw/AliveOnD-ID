using AliveOnD_ID.Models;

namespace AliveOnD_ID.Services.Interfaces;

public interface IAudioToTextService
{
    Task<AudioToTextResponse> ConvertAudioToTextAsync(byte[] audioData, string fileName);
    Task<AudioToTextResponse> ConvertAudioToTextAsync(Stream audioStream, string fileName);
}
