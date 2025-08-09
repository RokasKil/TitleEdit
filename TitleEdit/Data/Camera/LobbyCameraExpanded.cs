using FFXIVClientStructs.FFXIV.Client.Game;
using System.Runtime.InteropServices;

namespace TitleEdit.Data.Camera
{
    [StructLayout(LayoutKind.Explicit, Size = 0x300)]
    public unsafe struct LobbyCameraExpanded
    {
        [FieldOffset(0x00)]
        public LobbyCamera LobbyCamera;
        //Yaw and Pitch is part of Client::Game::Camera
        [FieldOffset(0x140)]
        public float Yaw;
        [FieldOffset(0x144)]
        public float Pitch;
        [FieldOffset(0x148)]
        public float Roll;
        [FieldOffset(0x2C0)]
        public bool TitleScreenLocked;
        [FieldOffset(0x2C2)]
        public bool CameraCurveEnabled;
        [FieldOffset(0x2E0)]
        public CurvePoint LowPoint;
        [FieldOffset(0x2D0)]
        public CurvePoint MidPoint;
        [FieldOffset(0x2F0)]
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
