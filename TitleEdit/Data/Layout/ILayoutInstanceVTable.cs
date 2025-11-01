using System.Runtime.InteropServices;

namespace TitleEdit.Data.Layout
{
    [StructLayout(LayoutKind.Explicit)]
    public unsafe partial struct ILayoutInstanceVTable
    {
        [FieldOffset(41 * 0x8)]
        public delegate* unmanaged[Stdcall]<void*, byte, float, void> setActiveVF41;
        [FieldOffset(54 * 0x8)]
        public delegate* unmanaged[Stdcall]<void*, byte, void> setActiveVF54;
        [FieldOffset(63 * 0x8)]
        public delegate* unmanaged[Stdcall]<void*, byte, void> setActive;
    }
}
