using Newtonsoft.Json;

namespace AliveOnD_ID.Controllers;
#region Request Models

/// <summary>
/// Request to send ICE candidate
/// </summary>
public class SendIceCandidateRequest
{
    [JsonProperty("sessionId")]
    public string SessionId { get; set; } = string.Empty;
    
    [JsonProperty("candidate")]
    public object Candidate { get; set; } = new();
}

#endregion