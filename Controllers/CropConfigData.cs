using Newtonsoft.Json;

namespace AliveOnD_ID.Controllers;
#region Request Models

/// <summary>
/// Stream configuration
/// </summary>
public class CropConfigData
{
    [JsonProperty("type")]
    public string Type { get; set; } = "wide";

    [JsonProperty("rectangle")]
    public RectangleConfigData? Rectangle { get; set; }
}

#endregion