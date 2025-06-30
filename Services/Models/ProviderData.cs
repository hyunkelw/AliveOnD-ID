using Newtonsoft.Json;

namespace AliveOnD_ID.Services.Models;
#region Request Models

/// <summary>
/// TTS Provider configuration
/// </summary>
public class ProviderData
{
    [JsonProperty("type")]
    public string Type { get; set; } = "microsoft";
    
    [JsonProperty("voice_id")]
    public string VoiceId { get; set; } = "en-US-JennyNeural";
}

#endregion