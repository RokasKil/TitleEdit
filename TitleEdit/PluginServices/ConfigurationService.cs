using Dalamud.Plugin;
using Dalamud.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using TitleEdit.Data.Persistence;
using TitleEdit.Utility;

namespace TitleEdit.PluginServices;

[Serializable]
public class ConfigurationService
{
    public int Version { get; set; } = 1;

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
        Type = CharacterDisplayType.Preset,
        PresetPath = "?/AetherialSea.json"
    };
    public List<KeyValuePair<ulong, CharacterDisplayTypeOption>> DisplayTypeOverrides = [];

    public TitleDisplayTypeOption TitleDisplayTypeOption = new()
    {
        Type = TitleDisplayType.Preset,
        PresetPath = "?/Dawntrail.json"
    };

    public TitleScreenLogo TitleScreenLogo = TitleScreenLogo.Dawntrail;
    public bool OverridePresetTitleScreenLogo = false;
    public bool DisplayTitleToast = false;
    public bool DebugLogging = false;
    public bool SettingsMigrated = false;
    public UiColorModel TitleScreenColor = UiColors.Dawntrail;
    public bool OverridePresetTitleScreenColor = false;

    [NonSerialized]
    private string filePath = null!;

    public static ConfigurationService Initialize(IDalamudPluginInterface pluginInterface)
    {
        ConfigurationService configurationService;
        string filePath = Path.Join(GetBaseConfigDirectory().CreateSubdirectory(PersistanceConsts.ConfigFolder).FullName, PersistanceConsts.ConfigName);
        if (File.Exists(filePath))
        {
            configurationService = Services.MigrationService.MigrateConfigurationService(File.ReadAllText(filePath)) ?? new();
        }
        else
        {
            configurationService = new();
        }
        configurationService.filePath = filePath;
        return configurationService;
    }

    public void Save()
    {
        Util.WriteAllTextSafe(filePath, JsonConvert.SerializeObject(this, Formatting.Indented));
    }

    public static DirectoryInfo GetBaseConfigDirectory()
    {
        return Services.PluginInterface.ConfigDirectory.CreateSubdirectory(PersistanceConsts.BaseFolder);
    }
}
