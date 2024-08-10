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
    public CharacterDisplayTypeOption GlobalDisplayType = new()
    {
        Type = CharacterDisplayType.LastLocation
    };
    public CharacterDisplayTypeOption NoCharacterDisplayType = new()
    {
        Type = CharacterDisplayType.AetherialSea
    };
    public List<KeyValuePair<ulong, CharacterDisplayTypeOption>> DisplayTypeOverrides = [];

    public TitleDisplayTypeOption TitleDisplayTypeOption = new()
    {
        Type = TitleDisplayType.Preset,
        PresetPath = "?/Dawntrail.json"
    };

    public TitleScreenLogo TitleScreenLogo = TitleScreenLogo.Dawntrail;
    public bool OverridePresetTitleScreenLogo = false;


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
