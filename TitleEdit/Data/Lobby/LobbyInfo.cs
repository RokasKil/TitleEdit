using System.Runtime.InteropServices;
using TitleEdit.Data.BGM;

namespace TitleEdit.Data.Lobby
{
    //Size is more than that, this is just what I use, also why does it even need a size when it can math it out of field offset
    [StructLayout(LayoutKind.Explicit, Size = 40)]
    public struct LobbyInfo
    {
        // What cinematic in title screen is currently playing
        [FieldOffset(0x10)]
        public TitleScreenMovie CurrentTitleScreenMovieType;
        [FieldOffset(0x20)]
        public LobbySong CurrentLobbyMusicIndex;
        [FieldOffset(0x34)]
        public TitleScreenExpansion CurrentTitleScreenType;
        // This needs to be set to 0 for logos to load properly, haven't seen it being read anywhere else
        [FieldOffset(0x38)]
        public bool PreDawntrailLogoFlag;
        [FieldOffset(0x39)]
        public bool FreeTrial;

    }
}
