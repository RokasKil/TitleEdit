using FFXIVClientStructs.FFXIV.Common.Math;
using System.Runtime.InteropServices;

namespace TitleEdit.Data.Camera
{
    [StructLayout(LayoutKind.Explicit, Size = 0x2b0)]
    public unsafe struct CameraExpanded
    {
        [FieldOffset(0x00)]
        public FFXIVClientStructs.FFXIV.Client.Game.Camera Camera;
        //Yaw and Pitch is part of Client::Game::Camera
        [FieldOffset(0x140)]
        public Vector3 Orientation;
        [FieldOffset(0x140)]
        public float Yaw;
        [FieldOffset(0x144)]
        public float Pitch;
        [FieldOffset(0x148)]
        public float Roll;
    }
}
