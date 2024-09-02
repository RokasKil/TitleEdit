using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Common.Math;
using System.Runtime.InteropServices;

namespace TitleEdit.Data.Camera
{
    [StructLayout(LayoutKind.Explicit, Size = 0x300)]
    public unsafe struct LobbyCameraExpanded
    {
        [FieldOffset(0x00)]
        public LobbyCamera LobbyCamera;
        //Yaw and Pitch is part of Client::Game::Camera
        [FieldOffset(0x130)]
        public Vector3 Orientation;
        [FieldOffset(0x130)]
        public float Yaw;
        [FieldOffset(0x134)]
        public float Pitch;
        [FieldOffset(0x138)]
        public float Roll;
        [FieldOffset(0x2B0)]
        public bool TitleScreenLocked;
        [FieldOffset(0x2D0)]
        public CurvePoint LowPoint;
        [FieldOffset(0x2C0)]
        public CurvePoint MidPoint;
        [FieldOffset(0x2E0)]
        public CurvePoint HighPoint;

    }

    [StructLayout(LayoutKind.Explicit, Size = 0x8)]
    public unsafe struct CurvePoint
    {
        [FieldOffset(0x0)]
        public float Position;
        [FieldOffset(0x4)]
        public float Value;

    }

}
