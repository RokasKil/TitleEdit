using FFXIVClientStructs.FFXIV.Common.Math;
using System.Runtime.InteropServices;

namespace CharacterSelectBackgroundPlugin.Data.Camera
{
    [StructLayout(LayoutKind.Explicit, Size = 0x2b0)]
    public unsafe struct CameraExpanded
    {
        [FieldOffset(0x00)]
        public FFXIVClientStructs.FFXIV.Client.Game.Camera Camera;
        //Yaw and Pitch is part of Client::Game::Camera
        [FieldOffset(0x130)]
        public Vector3 Orientation;
        [FieldOffset(0x130)]
        public float Yaw;
        [FieldOffset(0x134)]
        public float Pitch;
        [FieldOffset(0x138)]
        public float Roll;
    }
}
