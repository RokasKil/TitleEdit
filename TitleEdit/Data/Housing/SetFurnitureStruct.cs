using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace TitleEdit.Data.Housing;

[StructLayout(LayoutKind.Explicit, Size = 0x20)]
public struct SetFurnitureStruct
{
    [FieldOffset(0x0)] public uint Id;
    [FieldOffset(0x4)] public byte Stain;
    [FieldOffset(0x8)] public float Rotation;
    [FieldOffset(0xc)] public Vector3 Position;
}