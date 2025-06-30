using Newtonsoft.Json;

namespace AliveOnD_ID.Services.Models;
#region Request Models

/// <summary>
/// Stream configuration
/// </summary>
public class BackgroundConfigData
{
    [JsonProperty("color")]
    public bool Color { get; set; } = false;

}

#endregion