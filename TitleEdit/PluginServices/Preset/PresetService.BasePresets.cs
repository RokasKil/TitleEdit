using System;
using System.Numerics;
using TitleEdit.Data.Lobby;
using TitleEdit.Data.Persistence;
using TitleEdit.Utility;

namespace TitleEdit.PluginServices.Preset
{
    public partial class PresetService
    {
        private void LoadBasePresets()
        {
            // Title screens
            AddVanillaTitleScreenPresets();
            AddTitleScreenPresets();

            //Character select
            AddVanillaCharacterSelectPresets();
            AddCharacterSelectPresets();
        }

        private void AddVanillaTitleScreenPresets()
        {
            AddPreset(new()
            {
                Name = "A Realm Reborn",
                Tooltip = "Vanilla A Realm Reborn title screen",
                FileName = "?/ARealmReborn.json",
                LocationModel = new()
                {
                    LocationType = LocationType.TitleScreen,
                    TitleScreenOverride = TitleScreenExpansion.ARealmReborn,
                    ToastNotificationText = "Now displaying: A Realm Reborn",
                    TitleScreenLogo = TitleScreenLogo.ARealmReborn,
                    UiColor = UiColors.ARealmReborn
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
                    TitleScreenOverride = TitleScreenExpansion.Heavensward,
                    TitleScreenLogo = TitleScreenLogo.Heavensward,
                    UiColor = UiColors.Heavensward
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
                    TitleScreenOverride = TitleScreenExpansion.Stormblood,
                    TitleScreenLogo = TitleScreenLogo.Stormblood,
                    UiColor = UiColors.Stormblood
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
                    TitleScreenOverride = TitleScreenExpansion.Shadowbringers,
                    TitleScreenLogo = TitleScreenLogo.Shadowbringers,
                    UiColor = UiColors.Shadowbringers
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
                    TitleScreenOverride = TitleScreenExpansion.Endwalker,
                    TitleScreenLogo = TitleScreenLogo.Endwalker,
                    UiColor = UiColors.Endwalker
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
                    TitleScreenOverride = TitleScreenExpansion.Dawntrail,
                    TitleScreenLogo = TitleScreenLogo.Dawntrail,
                    UiColor = UiColors.Dawntrail
                }
            });
        }

