using TitleEdit.Data.BGM;
using TitleEdit.Data.Character;
using TitleEdit.Data.Lobby;
using TitleEdit.Data.Persistence;
using TitleEdit.Extensions;
using TitleEdit.Utility;

namespace TitleEdit.PluginServices.Lobby
{
    public unsafe partial class LobbyService
    {

        private bool liveEditTitleScreen = false;
        private bool liveEditTitleScreenLoaded = false;
        private LocationModel? liveEditCharacterSelectLocationModel;
        private bool ShouldLiveEditTitleScreen => liveEditTitleScreenLoaded && CurrentLobbyMap == GameLobbyType.Title;

        private bool liveEditCharacterSelect = false;
        private bool liveEditCharacterSelectLoaded = false;
        private LocationModel? liveEditTitleScreenLocationModel;
        private bool ShouldLiveEditCharacterSelect => liveEditCharacterSelectLoaded && CurrentLobbyMap == GameLobbyType.CharaSelect;

        public void StartLiveEdit(PresetModel preset)
        {
            Services.Log.Debug($"[StartLiveEdit] {preset.Name}; {preset.Author}; {preset.LocationModel.LocationType}; {preset.FileName}");
            UpdateLiveEditPreset(preset);
            if (preset.LocationModel.LocationType == LocationType.TitleScreen)
            {
                liveEditTitleScreen = true;
                liveEditTitleScreenLoaded = false;
                ReloadTitleScreen();
            }
            else if (preset.LocationModel.LocationType == LocationType.CharacterSelect)
            {
                liveEditCharacterSelect = true;
                liveEditCharacterSelectLoaded = false;
                ReloadCharacterSelect();
            }
        }

        public void UpdateLiveEditPreset(PresetModel preset)
        {
            // Services.Log.Debug($"[UpdateLiveEditPreset] {preset.Name}; {preset.Author}; {preset.LocationModel.LocationType}; {preset.FileName}");
            preset.LocationModel.ToastNotificationText = $"Live editing a {preset.LocationModel.LocationType.ToText()} preset";
            if (preset.LocationModel.LocationType == LocationType.TitleScreen)
            {
                liveEditTitleScreenLocationModel = preset.LocationModel;
                if (liveEditTitleScreenLoaded)
                {
                    titleScreenLocationModel = liveEditTitleScreenLocationModel.Value;
                }
            }
            else if (preset.LocationModel.LocationType == LocationType.CharacterSelect)
            {
                preset.LocationModel.CameraFollowMode = preset.CameraFollowMode;

                liveEditCharacterSelectLocationModel = preset.LocationModel;
                if (liveEditCharacterSelectLoaded)
                {
                    characterSelectLocationModel = liveEditCharacterSelectLocationModel.Value;
                }
            }
        }

        public void UpdateLiveEditTime(LocationType locationType)
        {
            if (locationType == LocationType.TitleScreen && ShouldLiveEditTitleScreen)
            {
                SetTime(titleScreenLocationModel.TimeOffset);
            }
            else if (locationType == LocationType.CharacterSelect && ShouldLiveEditCharacterSelect)
            {
                SetTime(characterSelectLocationModel.TimeOffset);

            }
        }

        public void UpdateLiveEditWeather(LocationType locationType)
        {
            if (locationType == LocationType.TitleScreen && ShouldLiveEditTitleScreen)
            {
                Services.WeatherService.WeatherId = titleScreenLocationModel.WeatherId;
            }
            else if (locationType == LocationType.CharacterSelect && ShouldLiveEditCharacterSelect)
            {
                Services.WeatherService.WeatherId = characterSelectLocationModel.WeatherId;

            }
        }

        public void UpdateLiveEditBgm(LocationType locationType)
        {
            if ((locationType == LocationType.TitleScreen && ShouldLiveEditTitleScreen) ||
                (locationType == LocationType.CharacterSelect && ShouldLiveEditCharacterSelect))
            {
                ForcePlaySongIndex(LobbyInfo->CurrentLobbyMusicIndex != LobbySong.None ? LobbyInfo->CurrentLobbyMusicIndex : LobbySong.CharacterSelect);
            }
        }

        public void UpdateLiveEditCharacterPosition()
        {
            if (ShouldLiveEditCharacterSelect && CurrentCharacter != null)
            {
                CurrentCharacter->GameObject.SetPosition(characterSelectLocationModel.Position);
            }
        }

        public void UpdateLiveEditCharacterRotation()
        {
            if (ShouldLiveEditCharacterSelect && CurrentCharacter != null)
            {
                CurrentCharacter->GameObject.SetRotation(characterSelectLocationModel.Rotation);
            }
        }

        public void UpdateLiveEditCharacterMount()
        {
            if (ShouldLiveEditCharacterSelect && CurrentCharacter != null)
            {
                SetupMount(CurrentCharacter, CurrentContentId, characterSelectLocationModel);
            }
        }

        public void UpdateLiveEditCharacterState()
        {
            if (ShouldLiveEditCharacterSelect && CurrentCharacter != null)
            {
                ((CharacterExpanded*)CurrentCharacter)->MovementMode = characterSelectLocationModel.MovementMode;
            }
        }


        public void StopLiveEdit(LocationType locationType)
        {
            Services.Log.Debug($"[StopLiveEdit] {locationType}");
            if (locationType == LocationType.TitleScreen)
            {
                liveEditTitleScreen = false;
                liveEditTitleScreenLoaded = false;
                liveEditTitleScreenLocationModel = null;
                ReloadTitleScreen();
            }
            else if (locationType == LocationType.CharacterSelect)
            {
                liveEditCharacterSelect = false;
                liveEditCharacterSelectLoaded = false;
                liveEditCharacterSelectLocationModel = null;
                ReloadCharacterSelect();
            }

        }
    }
}
