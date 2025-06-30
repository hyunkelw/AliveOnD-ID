using Newtonsoft.Json;

namespace AliveOnD_ID.Services.Models;
#region Request Models

/// <summary>
/// Script data structure
/// </summary>
public class ScriptData
{
    [JsonProperty("type")]
    public string Type { get; set; } = "text";
    
    [JsonProperty("provider")]
    public ProviderData? Provider { get; set; }
    
    [JsonProperty("input")]
    public string Input { get; set; } = string.Empty;
    
    [JsonProperty("ssml")]
    public bool Ssml { get; set; } = false;
}

#endregion