using AliveOnD_ID.Models;

namespace AliveOnD_ID.Controllers.Responses;

public class AvatarTestResponse
{
    public string StreamId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string TestSessionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object Offer { get; set; } = new();
    public List<IceServer> IceServers { get; set; } = new();
}
