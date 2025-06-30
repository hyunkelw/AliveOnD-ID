using Newtonsoft.Json;

namespace AliveOnD_ID.Services.Models;
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