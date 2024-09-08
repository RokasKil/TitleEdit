using Dalamud.Hooking;
using Dalamud.Utility;
using Dalamud.Utility.Signatures;
using System;
using TitleEdit.Data.BGM;
using TitleEdit.Data.Lobby;
using TitleEdit.Utility;

namespace TitleEdit.PluginServices.Lobby
{
    public unsafe partial class LobbyService
    {
        [Signature("E8 ?? ?? ?? ?? 33 C9 E8 ?? ?? ?? ?? 48 8B 0D")]
        private readonly delegate* unmanaged<nint, uint*, void> pickSongNative = null!;

        private delegate nint PlayMusicDelegate(nint self, string filename, float volume, uint fadeTime);

        private Hook<PlayMusicDelegate> playMusicHook = null!;

        // Used to know if we should reset music between reloads or not
        private string? lastBgmPath;

        private void HookSong()
        {
            // Called when lobby music needs to be changed - we force call the game to call it by resetting the CurrentLobbyMusicIndex pointer
            playMusicHook = Hook<PlayMusicDelegate>("E8 ?? ?? ?? ?? 48 89 47 18 89 5F 20", PlayMusicDetour);
        }

        // Sets songIndex to None and allows the game itself to restart music when it's appropiate
        private void ResetSongIndex()
        {
            Services.Log.Debug("ResetSongIndex");
            LobbyInfo->CurrentLobbyMusicIndex = LobbySong.None;
        }

        // Sets songIndex to None and calls the PickSong method to start playing
        private void ForcePlaySongIndex(LobbySong song)
        {
            LobbyInfo->CurrentLobbyMusicIndex = LobbySong.None;
            PickSong(song);
        }

        // Picks the lobby bgm depending on the provided songIndex, won't do anything if it thinks this songIndex is already playing
        private void PickSong(LobbySong songIndex) => pickSongNative(lobbyStructAddress, (uint*)&songIndex);

        // TODO: figure out looping on tracks that don't loop
        private nint PlayMusicDetour(nint self, string filename, float volume, uint fadeTime)
        {
            Services.Log.Debug($"PlayMusicDetour {self.ToInt64():X} {filename} {volume} {fadeTime}");

            // if we're waiting on a reload prevent music from playing to make it less jarring for the user
            if (shouldReloadTitleScreenOnLoadingStage2)
            {
                return IntPtr.Zero;
            }
            else if (CurrentLobbyMap == GameLobbyType.CharaSelect && !characterSelectLocationModel.BgmPath.IsNullOrEmpty())
            {
                Services.Log.Debug($"Setting music to {characterSelectLocationModel.BgmPath}");
                filename = characterSelectLocationModel.BgmPath;
            }
            else if (CurrentLobbyMap == GameLobbyType.Title &&
                ShouldModifyTitleScreen &&
                !titleScreenLocationModel.BgmPath.IsNullOrEmpty())
            {
                Services.Log.Debug($"Setting music to {titleScreenLocationModel.BgmPath}");
                filename = titleScreenLocationModel.BgmPath;
            }
            lastBgmPath = filename;
            return playMusicHook.Original(self, filename, volume, fadeTime);
        }
    }
}
