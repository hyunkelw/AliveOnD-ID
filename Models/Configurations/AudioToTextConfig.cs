namespace AliveOnD_ID.Models.Configurations;

public class AudioToTextConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public int Timeout { get; set; } = 30;
}
