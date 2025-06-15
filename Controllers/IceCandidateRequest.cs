namespace AliveOnD_ID.Controllers;

public class IceCandidateRequest
{
    public string SessionId { get; set; } = string.Empty;
    public object Candidate { get; set; } = new();
}
