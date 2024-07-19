using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using System.Runtime.InteropServices;

namespace CharacterSelectBackgroundPlugin.Data.Layout
{

    [StructLayout(LayoutKind.Explicit, Size = 0xa0)]
    public unsafe struct VfxLayoutInstance
    {
        [FieldOffset(0x00)] public ILayoutInstance ILayoutInstance;
        [FieldOffset(0x30)] public DrawObject* VfxDrawObject; // probably drawobject not 100% either way it represents the vfx object
        [FieldOffset(0x94)] public byte Flags;
        [FieldOffset(0x96)] public short VfxTriggerIndex;
    }
}
