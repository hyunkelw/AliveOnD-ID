using Newtonsoft.Json;

namespace AliveOnD_ID.Controllers;
#region Request Models

/// <summary>
/// Stream configuration
/// </summary>
public class ConfigData
{
    [JsonProperty("stitch")]
    public bool Stitch { get; set; } = true;
}

#endregion