        private void AddTitleScreenPresets()
        {

            AddPreset(new()
            {
                Name = "TE_Amaurot 2",
                FileName = "?/TE_Amaurot 2.json",
                LocationModel = new()
                {
                    LocationType = LocationType.TitleScreen,
                    TitleScreenLogo = TitleScreenLogo.Shadowbringers,
                    TerritoryPath = "ex3/01_nvt_n4/dun/n4d6/level/n4d6",
                    TerritoryTypeId = 838,
                    CameraPosition = new(-0.38227558f, -491.87146f, 686.6074f),
                    Yaw = 3.1415927f,
                    Roll = 0.0f,
                    Pitch = -0.42794815f,
                    Fov = 45.0f,
                    WeatherId = 120,
                    TimeOffset = 0,
                    BgmId = 692,
                    BgmPath = "music/ex3/BGM_EX3_Dan_D06.scd",
                    SaveLayout = false,
                    SaveFestivals = false,
                    UiColor = UiColors.Dawntrail
                }
            });
            AddPreset(new()
            {
                Name = "TE_Amaurot 3",
                FileName = "?/TE_Amaurot 3.json",
                LocationModel = new()
                {
                    LocationType = LocationType.TitleScreen,
                    TitleScreenLogo = TitleScreenLogo.None,
                    TerritoryPath = "ex3/01_nvt_n4/dun/n4d6/level/n4d6",
                    TerritoryTypeId = 838,
                    CameraPosition = new(-174.61586f, -704.9572f, -55.22666f),
                    Yaw = -0.84668314f,
                    Roll = 0.0f,
                    Pitch = 0.40988576f,
                    Fov = 45.0f,
                    WeatherId = 2,
                    TimeOffset = 0,
                    BgmId = 647,
                    BgmPath = "music/ex3/BGM_EX3_Event_02.scd",
                    SaveLayout = false,
                    SaveFestivals = false,
                    UiColor = UiColors.Dawntrail
                }
            });
            AddPreset(new()
            {
                Name = "TE_Amaurot 1",
                FileName = "?/TE_Amaurot 1.json",
                LocationModel = new()
                {
                    LocationType = LocationType.TitleScreen,
                    TitleScreenLogo = TitleScreenLogo.Shadowbringers,
                    TerritoryPath = "ex3/01_nvt_n4/fld/n4f6/level/n4f6",
                    TerritoryTypeId = 818,
                    CameraPosition = new(22.773487f, -61.84117f, 757.2081f),
                    Yaw = -1.1805015f,
                    Roll = 0.0f,
                    Pitch = 0.29725394f,
                    Fov = 45.0f,
                    WeatherId = 1,
                    TimeOffset = 0,
                    BgmId = 0,
                    BgmPath = "music/ffxiv/orchestrion/bgm_orch_430.scd",
                    SaveLayout = false,
                    SaveFestivals = false,
                    UiColor = UiColors.Dawntrail
                }
            });
            AddPreset(new()
            {
                Name = "TEE_AzysLla",
                FileName = "?/TEE_AzysLla.json",
                LocationModel = new()
                {
                    LocationType = LocationType.TitleScreen,
                    TitleScreenLogo = TitleScreenLogo.None,
                    TerritoryPath = "ex1/03_abr_a2/fld/a2f2/level/a2f2",
                    TerritoryTypeId = 402,
                    CameraPosition = new(-582.60913f, 50.544624f, -251.91136f),
                    Yaw = 0.9263441f,
                    Roll = 0.0f,
                    Pitch = 0.49396738f,
                    Fov = 1.0f,
                    WeatherId = 54,
                    TimeOffset = 0,
                    BgmId = 385,
                    BgmPath = "music/ex1/BGM_EX1_Boss04.scd",
                    SaveLayout = false,
                    SaveFestivals = false,
                    UiColor = UiColors.Dawntrail
                }
            });
            AddPreset(new()
            {
                Name = "TE_Carrotorium",
                FileName = "?/TE_Carrotorium.json",
                LocationModel = new()
                {
                    LocationType = LocationType.TitleScreen,
                    TitleScreenLogo = TitleScreenLogo.Endwalker,
                    TerritoryPath = "ex4/04_uvs_u5/fld/u5f1/level/u5f1",
                    TerritoryTypeId = 959,
                    CameraPosition = new(-510.2475f, -150.07487f, -522.2673f),
                    Yaw = -1.8643512f,
                    Roll = 0.0f,
                    Pitch = -0.8711339f,
                    Fov = 1.0f,
                    WeatherId = 2,
                    TimeOffset = 0,
                    BgmId = 880,
                    BgmPath = "music/ex4/BGM_EX4_Dan_D07.scd",
                    SaveLayout = false,
                    SaveFestivals = false,
                    UiColor = UiColors.Dawntrail
                }
            });
            AddPreset(new()
            {
                Name = "TE_Central Shroud",
                FileName = "?/TE_Central Shroud.json",
                LocationModel = new()
                {
                    LocationType = LocationType.TitleScreen,
                    TitleScreenLogo = TitleScreenLogo.ARealmReborn,
                    TerritoryPath = "ffxiv/fst_f1/fld/f1f1/level/f1f1",
                    TerritoryTypeId = 148,
                    CameraPosition = new(189.35226f, -29.25525f, 375.48474f),
                    Yaw = 1.5353248f,
                    Roll = 0.0f,
                    Pitch = 0.081796646f,
                    Fov = 45.0f,
                    WeatherId = 1,
                    TimeOffset = 640,
                    BgmId = 0,
                    BgmPath = "music/ffxiv/orchestrion/bgm_orch_391.scd",
                    SaveLayout = false,
                    SaveFestivals = false,
                    UiColor = UiColors.Dawntrail
                }
            });
            AddPreset(new()
            {
                Name = "TE_Dalamud 2",
                FileName = "?/TE_Dalamud 2.json",
                LocationModel = new()
                {
                    LocationType = LocationType.TitleScreen,
                    TitleScreenLogo = TitleScreenLogo.None,
                    TerritoryPath = "ffxiv/wil_w1/twn/w1t1/level/w1t1",
                    TerritoryTypeId = 130,
                    CameraPosition = new(-124.29569f, 20.383795f, -96.238174f),
                    Yaw = 1.9675864f,
                    Roll = 0.0f,
                    Pitch = 0.08992102f,
                    Fov = 45.0f,
                    WeatherId = 101,
                    TimeOffset = 800,
                    BgmId = 237,
                    BgmPath = "music/ffxiv/BGM_Con_Bahamut_Bigboss2.scd",
                    SaveLayout = false,
                    SaveFestivals = false,
                    UiColor = UiColors.Dawntrail
                }
            });
            AddPreset(new()
            {
                Name = "TE_Dalamud",
                FileName = "?/TE_Dalamud.json",
                LocationModel = new()
                {
                    LocationType = LocationType.TitleScreen,
                    TitleScreenLogo = TitleScreenLogo.ARealmReborn,
                    TerritoryPath = "ex3/01_nvt_n4/fld/n4fe/level/n4fe",
                    TerritoryTypeId = 897,
                    CameraPosition = new(192.5f, 23.0f, 128.5f),
                    Yaw = 2.5567405f,
                    Roll = 0.0f,
                    Pitch = 0.3920805f,
                    Fov = 45.0f,
                    WeatherId = 101,
                    TimeOffset = 0,
                    BgmId = 198,
                    BgmPath = "music/ffxiv/BGM_Con_Neal.scd",
                    SaveLayout = false,
                    SaveFestivals = false,
                    UiColor = UiColors.Dawntrail
                }
            });
            AddPreset(new()
            {
                Name = "TE_Don't Ask",
                FileName = "?/TE_Don't Ask.json",
                LocationModel = new()
                {
                    LocationType = LocationType.TitleScreen,
                    TitleScreenLogo = TitleScreenLogo.None,
                    TerritoryPath = "ffxiv/wil_w1/twn/w1tz/level/w1tz",
                    TerritoryTypeId = 796,
                    CameraPosition = new(70.45863f, 12.015016f, 149.7881f),
                    Yaw = -0.9926575f,
                    Roll = 0.0f,
                    Pitch = 0.12776367f,
                    Fov = 45.0f,
                    WeatherId = 2,
                    TimeOffset = 1100,
                    BgmId = 538,
                    BgmPath = "music/ffxiv/BGM_Minigame_Egghunt.scd",
                    SaveLayout = false,
                    SaveFestivals = false,
                    UiColor = UiColors.Dawntrail
                }
            });
            AddPreset(new()
            {
                Name = "TE_Garlemald",
                FileName = "?/TE_Garlemald.json",
                LocationModel = new()
                {
                    LocationType = LocationType.TitleScreen,
                    TitleScreenLogo = TitleScreenLogo.Shadowbringers,
                    TerritoryPath = "ex3/01_nvt_n4/evt/n4ed/level/n4ed",
                    TerritoryTypeId = 0,
                    CameraPosition = new(-472.1514f, 280.8405f, -953.2695f),
                    Yaw = 1.0745571f,
                    Roll = 0.0f,
                    Pitch = 0.4948496f,
                    Fov = 1.0f,
                    WeatherId = 2,
                    TimeOffset = 2105,
                    BgmId = 761,
                    BgmPath = "music/ex3/BGM_EX3_Event_23.scd",
                    SaveLayout = false,
                    SaveFestivals = false,
                    UiColor = UiColors.Dawntrail
                }
            });
            AddPreset(new()
            {
                Name = "TE_Hell's Kier",
                FileName = "?/TE_Hell's Kier.json",
                LocationModel = new()
                {
                    LocationType = LocationType.TitleScreen,
                    TitleScreenLogo = TitleScreenLogo.Stormblood,
                    TerritoryPath = "ex2/02_est_e3/fld/e3fe/level/e3fe",
                    TerritoryTypeId = 810,
                    CameraPosition = new(184.04657f, -39.467655f, 54.550262f),
                    Yaw = -1.3519993f,
                    Roll = 0.0f,
                    Pitch = 0.1990427f,
                    Fov = 1.24f,
                    WeatherId = 104,
                    TimeOffset = 0,
                    BgmId = 582,
                    BgmPath = "music/ex2/BGM_EX2_Ban_25.scd",
                    SaveLayout = false,
                    SaveFestivals = false,
                    UiColor = UiColors.Dawntrail
                }
            });
            AddPreset(new()
            {
                Name = "TE_Ishgard",
                FileName = "?/TE_Ishgard.json",
                LocationModel = new()
                {
                    LocationType = LocationType.TitleScreen,
                    TitleScreenLogo = TitleScreenLogo.Heavensward,
                    TerritoryPath = "ex1/01_roc_r2/twn/r2t1/level/r2t1",
                    TerritoryTypeId = 418,
                    CameraPosition = new(-251.92082f, 8.874063f, 166.83122f),
                    Yaw = 2.3122256f,
                    Roll = 0.0f,
                    Pitch = 0.41671044f,
                    Fov = 45.0f,
                    WeatherId = 15,
                    TimeOffset = 1985,
                    BgmId = 318,
                    BgmPath = "music/ex1/BGM_EX1_Event_Start.scd",
                    SaveLayout = false,
                    SaveFestivals = false,
                    UiColor = UiColors.Dawntrail
                }
            });
            AddPreset(new()
            {
                Name = "TE_Kozama'uka - Midday",
                FileName = "?/TE_Kozama'uka - Midday.json",
                Author = "bevral",
                LocationModel = new()
                {
                    LocationType = LocationType.TitleScreen,
                    TitleScreenLogo = TitleScreenLogo.Dawntrail,
                    TerritoryPath = "ex5/02_ykt_y6/fld/y6f2/level/y6f2",
                    TerritoryTypeId = 1188,
                    CameraPosition = new(-508.61078f, 65.44885f, -416.0996f),
                    Yaw = 1.1428189f,
                    Roll = 0.0f,
                    Pitch = 0.09602679f,
                    Fov = 1.08f,
                    WeatherId = 2,
                    TimeOffset = 1100,
                    BgmId = 20063,
                    BgmPath = "music/ex5/BGM_EX5_Field_Safe02.scd",
                    SaveLayout = false,
                    SaveFestivals = false,
                    UiColor = UiColors.Dawntrail
                }
            });
            AddPreset(new()
            {
                Name = "TE_Limsa Lominsa",
                FileName = "?/TE_Limsa Lominsa.json",
                LocationModel = new()
                {
                    LocationType = LocationType.TitleScreen,
                    TitleScreenLogo = TitleScreenLogo.ARealmReborn,
                    TerritoryPath = "ffxiv/sea_s1/twn/s1t1/level/s1t1",
                    TerritoryTypeId = 128,
                    CameraPosition = new(25.326021f, 48.90745f, 152.9382f),
                    Yaw = 0.36621737f,
                    Roll = 0.0f,
                    Pitch = 0.047502767f,
                    Fov = 1.0f,
                    WeatherId = 1,
                    TimeOffset = 400,
                    BgmId = 747,
                    BgmPath = "music/ex1/BGM_EX1_Hukko_Finish01.scd",
                    SaveLayout = false,
                    SaveFestivals = false,
                    UiColor = UiColors.Dawntrail
                }
            });
            AddPreset(new()
            {
                Name = "TE_Reisen Temple",
                FileName = "?/TE_Reisen Temple.json",
                LocationModel = new()
                {
                    LocationType = LocationType.TitleScreen,
                    TitleScreenLogo = TitleScreenLogo.None,
                    TerritoryPath = "ex2/02_est_e3/evt/e3e8/level/e3e8",
                    TerritoryTypeId = 764,
                    CameraPosition = new(-10.635f, -54.55f, -165.71f),
                    Yaw = 2.8003855f,
                    Roll = 0.0f,
                    Pitch = 0.008002617f,
                    Fov = 45.0f,
                    WeatherId = 2,
                    TimeOffset = 0,
                    BgmId = 519,
                    BgmPath = "music/ex2/BGM_EX2_Field_Iroha.scd",
                    SaveLayout = false,
                    SaveFestivals = false,
                    UiColor = UiColors.Dawntrail
                }
            });
            AddPreset(new()
            {
                Name = "TE_Shadowbringers 2",
                FileName = "?/TE_Shadowbringers 2.json",
                LocationModel = new()
                {
                    LocationType = LocationType.TitleScreen,
                    TitleScreenLogo = TitleScreenLogo.Shadowbringers,
                    TerritoryPath = "ex3/01_nvt_n4/twn/n4t1/level/n4t1",
                    TerritoryTypeId = 819,
                    CameraPosition = new(-508.77274f, 0.96345747f, 74.34585f),
                    Yaw = 1.6631284f,
                    Roll = 0.0f,
                    Pitch = 0.3642672f,
                    Fov = 45.0f,
                    WeatherId = 118,
                    TimeOffset = 0,
                    BgmId = 705,
                    BgmPath = "music/ex3/BGM_EX3_EndCredit01.scd",
                    SaveLayout = false,
                    SaveFestivals = false,
                    UiColor = UiColors.Dawntrail
                }
            });
            AddPreset(new()
            {
                Name = "TE_Titania's Forest",
                FileName = "?/TE_Titania's Forest.json",
                LocationModel = new()
                {
                    LocationType = LocationType.TitleScreen,
                    TitleScreenLogo = TitleScreenLogo.Shadowbringers,
                    TerritoryPath = "ex3/01_nvt_n4/fld/n4fa/level/n4fa",
                    TerritoryTypeId = 354,
                    CameraPosition = new(68.31682f, 7.3394575f, 136.57358f),
                    Yaw = -2.013775f,
                    Roll = 0.0f,
                    Pitch = 0.22415328f,
                    Fov = 45.0f,
                    WeatherId = 123,
                    TimeOffset = 0,
                    BgmId = 700,
                    BgmPath = "music/ffxiv/BGM_Ride_Titania.scd",
                    SaveLayout = false,
                    SaveFestivals = false,
                    UiColor = UiColors.Dawntrail
                }
            });
            AddPreset(new()
            {
                Name = "TE_Tuliyollal Night",
                FileName = "?/TE_Tuliyollal Night.json",
                Author = "alyssile",
                LocationModel = new()
                {
                    LocationType = LocationType.TitleScreen,
                    TitleScreenLogo = TitleScreenLogo.Dawntrail,
                    TerritoryPath = "ex5/02_ykt_y6/twn/y6t1/level/y6t1",
                    TerritoryTypeId = 1185,
                    CameraPosition = new(130.34209f, -16.301905f, 172.8012f),
                    Yaw = -2.5165493f,
                    Roll = 0.0f,
                    Pitch = 0.14567117f,
                    Fov = 1.0f,
                    WeatherId = 1,
                    TimeOffset = 2200,
                    BgmId = 20046,
                    BgmPath = "music/ex5/BGM_EX5_Town_T_Night.scd",
                    SaveLayout = false,
                    SaveFestivals = false,
                    UiColor = UiColors.Dawntrail
                }
            });
            AddPreset(new()
            {
                Name = "TE_Werlyt",
                FileName = "?/TE_Werlyt.json",
                LocationModel = new()
                {
                    LocationType = LocationType.TitleScreen,
                    TitleScreenLogo = TitleScreenLogo.None,
                    TerritoryPath = "ex3/01_nvt_n4/fld/n4fg/level/n4fg",
                    TerritoryTypeId = 934,
                    CameraPosition = new(78.44922f, 17.70825f, 101.83251f),
                    Yaw = -2.9742289f,
                    Roll = 0.0f,
                    Pitch = -0.006300778f,
                    Fov = 45.0f,
                    WeatherId = 105,
                    TimeOffset = 0,
                    BgmId = 767,
                    BgmPath = "music/ex3/BGM_EX3_Field_Welrit.scd",
                    SaveLayout = false,
                    SaveFestivals = false,
                    UiColor = UiColors.Dawntrail
                }
            });
        }

