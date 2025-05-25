namespace AliveOnD_ID.Models;

public class ServiceConfiguration
{
    public AudioToTextConfig AudioToText { get; set; } = new();
    public LLMConfig LLM { get; set; } = new();
    public DIDConfig DID { get; set; } = new();
}

public class AudioToTextConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public int Timeout { get; set; } = 30;
}

public class LLMConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Timeout { get; set; } = 60;
}

public class DIDConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string PresenterId { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
}

public class RedisConfig
{
    public string ConnectionString { get; set; } = "localhost:6379";
    public int DefaultDatabase { get; set; } = 0;
    public string KeyPrefix { get; set; } = "AvatarChat:";
}

public class ChatConfig
{
    public int SessionTimeoutMinutes { get; set; } = 30;
    public int MaxMessagesPerSession { get; set; } = 100;
    public int MaxAudioDurationSeconds { get; set; } = 60;
}