using Dalamud.Hooking;
using Dalamud.Utility;
using Dalamud.Utility.Signatures;
using System.Runtime.InteropServices;
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

        public LobbySong CurrentLobbyMusicIndex
        {
            get => (LobbySong)Marshal.ReadInt32(lobbyStructAddress, 0x20);
            set => Marshal.WriteInt32(lobbyStructAddress, 0x20, (int)value);
        }

        private string? lastBgmPath;

        private void HookSong()
        {
            // Called when lobby music needs to be changed - we force call the game to call it by resetting the CurrentLobbyMusicIndex pointer
            playMusicHook = Hook<PlayMusicDelegate>("E8 ?? ?? ?? ?? 48 89 47 18 89 5F 20", PlayMusicDetour);
        }

        private void ResetSongIndex()
        {
            Services.Log.Debug("ResetSongIndex");
            CurrentLobbyMusicIndex = LobbySong.None;
        }

        private void ForcePlaySongIndex(LobbySong song)
        {
            CurrentLobbyMusicIndex = LobbySong.None;
            PickSong(song);
        }

        private void PickSong(LobbySong songIndex)
        {
            pickSongNative(lobbyStructAddress, (uint*)&songIndex);

        }

        // TODO: figure out looping on tracks that don't loop
        private nint PlayMusicDetour(nint self, string filename, float volume, uint fadeTime)
        {
            Services.Log.Debug($"PlayMusicDetour {self.ToInt64():X} {filename} {volume} {fadeTime}");

            if (CurrentLobbyMap == GameLobbyType.CharaSelect && !characterSelectLocationModel.BgmPath.IsNullOrEmpty())
            {
                Services.Log.Debug($"Setting music to {characterSelectLocationModel.BgmPath}");
                filename = characterSelectLocationModel.BgmPath;
            }
            else if (CurrentLobbyMap == GameLobbyType.Title &&
                titleScreenLocationModel.TitleScreenOverride == null &&
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
