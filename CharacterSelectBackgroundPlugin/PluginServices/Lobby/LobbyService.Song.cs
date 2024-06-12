using CharacterSelectBackgroundPlugin.Data.BGM;
using CharacterSelectBackgroundPlugin.Data.Lobby;
using CharacterSelectBackgroundPlugin.Utility;
using Dalamud.Hooking;
using Dalamud.Utility;
using Dalamud.Utility.Signatures;
using System;
using System.Runtime.InteropServices;

namespace CharacterSelectBackgroundPlugin.PluginServices.Lobby
{
    public unsafe partial class LobbyService
    {
        [Signature("E8 ?? ?? ?? ?? 33 C9 E8 ?? ?? ?? ?? 48 8B 0D")]
        private readonly delegate* unmanaged<nint, ushort, void> pickSongNative = null!;

        private delegate nint PlayMusicDelegate(nint self, string filename, float volume, uint fadeTime);

        private Hook<PlayMusicDelegate> playMusicHook = null!;

        // Probably some lobby instance
        // method at E8 ?? ?? ?? ?? 33 C9 E8 ?? ?? ?? ?? 48 8B 0D picks a song from an array of 7 entries
        // ["", <arr title>, <char select>, <hw title>, <sb title>, <shb title>, <ew title>]
        // calls the method hooked at playMusicHook with selected path and stores the model at 0x18 with the index being stored at 0x20
        // on subsequent calls it checks if we need to reset by comparing offset 0x20 with provided music index
        // we abuse that by setting it back to 0
        private nint* lobbyBgmBasePointerAddress = null!;

        public LobbySong CurrentLobbyMusicIndex
        {
            get => (LobbySong)Marshal.ReadInt32(*lobbyBgmBasePointerAddress, 0x20);
            set => Marshal.WriteInt32(*lobbyBgmBasePointerAddress, 0x20, (int)value);
        }

        private string? lastBgmPath;

        private void HookSong()
        {
            if (setTimeNative == null)
            {
                throw new Exception("Failed to find setTimeNative");
            }
            // Points to a Value that indicates the current lobby bgm Type that's playing, we maniplate this to force bgm change alongside playMusicHook
            lobbyBgmBasePointerAddress = (nint*)Utils.GetStaticAddressFromSigOrThrow("48 8B 35 ?? ?? ?? ?? 88 46");

            // Called when lobby music needs to be changed - we force call the game to call it by resetting the CurrentLobbyMusicIndex pointer
            playMusicHook = Hook<PlayMusicDelegate>("E8 ?? ?? ?? ?? 48 89 47 18 89 5F 20", PlayMusicDetour);
        }

        private void ResetSongIndex()
        {
            CurrentLobbyMusicIndex = LobbySong.None;
        }

        private void ForcePlaySongIndex(LobbySong song)
        {
            CurrentLobbyMusicIndex = LobbySong.None;
            PickSong(song);
        }

        private void PickSong(LobbySong songIndex) => pickSongNative(*lobbyBgmBasePointerAddress, (ushort)songIndex);

        // TODO: figure out looping on tracks that don't loop
        private nint PlayMusicDetour(nint self, string filename, float volume, uint fadeTime)
        {
            Services.Log.Debug($"PlayMusicDetour {self.ToInt64():X} {filename} {volume} {fadeTime}");

            if (CurrentLobbyMap == GameLobbyType.CharaSelect && !locationModel.BgmPath.IsNullOrEmpty())
            {
                Services.Log.Debug($"Setting music to {locationModel.BgmPath}");
                filename = locationModel.BgmPath;
            }
            lastBgmPath = filename;
            return playMusicHook.Original(self, filename, volume, fadeTime);
        }
    }
}
