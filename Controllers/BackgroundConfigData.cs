using Newtonsoft.Json;

namespace AliveOnD_ID.Controllers;
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