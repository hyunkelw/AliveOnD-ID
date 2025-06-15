using Newtonsoft.Json;

namespace AliveOnD_ID.Models;

public class DIDStreamResponse
{
    public string Id { get; set; } = string.Empty;

    [JsonProperty("session_id")]  // Add this attribute
    public string SessionId { get; set; } = string.Empty;

    public object Offer { get; set; } = new();
    public List<IceServer> IceServers { get; set; } = new();
}
