using AliveOnD_ID.Models;

namespace AliveOnD_ID.Controllers;

public class AddMessageRequest
{
    public MessageType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }
}
