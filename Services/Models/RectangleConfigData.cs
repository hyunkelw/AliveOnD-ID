using Newtonsoft.Json;

namespace AliveOnD_ID.Services.Models;
#region Request Models

/// <summary>
/// Stream configuration
/// </summary>
public class RectangleConfigData
{
    [JsonProperty("bottom")]
    public double Bottom { get; set; } = 1;

    [JsonProperty("right")]
    public double Right { get; set; } = 1;

    [JsonProperty("left")]
    public double Left { get; set; } = 0;

    [JsonProperty("top")]
    public double Top { get; set; } = 0;
}

#endregion