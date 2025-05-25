namespace AliveOnD_ID.Models;

public class ChatSession
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public List<ChatMessage> Messages { get; set; } = new();
    public AvatarStreamInfo? ActiveStream { get; set; }
}

public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public MessageType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public MessageStatus Status { get; set; } = MessageStatus.Pending;
    public string? AudioUrl { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum MessageType
{
    UserText,
    UserAudio,
    AssistantText,
    AssistantAvatar,
    System
}

public enum MessageStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public class AvatarStreamInfo
{
    public string StreamId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public StreamStatus Status { get; set; } = StreamStatus.Disconnected;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum StreamStatus
{
    Disconnected,
    Connecting,
    Connected,
    Speaking,
    Error
}

// API Response Models
public class AudioToTextResponse
{
    public string Text { get; set; } = string.Empty;
    public float Confidence { get; set; }
}

public class LLMResponse
{
    public string Text { get; set; } = string.Empty;
    public string? Emotion { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class DIDStreamResponse
{
    public string Id { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public object Offer { get; set; } = new();
    public List<IceServer> IceServers { get; set; } = new();
}

public class IceServer
{
    public List<string> Urls { get; set; } = new();
    public string? Username { get; set; }
    public string? Credential { get; set; }
}