using System.Numerics;
using TitleEdit.Data.Lobby;
using TitleEdit.Data.Persistence;

namespace TitleEdit.PluginServices.Preset
{
    public partial class PresetService
    {
        private void LoadBasePresets()
        {
            // Title screens
            AddPreset(new()
            {
                Name = "A Realm Reborn",
                Tooltip = "Vanilla A Realm Reborn title screen",
                FileName = "?/ARealmReborn.json",
                LocationModel = new()
                {
                    LocationType = LocationType.TitleScreen,
                    TitleScreenOverride = TitleScreenExpansion.ARealmReborn
                }
            });

            AddPreset(new()
            {
                Name = "Heavensward",
                Tooltip = "Vanilla Heavensward title screen",
                FileName = "?/Heavensward.json",
                LocationModel = new()
                {
                    LocationType = LocationType.TitleScreen,
                    TitleScreenOverride = TitleScreenExpansion.Heavensward
                }
            });

            AddPreset(new()
            {
                Name = "Stormblood",
                Tooltip = "Vanilla Stormblood title screen",
                FileName = "?/Stormblood.json",
                LocationModel = new()
                {
                    LocationType = LocationType.TitleScreen,
                    TitleScreenOverride = TitleScreenExpansion.Stormblood
                }
            });

            AddPreset(new()
            {
                Name = "Shadowbringers",
                Tooltip = "Vanilla Shadowbringers title screen",
                FileName = "?/Shadowbringers.json",
                LocationModel = new()
                {
                    LocationType = LocationType.TitleScreen,
                    TitleScreenOverride = TitleScreenExpansion.Shadowbringers
                }
            });

            AddPreset(new()
            {
                Name = "Endwalker",
                Tooltip = "Vanilla Endwalker title screen",
                FileName = "?/Endwalker.json",
                LocationModel = new()
                {
                    LocationType = LocationType.TitleScreen,
                    TitleScreenOverride = TitleScreenExpansion.Endwalker
                }
            });

            AddPreset(new()
            {
                Name = "Dawntrail",
                Tooltip = "Vanilla Dawntrail title screen",
                FileName = "?/Dawntrail.json",
                LocationModel = new()
                {
                    LocationType = LocationType.TitleScreen,
                    TitleScreenOverride = TitleScreenExpansion.Dawntrail
                }
            });

            // Character select
            AddPreset(new()
            {
                Name = "Atherial Sea",
                Tooltip = "Vanilla Atherial Sea character select screen",
                FileName = "?/AetherialSea.json",
                LocationModel = new()
                {
                    LocationType = LocationType.CharacterSelect,
                    TerritoryPath = "ffxiv/zon_z1/chr/z1c1/level/z1c1",
                    Position = Vector3.Zero,
                    Rotation = 0,
                    WeatherId = 2,
                    TimeOffset = 0,
                    BgmPath = "music/ffxiv/BGM_System_Chara.scd",
                }
            });

        }

        private void AddPreset(PresetModel preset)
        {
            presets.Add(preset.FileName, preset);
        }
    }
}
