namespace AliveOnD_ID.Controllers;

public class SpeakRequest
{
    public string Text { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string? Emotion { get; set; }
}