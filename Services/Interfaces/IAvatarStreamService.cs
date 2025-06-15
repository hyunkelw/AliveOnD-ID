using AliveOnD_ID.Models;

namespace AliveOnD_ID.Services.Interfaces;

public interface IAvatarStreamService
{
    Task<DIDStreamResponse> CreateStreamAsync(string? presenterId = null, string? driverId = null);
    Task<bool> StartStreamAsync(string streamId, string sessionId, object sdpAnswer);
    Task<bool> SendIceCandidateAsync(string streamId, string sessionId, object iceCandidate);
    Task<bool> SendTextToAvatarAsync(string streamId, string sessionId, string text, string? emotion = null);
    Task<bool> SendAudioToAvatarAsync(string streamId, string sessionId, string audioUrl);
    Task<bool> CloseStreamAsync(string streamId, string sessionId);
}
