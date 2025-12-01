using Newtonsoft.Json;
using TitleEdit.Data.Persistence;

namespace TitleEdit.Data.Network;

public class SharePresetResponse
{
    [JsonProperty("code")]
    public required string Code { get; set; }
}
