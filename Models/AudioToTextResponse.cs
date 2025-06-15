namespace AliveOnD_ID.Models;

// API Response Models
public class AudioToTextResponse
{
    public string Text { get; set; } = string.Empty;
    public float Confidence { get; set; }
}
