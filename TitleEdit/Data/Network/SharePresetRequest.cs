using Newtonsoft.Json;
using TitleEdit.Data.Persistence;

namespace TitleEdit.Data.Network;

public class SharePresetRequest
{
    [JsonProperty("preset")]
    public PresetModel Preset { get; set; }
}
