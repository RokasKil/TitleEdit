using Newtonsoft.Json;
using System.Linq;
using TitleEdit.Data.Persistence;
using TitleEdit.Data.Persistence.Migration;
using TitleEdit.Utility;

namespace TitleEdit.PluginServices.Migration
{
    public partial class MigrationService
    {
        public ConfigurationService? MigrateConfigurationService(string configurationTextData) => MigrateConfigurationService(configurationTextData, out _);

        public ConfigurationService? MigrateConfigurationService(string configurationTextData, out bool changed)
        {
            changed = false;
            var configurationService = JsonConvert.DeserializeObject<ConfigurationService>(configurationTextData);
            if (configurationService == null)
            {
                return null;
            }
            switch (configurationService.Version)
            {
                case 0:
                    configurationService = MigrateV0(configurationService);
                    changed = true;
                    break;
                default:
                    break;
            }
            return configurationService;
        }

        private ConfigurationService MigrateV0(ConfigurationService configurationService)
        {
            Services.Log.Info($"Migrating config to v1");
            configurationService.Version = 1;
            MigrateAetherialSeaDisplayType(ref configurationService.NoCharacterDisplayType);
            MigrateAetherialSeaDisplayType(ref configurationService.GlobalDisplayType);
            for (int i = 0; i < configurationService.DisplayTypeOverrides.Count; i++)
            {
                var item = configurationService.DisplayTypeOverrides[i];
                configurationService.DisplayTypeOverrides[i] = new(item.Key, MigrateAetherialSeaDisplayType(item.Value));
            }
            return configurationService;
        }

        private void MigrateAetherialSeaDisplayType(ref CharacterDisplayTypeOption option) => option = MigrateAetherialSeaDisplayType(option);
        private CharacterDisplayTypeOption MigrateAetherialSeaDisplayType(CharacterDisplayTypeOption option)
        {
#pragma warning disable CS0612 // Type or member is obsolete
            if (option.Type == CharacterDisplayType.AetherialSea)
            {
                option = new()
                {
                    Type = CharacterDisplayType.Preset,
                    PresetPath = "?/AetherialSea.json"
                };
            }
#pragma warning restore CS0612 // Type or member is obsolete
            return option;
        }

        public bool MigrateTitleScreenV2Configuration()
        {
            var oldConfig = Services.PluginInterface.GetPluginConfig() as TitleEditV2Configuration;
            if (oldConfig != null)
            {
                if (oldConfig.SelectedTitleFileName == "Random")
                {
                    Services.ConfigurationService.TitleDisplayTypeOption = new()
                    {
                        Type = TitleDisplayType.Random,
                        PresetPath = "?/RandomTitleScreen.json"
                    };
                }
                else if (oldConfig.SelectedTitleFileName == "Random (custom)" && oldConfig.TitleList != null)
                {
                    bool included = IncludedPresets.Contains(oldConfig.SelectedTitleFileName) || oldConfig.SelectedTitleFileName.StartsWith("TE_");
                    var groupModel = new GroupModel()
                    {
                        Name = "Random (custom)",
                        LocationType = LocationType.TitleScreen,
                        PresetFileNames = oldConfig.TitleList.Select((presetName) =>
                        {
                            bool included = IncludedPresets.Contains(presetName) || presetName.StartsWith("TE_");
                            return (string?)$"{(included ? "?/" : "")}{presetName}.json";
                        }).Where((presetPath) => Services.PresetService.TryGetPreset(presetPath!, out _, LocationType.TitleScreen))
                        .ToList(),
                        FileName = "random_custom.jsom"
                    };
                    if (groupModel.PresetFileNames.Count > 0)
                    {
                        Services.ConfigurationService.TitleDisplayTypeOption = new()
                        {
                            Type = TitleDisplayType.Random,
                            PresetPath = Services.GroupService.Save(groupModel)
                        };
                        Services.Log.Info("Transfered old Random (custom) settings");
                    }
                    else
                    {
                        Services.Log.Info($"Failed to transfer Random (custom) 0 valid presets found from {oldConfig.TitleList.Count}");
                    }
                }
                else if (!string.IsNullOrEmpty(oldConfig.SelectedTitleFileName))
                {
                    bool included = IncludedPresets.Contains(oldConfig.SelectedTitleFileName) || oldConfig.SelectedTitleFileName.StartsWith("TE_");

                    Services.ConfigurationService.TitleDisplayTypeOption = new()
                    {
                        Type = TitleDisplayType.Preset,
                        PresetPath = $"{(included ? "?/" : "")}{oldConfig.SelectedTitleFileName}.json"
                    };
                }
                if (Services.ConfigurationService.TitleDisplayTypeOption.Type == TitleDisplayType.Preset &&
                    !Services.PresetService.TryGetPreset(Services.ConfigurationService.TitleDisplayTypeOption.PresetPath!, out _, LocationType.TitleScreen))
                {
                    Services.Log.Info($"Failed to find valid preset after migrating settings");
                    Services.ConfigurationService.TitleDisplayTypeOption = new()
                    {
                        Type = TitleDisplayType.Preset,
                        PresetPath = $"?/Dawntrail.json"
                    };
                }
                if (!oldConfig.DisplayTitleLogo)
                {
                    Services.ConfigurationService.TitleScreenLogo = TitleScreenLogo.None;
                }
                else
                {
                    Services.ConfigurationService.TitleScreenLogo = oldConfig.SelectedLogoName switch
                    {
                        "A Realm Reborn" => TitleScreenLogo.ARealmReborn,
                        "FFXIV Free Trial" => TitleScreenLogo.FreeTrial,
                        "Heavensward" => TitleScreenLogo.Heavensward,
                        "Stormblood" => TitleScreenLogo.Stormblood,
                        "Shadowbringers" => TitleScreenLogo.Shadowbringers,
                        "Endwalker" => TitleScreenLogo.Endwalker,
                        "Dawntrail" => TitleScreenLogo.Dawntrail,
                        _ => TitleScreenLogo.None
                    };
                }
                Services.ConfigurationService.OverridePresetTitleScreenLogo = oldConfig.Override == OverrideSetting.Override || oldConfig.VisibilityOverride == OverrideSetting.Override;
                Services.ConfigurationService.DisplayTitleToast = oldConfig.DisplayTitleToast;
                Services.ConfigurationService.DebugLogging = oldConfig.DebugLogging;
            }
            Services.ConfigurationService.SettingsMigrated = true;
            Services.ConfigurationService.Save();
            return oldConfig != null;
        }
    }
}
