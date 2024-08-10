using CharacterSelectBackgroundPlugin.Data.Character;
using CharacterSelectBackgroundPlugin.Data.Persistence;
using CharacterSelectBackgroundPlugin.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace CharacterSelectBackgroundPlugin.PluginServices.Lobby
{
    public unsafe partial class LobbyService
    {

        private LocationModel locationModel;

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
            else if (displayOption.Type == CharacterDisplayType.AetherialSea)
            {
                model = LocationService.DefaultLocation;
            }
            else
            {
                if (displayOption.PresetPath != null && Services.PresetService.TryGetPreset(displayOption.PresetPath, out var preset, LocationType.CharacterSelect))
                {
                    model = preset.LocationModel;
                    if (preset.LastLocationMount)
                    {
                        locationModel.Mount = Services.LocationService.GetLocationModel(contentId).Mount;
                    }
                    locationModel.CameraFollowMode = preset.CameraFollowMode;
                }
                else
                {
                    Services.Log.Error($"Preset \"{displayOption.PresetPath}\" not found");
                    model = LocationService.DefaultLocation;
                }
            }

            model.Position = OffsetPosition(model.Position);
            return model;
        }

        private LocationModel GetNothingSelectedLocation()
        {
            var displayOption = Services.ConfigurationService.NoCharacterDisplayType;
            LocationModel model;
            if (displayOption.Type == CharacterDisplayType.AetherialSea || displayOption.Type == CharacterDisplayType.LastLocation)
            {
                model = LocationService.DefaultLocation;
            }
            else
            {
                if (displayOption.PresetPath != null && Services.PresetService.TryGetPreset(displayOption.PresetPath, out var preset, LocationType.CharacterSelect))
                {
                    model = preset.LocationModel;
                    locationModel.CameraFollowMode = preset.CameraFollowMode;
                }
                else
                {
                    Services.Log.Error($"Preset \"{displayOption.PresetPath}\" not found");
                    model = LocationService.DefaultLocation;
                }
            }
            model.Position = OffsetPosition(model.Position);
            return model;
        }


        public void Apply(PresetModel preset)
        {
            var agentLobby = AgentLobby.Instance();
            locationModel = preset.LocationModel;
            locationModel.CameraFollowMode = preset.CameraFollowMode;
            locationModel.Position = OffsetPosition(locationModel.Position);
            Services.Log.Debug("Applying location model");
            if (CurrentCharacter != null && agentLobby != null)
            {
                var contentId = agentLobby->LobbyData.CharaSelectEntries[agentLobby->HoveredCharacterIndex].Value->ContentId;
                if (preset.LastLocationMount)
                {
                    locationModel.Mount = Services.LocationService.GetLocationModel(contentId).Mount;
                }
                Services.Log.Debug($"Setting character postion {(nint)CurrentCharacter:X}");
                CurrentCharacter->GameObject.SetPosition(locationModel.Position.X, locationModel.Position.Y, locationModel.Position.Z);
                ((CharacterExpanded*)CurrentCharacter)->MovementMode = locationModel.MovementMode;

                if (CurrentCharacter->Mount.MountId != locationModel.Mount.MountId)
                {
                    SetupMount(CurrentCharacter, locationModel);
                }


            }
            resetScene = true;
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
