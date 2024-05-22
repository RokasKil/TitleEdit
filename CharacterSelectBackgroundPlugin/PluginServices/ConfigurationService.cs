using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace CharacterSelectBackgroundPlugin.PluginServices;

[Serializable]
public class ConfigurationService : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool PeriodicSaving { get; set; } = true;
    public int SavePeriod { get; set; } = 10;
    // the below exist just to make saving less cumbersome
    [NonSerialized]
    private DalamudPluginInterface? PluginInterface;

    public void Initialize(DalamudPluginInterface pluginInterface)
    {
        PluginInterface = pluginInterface;
    }

    public void Save()
    {
        PluginInterface!.SavePluginConfig(this);
    }
}
