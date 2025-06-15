namespace AliveOnD_ID.Models;

public class LLMResponse
{
    public string Text { get; set; } = string.Empty;
    public string? Emotion { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
