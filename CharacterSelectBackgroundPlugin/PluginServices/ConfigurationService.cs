using CharacterSelectBackgroundPlugin.Data.Persistence;
using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace CharacterSelectBackgroundPlugin.PluginServices;

[Serializable]
public class ConfigurationService : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool TrackPlayerLocation = true;
    public bool PeriodicSaving = true;
    public int SavePeriod = 10;
    public bool SaveLayout = true;
    public bool SaveLayoutInInstance = true;
    public bool SaveMount = true;
    public bool SaveBgm = true;
    public bool SaveTime = true;
    public bool DrawCharacterSelectButton = true;
    public CameraFollowMode CameraFollowMode = CameraFollowMode.ModelPosition;
    public DisplayTypeOption GlobalDisplayType = new()
    {
        Type = DisplayType.LastLocation
    };
    public DisplayTypeOption NoCharacterDisplayType = new()
    {
        Type = DisplayType.AetherialSea
    };

    public List<KeyValuePair<ulong, DisplayTypeOption>> DisplayTypeOverrides = [];

    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
    }

    public void Save()
    {
        pluginInterface!.SavePluginConfig(this);
    }
}
