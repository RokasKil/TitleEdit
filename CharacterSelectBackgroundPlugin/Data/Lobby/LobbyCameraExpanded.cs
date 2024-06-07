using FFXIVClientStructs.FFXIV.Client.Game;
using System.Runtime.InteropServices;

namespace CharacterSelectBackgroundPlugin.Data.Lobby
{
    [StructLayout(LayoutKind.Explicit, Size = 0x300)]
    public unsafe struct LobbyCameraExpanded
    {
        [FieldOffset(0x00)]
        public LobbyCamera LobbyCamera;
        //Yaw and Pitch is part of Client::Game::Camera
        [FieldOffset(0x130)]
        public float Yaw;
        [FieldOffset(0x134)]
        public float Pitch;
        [FieldOffset(0x2D0)]
        public CurvePoint lowPoint;
        [FieldOffset(0x2C0)]
        public CurvePoint midPoint;
        [FieldOffset(0x2E0)]
        public CurvePoint highPoint;

    }

    [StructLayout(LayoutKind.Explicit, Size = 0x8)]
    public unsafe struct CurvePoint
    {
        [FieldOffset(0x0)]
        public float position;
        [FieldOffset(0x4)]
        public float value;

    }

}