        private void AddVanillaCharacterSelectPresets()
        {
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
                    ToastNotificationText = "Now displaying: Atherial Sea"
                }
            });
        }

        private void AddCharacterSelectPresets()
        {
            AddPreset(new()
            {
                Name = "TE_Candy Store",
                FileName = "?/TE_Candy Store.json",
                Author = "Thorian",
                LocationModel = new()
                {
                    LocationType = LocationType.CharacterSelect,
                    TerritoryPath = "ex3/01_nvt_n4/evt/n4e9/level/n4e9",
                    TerritoryTypeId = 892,
                    LayoutTerritoryTypeId = 892,
                    LayoutLayerFilterKey = 0,
                    Position = new(77.35472f, 0.15887594f, 50.74012f),
                    Rotation = -0.7853982f,
                    WeatherId = 2,
                    TimeOffset = 1228,
                    BgmId = 719,
                    BgmPath = "music/ex3/BGM_EX3_BanFort_Fairy_Good.scd",
                    SaveLayout = false,
                    SaveFestivals = false
                }
            });
            AddPreset(new()
            {
                Name = "TE_Convocation",
                FileName = "?/TE_Convocation.json",
                Author = "bot_",
                LocationModel = new()
                {
                    LocationType = LocationType.CharacterSelect,
                    TerritoryPath = "ffxiv/zon_z1/evt/z1e7/level/z1e7",
                    TerritoryTypeId = 0,
                    LayoutTerritoryTypeId = 0,
                    LayoutLayerFilterKey = 0,
                    Position = new(0.0f, 0.0f, -0.3f),
                    Rotation = 0.0f,
                    WeatherId = 2,
                    TimeOffset = 1200,
                    BgmId = 58,
                    BgmPath = "music/ffxiv/BGM_Field_Danger2.scd",
                    SaveLayout = false,
                    SaveFestivals = false
                }
            });
            AddPreset(new()
            {
                Name = "TE_Elysion",
                FileName = "?/TE_Elysion.json",
                Author = "Speedas",
                LocationModel = new()
                {
                    LocationType = LocationType.CharacterSelect,
                    TerritoryPath = "ex4/04_uvs_u5/evt/u5e3/level/u5e3",
                    TerritoryTypeId = 1073,
                    LayoutTerritoryTypeId = 0,
                    LayoutLayerFilterKey = 268557,
                    Position = new(91.0f, 497.26218f, -8.9f),
                    Rotation = -1.5707964f,
                    WeatherId = 2,
                    TimeOffset = 2205,
                    BgmId = 929,
                    BgmPath = "music/ex4/BGM_EX4_BanFort_Omi_Good.scd",
                    SaveLayout = false,
                    SaveFestivals = false
                }
            });
            AddPreset(new()
            {
                Name = "TE_Jungle",
                FileName = "?/TE_Jungle.json",
                Author = "Thorian",
                LocationModel = new()
                {
                    LocationType = LocationType.CharacterSelect,
                    TerritoryPath = "ffxiv/sea_s1/dun/s1d5/level/s1d5",
                    TerritoryTypeId = 361,
                    LayoutTerritoryTypeId = 0,
                    LayoutLayerFilterKey = 0,
                    Position = new(263.6f, 63.237f, 160.25f),
                    Rotation = 3.1415927f,
                    WeatherId = 2,
                    TimeOffset = 1126,
                    BgmId = 742,
                    BgmPath = "music/ex3/BGM_EX3_Event_20.scd",
                    SaveLayout = false,
                    SaveFestivals = false
                }
            });
            AddPreset(new()
            {
                Name = "TE_Nightbloom",
                FileName = "?/TE_Nightbloom.json",
                Author = "Kamgigari",
                LocationModel = new()
                {
                    LocationType = LocationType.CharacterSelect,
                    TerritoryPath = "ex2/02_est_e3/fld/e3fd/level/e3fd",
                    TerritoryTypeId = 778,
                    LayoutTerritoryTypeId = 0,
                    LayoutLayerFilterKey = 0,
                    Position = new(100.0f, 0.0f, 100.0f),
                    Rotation = 0.0f,
                    WeatherId = 99,
                    TimeOffset = 0,
                    BgmId = 542,
                    BgmPath = "music/ex2/BGM_EX2_Ban_17.scd",
                    SaveLayout = false,
                    SaveFestivals = false
                }
            });
            AddPreset(new()
            {
                Name = "TE_The Dancing Plague",
                FileName = "?/TE_The Dancing Plague.json",
                Author = "Speedas",
                LocationModel = new()
                {
                    LocationType = LocationType.CharacterSelect,
                    TerritoryPath = "ex3/01_nvt_n4/fld/n4fa/level/n4fa",
                    TerritoryTypeId = 845,
                    LayoutTerritoryTypeId = 845,
                    LayoutLayerFilterKey = 0,
                    Position = new(100.0f, 0.0f, 100.0f),
                    Rotation = 0.0f,
                    WeatherId = 122,
                    TimeOffset = 0,
                    BgmId = 683,
                    BgmPath = "music/ex3/BGM_EX3_Ban_01.scd",
                    SaveLayout = false,
                    SaveFestivals = false
                }
            });
            AddPreset(new()
            {
                Name = "TE_Zero Domain",
                FileName = "?/TE_Zero Domain.json",
                Author = "bot_",
                LocationModel = new()
                {
                    LocationType = LocationType.CharacterSelect,
                    TerritoryPath = "ex4/05_zon_z5/evt/z5e4/level/z5e4",
                    TerritoryTypeId = 1077,
                    LayoutTerritoryTypeId = 0,
                    LayoutLayerFilterKey = 0,
                    Position = new(0.0f, -0.393f, -20.05f),
                    Rotation = 3.1415927f,
                    WeatherId = 171,
                    TimeOffset = 716,
                    BgmId = 928,
                    BgmPath = "music/ex4/BGM_EX4_Event_28.scd",
                    SaveLayout = false,
                    SaveFestivals = false
                }
            });
        }

        private void AddPreset(PresetModel preset)
        {
            try
            {
                Validate(preset);
            }
            catch (Exception e)
            {
                Services.Log.Warning($"Failed to base preset '{preset.FileName}' with error: {e.Message}");
                return;
            }
            presets.Add(preset.FileName, preset);
        }
    }
}
