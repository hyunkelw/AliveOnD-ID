namespace AliveOnD_ID.Controllers.Requests;

public class IceCandidateRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string Candidate { get; set; } = string.Empty;
    public string Mid { get; set; } = string.Empty;
    public int LineIndex { get; set; }
}
