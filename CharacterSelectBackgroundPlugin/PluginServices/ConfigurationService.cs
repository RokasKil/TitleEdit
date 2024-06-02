using CharacterSelectBackgroundPlugin.Data;
using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace CharacterSelectBackgroundPlugin.PluginServices;

[Serializable]
public class ConfigurationService : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public bool TrackPlayerLocation { get; set; } = true;
    public bool PeriodicSaving { get; set; } = true;
    public int SavePeriod { get; set; } = 10;
    public bool SaveLayout { get; set; } = true;
    public bool SaveLayoutInInstance { get; set; } = true;
    public bool SaveMount { get; set; } = true;
    public bool SaveBgm { get; set; } = true;
    public bool SaveTime { get; set; } = true;
    public DisplayTypeOption GlobalDisplayType { get; set; } = new()
    {
        type = DisplayType.Preset
    };

    public Dictionary<ulong, DisplayTypeOption> DisplayTypeOverrides { get; set; } = [];
    // the below exist just to make saving less cumbersome
    [NonSerialized]
    private DalamudPluginInterface? pluginInterface;

    public void Initialize(DalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
    }

    public void Save()
    {
        pluginInterface!.SavePluginConfig(this);
    }
}
