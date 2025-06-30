using System.Text.Json.Serialization;

namespace AliveOnD_ID.Models.Chat;

public class EVEConversationStarted
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; set; } = string.Empty;
}