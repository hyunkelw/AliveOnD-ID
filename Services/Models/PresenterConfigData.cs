using Newtonsoft.Json;

namespace AliveOnD_ID.Services.Models;
#region Request Models

/// <summary>
/// Stream configuration
/// </summary>
public class PresenterConfigData
{
    [JsonProperty("crop")]
    public CropConfigData? CropConfigData { get; set; }
}

#endregion