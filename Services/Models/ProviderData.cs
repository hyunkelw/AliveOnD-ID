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
    public string? VoiceId { get; set; } // Nullable, set from config
    
    [JsonProperty("voice_config")]
    public object? VoiceConfig { get; set; } // Nullable, set from config or left null
}

#endregion