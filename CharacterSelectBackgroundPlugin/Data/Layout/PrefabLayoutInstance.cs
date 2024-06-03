using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using FFXIVClientStructs.Interop;
using FFXIVClientStructs.STD;
using System.Runtime.InteropServices;

namespace CharacterSelectBackgroundPlugin.Data.Layout
{

    [StructLayout(LayoutKind.Explicit, Size = 0x190)]
    public unsafe struct PrefabLayoutInstance
    {
        [FieldOffset(0x000)] public ILayoutInstance ILayoutInstance;
        [FieldOffset(0x030)] public void** ResourceEventListener; // base class; contains only vtable
        [FieldOffset(0x038)] public ResourceHandle* ResourceHandle;
        [FieldOffset(0x050)] public Transform Transform;
        [FieldOffset(0x080)] public InstanceList Instances;
        //[FieldOffset(0x0A8)] public InstanceList uA8;
        [FieldOffset(0x120)] public uint Flags1; // 0x1 = load started; 0x3 = load failed or contents added; 0x4 = failed to add contents
        [FieldOffset(0x12C)] public uint Flags2; // 0x8 = colliders active

        [StructLayout(LayoutKind.Explicit, Size = 0x50)]
        public unsafe struct InstanceData
        {
            [FieldOffset(0x10)] public ILayoutInstance* Instance;
            [FieldOffset(0x20)] public Transform Transform;

        }

        [StructLayout(LayoutKind.Explicit, Size = 0x28)]
        public unsafe struct InstanceList
        {
            [FieldOffset(0x08)] public PrefabLayoutInstance* Owner;
            [FieldOffset(0x10)] public StdVector<Pointer<InstanceData>> Instances;

        }
    }
}
