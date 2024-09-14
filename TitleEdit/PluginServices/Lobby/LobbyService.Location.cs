using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Common.Math;
using System;
using System.Linq;
using TitleEdit.Data.Persistence;
using TitleEdit.Utility;

namespace TitleEdit.PluginServices.Lobby
{
    public unsafe partial class LobbyService
    {

        private LocationModel characterSelectLocationModel;

        private LocationModel titleScreenLocationModel;

        private string? characterSelectGroupModelPath;
        private string? characterSelectGroupPresetPath;

        private Random random = new();


        private void ClearCharacterSelectGroupCache()
        {
            if (characterSelectGroupModelPath != null)
                Services.Log.Debug("[ClearCharacterSelectGroupCache]");
            characterSelectGroupModelPath = null;
            characterSelectGroupPresetPath = null;
        }

        private LocationModel GetLocationForContentId(ulong contentId)
        {
            if (liveEditCharacterSelect)
            {
                if (!liveEditCharacterSelectLoaded)
                {
                    characterSelectLocationModel = liveEditCharacterSelectLocationModel!.Value;
                    liveEditCharacterSelectLoaded = true;
                }
                return characterSelectLocationModel;
            }
            var displayOverrideIdx = Services.ConfigurationService.DisplayTypeOverrides.FindIndex((entry) => entry.Key == contentId);

            CharacterDisplayTypeOption displayOption;
            LocationModel model;

            if (displayOverrideIdx != -1)
            {
                displayOption = Services.ConfigurationService.DisplayTypeOverrides[displayOverrideIdx].Value;
            }
            else
            {
                displayOption = Services.ConfigurationService.GlobalDisplayType;
            }

            if (displayOption.Type == CharacterDisplayType.LastLocation)
            {
                if (Services.LocationService.Locations.TryGetValue(contentId, out model))
                {
                    model.ToastNotificationText = $"Now displaying last recorded location";
                    //Should always have name but just in case
                    if (Services.CharactersService.Characters.TryGetValue(contentId, out var characterName))
                    {
                        model.ToastNotificationText += $" for {characterName}";
                    }
                }
                else
                {
                    model = GetNothingSelectedLocation();
                }
            }
            else if (displayOption.Type == CharacterDisplayType.Preset)
            {
                model = GetPresetLocationModel(displayOption.PresetPath, LocationType.CharacterSelect);
            }
            else if (displayOption.Type == CharacterDisplayType.Random)
            {
                model = GetGroupLocationModel(displayOption.PresetPath, LocationType.CharacterSelect);
            }
            else
            {
                model = Services.PresetService.GetDefaultPreset(LocationType.CharacterSelect).LocationModel;
            }

            if (displayOption.Type != CharacterDisplayType.Random)
            {
                characterSelectGroupModelPath = null;
                characterSelectGroupPresetPath = null;
            }

            model.Position = OffsetPosition(model.Position);
            return model;
        }

        private LocationModel GetNothingSelectedLocation()
        {
            if (liveEditCharacterSelect)
            {
                if (!liveEditCharacterSelectLoaded)
                {
                    characterSelectLocationModel = liveEditCharacterSelectLocationModel!.Value;
                    liveEditCharacterSelectLoaded = true;
                }
                return characterSelectLocationModel;
            }
            var displayOption = Services.ConfigurationService.NoCharacterDisplayType;
            LocationModel model;
            if (displayOption.Type == CharacterDisplayType.Preset)
            {
                model = GetPresetLocationModel(displayOption.PresetPath, LocationType.CharacterSelect);
            }
            else if (displayOption.Type == CharacterDisplayType.Random)
            {
                model = GetGroupLocationModel(displayOption.PresetPath, LocationType.CharacterSelect);
            }
            else
            {
                model = Services.PresetService.GetDefaultPreset(LocationType.CharacterSelect).LocationModel;
            }
            if (displayOption.Type != CharacterDisplayType.Random)
            {
                characterSelectGroupModelPath = null;
                characterSelectGroupPresetPath = null;
            }
            model.Position = OffsetPosition(model.Position);
            return model;
        }

        private LocationModel GetTitleLocation()
        {
            if (liveEditTitleScreen)
            {
                if (!liveEditTitleScreenLoaded)
                {
                    titleScreenLocationModel = liveEditTitleScreenLocationModel!.Value;
                    liveEditTitleScreenLoaded = true;
                }
                return titleScreenLocationModel;
            }
            var displayOption = Services.ConfigurationService.TitleDisplayTypeOption;
            LocationModel model;
            if (displayOption.Type == TitleDisplayType.Preset)
            {
                model = GetPresetLocationModel(displayOption.PresetPath, LocationType.TitleScreen);
            }
            else if (displayOption.Type == TitleDisplayType.Random)
            {
                model = GetGroupLocationModel(displayOption.PresetPath, LocationType.TitleScreen);
            }
            else
            {
                model = Services.PresetService.GetDefaultPreset(LocationType.TitleScreen).LocationModel;
            }
            model.CameraPosition = OffsetPosition(model.CameraPosition);
            return model;
        }

        private LocationModel GetGroupLocationModel(string? groupPath, LocationType type)
        {
            LocationModel model;
            if (groupPath != null && Services.GroupService.TryGetGroup(groupPath, out var group, type))
            {
                string? path;
                // if we have a character select location cached use that
                if (group.PresetFileNames.Count > 0)
                {
                    path = group.PresetFileNames[random.Next(group.PresetFileNames.Count)];
                }
                else
                {
                    var presets = Services.PresetService.Presets.Where(preset => preset.Value.LocationModel.LocationType == type);
                    path = presets.Skip(random.Next(presets.Count())).FirstOrDefault().Key;
                }

                if (type == LocationType.CharacterSelect)
                {
                    if (characterSelectGroupModelPath == groupPath && characterSelectGroupPresetPath != null)
                    {
                        path = characterSelectGroupPresetPath;
                    }
                    else
                    {
                        characterSelectGroupModelPath = groupPath;
                        characterSelectGroupPresetPath = path;
                    }
                }
                model = GetPresetLocationModel(path, type);
            }
            else
            {
                Services.Log.Error($"Group \"{groupPath}\" not found");
                model = Services.PresetService.GetDefaultPreset(type).LocationModel;
            }
            return model;
        }

        private LocationModel GetPresetLocationModel(string? presetPath, LocationType type)
        {
            LocationModel model;
            if (presetPath != null && Services.PresetService.TryGetPreset(presetPath, out var preset, type))
            {
                model = preset.LocationModel;
                characterSelectLocationModel.CameraFollowMode = preset.CameraFollowMode;
                model.ToastNotificationText = $"Now displaying: {preset.Name}";
                if (!preset.Author.IsNullOrEmpty())
                {
                    model.ToastNotificationText += $"\nBy {preset.Author}";
                }
            }
            else
            {
                Services.Log.Error($"Preset \"{presetPath}\" not found");
                model = Services.PresetService.GetDefaultPreset(type).LocationModel;
            }
            return model;
        }

        private Vector3 OffsetPosition(Vector3 position)
        {
            // There is some weird issue when rapidly switching the camera while the world is loading 
            // and the camera focus is at (0,0,0)
            // that causes incredibly loud and persistent noises to start playing
            // we work around that by imperceivably offsetting the Position

            if (position.X == 0) position.X = 0.001f;
            if (position.Y == 0) position.Y = 0.001f;
            if (position.Z == 0) position.Z = 0.001f;
            return position;
        }
    }
}
