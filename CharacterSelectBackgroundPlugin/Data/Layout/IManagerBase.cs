using System.Runtime.InteropServices;

namespace CharacterSelectBackgroundPlugin.Data.Layout
{
    [StructLayout(LayoutKind.Explicit, Size = 0x18)]
    public unsafe partial struct IManagerBase
    {
        [FieldOffset(0x08)] public IManagerBase* Owner;
        [FieldOffset(0x10)] public uint Id;
    }
}
