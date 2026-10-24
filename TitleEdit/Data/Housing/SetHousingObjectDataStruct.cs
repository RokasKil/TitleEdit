using System.Runtime.InteropServices;

namespace TitleEdit.Data.Housing;

[StructLayout(LayoutKind.Explicit, Size = 0x20)]
public unsafe struct SetHousingObjectDataStruct
{
    [FieldOffset(0x0)] public ushort Index;
    [FieldOffset(0x2)] public byte Count;
    [FieldOffset(0x4)] public fixed ushort Value1[8];
    [FieldOffset(0x14)] public fixed byte Value2[8];
    [FieldOffset(0x1C)] public fixed byte Value3[8];
    [FieldOffset(0x24)] public fixed byte Value4[8];
}