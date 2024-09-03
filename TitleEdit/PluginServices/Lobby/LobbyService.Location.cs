using FFXIVClientStructs.FFXIV.Common.Math;
using System;
using System.Linq;
using TitleEdit.Data.Character;
using TitleEdit.Data.Persistence;
using TitleEdit.Utility;

namespace TitleEdit.PluginServices.Lobby
{
    public unsafe partial class LobbyService
    {

        private LocationModel characterSelectLocationModel;

        private LocationModel titleScreenLocationModel;

        private Random random = new();

        private LocationModel GetLocationForContentId(ulong contentId)
        {
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
                if (!Services.LocationService.Locations.TryGetValue(contentId, out model))
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

            model.Position = OffsetPosition(model.Position);
            return model;
        }

        private LocationModel GetNothingSelectedLocation()
        {
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
            model.Position = OffsetPosition(model.Position);
            return model;
        }

        private LocationModel GetTitleLocation()
        {
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

        // TODO: Rework
        public void Apply(PresetModel preset)
        {
            characterSelectLocationModel = preset.LocationModel;
            characterSelectLocationModel.CameraFollowMode = preset.CameraFollowMode;
            characterSelectLocationModel.Position = OffsetPosition(characterSelectLocationModel.Position);
            Services.Log.Debug("Applying location model");
            if (CurrentCharacter != null)
            {
                var contentId = AgentLobby->LobbyData.CharaSelectEntries[AgentLobby->HoveredCharacterIndex].Value->ContentId;
                if (preset.LastLocationMount)
                {
                    characterSelectLocationModel.Mount = Services.LocationService.GetLocationModel(contentId).Mount;
                }
                Services.Log.Debug($"Setting character postion {(nint)CurrentCharacter:X}");
                CurrentCharacter->GameObject.SetPosition(characterSelectLocationModel.Position.X, characterSelectLocationModel.Position.Y, characterSelectLocationModel.Position.Z);
                ((CharacterExpanded*)CurrentCharacter)->MovementMode = characterSelectLocationModel.MovementMode;

                if (CurrentCharacter->Mount.MountId != characterSelectLocationModel.Mount.MountId)
                {
                    SetupMount(CurrentCharacter, characterSelectLocationModel);
                }


            }
            resetScene = true;
        }

        private LocationModel GetGroupLocationModel(string? groupPath, LocationType type)
        {
            LocationModel model;
            if (groupPath != null && Services.GroupService.TryGetGroup(groupPath, out var group, type))
            {
                if (group.PresetFileNames.Count > 0)
                {
                    model = GetPresetLocationModel(group.PresetFileNames[random.Next(group.PresetFileNames.Count)], type);
                }
                else
                {
                    var presets = Services.PresetService.Presets.Where(preset => preset.Value.LocationModel.LocationType == type);
                    model = GetPresetLocationModel(presets.Skip(random.Next(presets.Count())).FirstOrDefault().Key, type);
                }
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
