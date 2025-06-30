namespace AliveOnD_ID.Controllers.Requests;

public class SendTextRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string? Emotion { get; set; }
}