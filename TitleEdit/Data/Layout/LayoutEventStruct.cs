using System.Runtime.InteropServices;

namespace TitleEdit.Data.Layout;

[StructLayout(LayoutKind.Explicit, Size = 0x20)]
public struct LayoutEventStruct
{
    [FieldOffset(0x0)]
    public uint StartId;
    [FieldOffset(0x8)]
    public uint FinishId;
    [FieldOffset(0x18)]
    public bool Loaded;
}
