using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace AliveOnD_ID.Controllers;
#region Request Models

/// <summary>
/// Request to send script (text) to avatar
/// </summary>
public class SendScriptRequest
{
    [JsonPropertyName("session_id")]
    public string SessionId { get; set; } = string.Empty;
    
    [JsonProperty("script")]
    public ScriptData Script { get; set; } = new();
    
    [JsonProperty("config")]
    public ConfigData? Config { get; set; }
}

#endregion