using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using FFXIVClientStructs.Interop;
using FFXIVClientStructs.STD;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace CharacterSelectBackgroundPlugin.Data.Layout
{
    //Taken from https://github.com/aers/FFXIVClientStructs/pull/809
    [StructLayout(LayoutKind.Explicit, Size = 0xB90)]
    public unsafe struct LayoutManagerExpanded
    {
        [FieldOffset(0x000)] public LayoutManager layoutManager;
        [FieldOffset(0x000)] public IManagerBase IManagerBase;
        [FieldOffset(0x018)] public int InitState; // 7 is fully loaded and ready, <7 are various stages of init
        [FieldOffset(0x01C)] public int Type; // 2 for normal levels, 3 ???
        [FieldOffset(0x020)] public uint TerritoryTypeId; // TerritoryType row id
        [FieldOffset(0x024)] public uint CfcId; // ContentFinderCondition row id
        [FieldOffset(0x028)] public uint LayerFilterKey; // used for finding correct layer filter if TerritoryTypeId == 0
        [FieldOffset(0x038)] public uint FestivalStatus; // SetActiveFestivals will not allow a change when not 5 or 0
        [FieldOffset(0x03D)] public bool InsideFestivalTransitionLayerUpdate; // when festival changes, layers are added/removed over 1s
        [FieldOffset(0x040)] public fixed uint ActiveFestivals[4]; // each element is a pair of words (id low + sub-id high)
        [FieldOffset(0x050)] public fixed uint NewFestivals[4]; // festival (de)activation is not immedate
        [FieldOffset(0x060)] public float FestivalLayersAddTimer; // dt * 30
        [FieldOffset(0x064)] public float FestivalLayersRemoveTimer; // dt * 30
        [FieldOffset(0x068)] public void* StreamingManager;
        [FieldOffset(0x070)] public void* Environment;
        [FieldOffset(0x078)] public void* OBSetManager;
        [FieldOffset(0x080), Obsolete("Use OutdoorAreaData")] public void* HousingController;
        [FieldOffset(0x080)] public OutdoorAreaLayoutData* OutdoorAreaData;
        [FieldOffset(0x090)] public IndoorAreaLayoutData* IndoorAreaData;
        [FieldOffset(0x0C8)] public void* PVPData;
        [FieldOffset(0x0DC)] public int ForceUpdateAllStreaming;
        [FieldOffset(0x0E2)] public bool SkipAddingTerrainCollider;
        //[FieldOffset(0x0E3)] public bool uE3;
        [FieldOffset(0x0F0)] public int StreamingOriginType;
        [FieldOffset(0x100)] public Vector3 ForcedStreamingOrigin;
        //[FieldOffset(0x110)] public Vector3 u110_streamingType5Origin;
        [FieldOffset(0x120)] public Vector3 LastUpdatedStreamingOrigin;
        [FieldOffset(0x170)] public int HousingType;
        [FieldOffset(0x184)] public float LastUpdateDT; // set to dt on update
        [FieldOffset(0x188)] public int LastUpdateOdd; // flips between 0 and 1 on update, presumably for some double buffering somewhere
        [FieldOffset(0x1A0)] public StringTable ResourcePaths;
        [FieldOffset(0x1C0)] public ResourceHandle* LvbResourceHandle;
        [FieldOffset(0x1C8)] public StdVector<Pointer<ResourceHandle>> LayerGroupResourceHandles;
        [FieldOffset(0x1F8)] public StdMap<uint, Pointer<LayoutTerrain>> Terrains;
        [FieldOffset(0x208)] public StdMap<ushort, Pointer<LayoutLayer>> Layers;
        [FieldOffset(0x218)] public StdVector<Pointer<LayoutLayer>> FestivalLayersToRemove;
        [FieldOffset(0x230)] public StdVector<Pointer<LayoutLayer>> FestivalLayersToAdd;
        [FieldOffset(0x248)] public StdMap<InstanceType, Pointer<StdMap<ulong, Pointer<ILayoutInstance>>>> InstancesByType; // key in nested map is InstanceId << 32 | SubId
        [FieldOffset(0x258)] public StdMap<uint, Pointer<RefCountedString>> CrcToPath;
        [FieldOffset(0x268)] public StdMap<AnalyticShapeDataKey, AnalyticShapeData> CrcToAnalyticShapeData; // note: Value is aligned to 16 bytes, so key has tons of padding
        [FieldOffset(0x278)] public StdMap<uint, Pointer<Filter>> Filters;
        // 2A0: some map
        // 2B0: vector<LayoutU3*> streamingoriginupdatelisteners
        [FieldOffset(0x2E8)] public ResourceHandle* SvbResourceHandle;
        [FieldOffset(0x2F0)] public ResourceHandle* LcbResourceHandle;
        [FieldOffset(0x2F8)] public ResourceHandle* UwbResourceHandle;


        [StructLayout(LayoutKind.Explicit, Size = 0xC)]
        public unsafe struct Filter
        {
            [FieldOffset(0)] public uint Key;
            [FieldOffset(4)] public uint TerritoryTypeId;
            [FieldOffset(8)] public uint CfcId;
        }

        // note: this is a bad bad hack...
        [StructLayout(LayoutKind.Explicit, Size = 0x14)]
        public unsafe struct AnalyticShapeDataKey
        {
            [FieldOffset(0)] private uint _alignment;
            [FieldOffset(4)] public uint Key;
        }
    }
    [StructLayout(LayoutKind.Explicit, Size = 0x108)]
    public unsafe struct RefCountedString
    {
        [FieldOffset(0)] public int NumRefs;
        [FieldOffset(4)] public fixed byte Data[260];
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x20)]
    public unsafe struct StringTable
    {
        [FieldOffset(0x00)] public StdVector<Pointer<RefCountedString>> Strings;
        [FieldOffset(0x18)] public int NumNulls;
    }
    [StructLayout(LayoutKind.Explicit, Size = 0x130)]
    public unsafe struct LayoutTerrain
    {
        [FieldOffset(0x000)] public IManagerBase IManagerBase;
        [FieldOffset(0x018)] public void* GfxTerrain; // Client::Graphics::Scene::Terrain*
        [FieldOffset(0x020)] public ColliderStreamed* Collider;
        [FieldOffset(0x028)] public fixed byte Path[260];
        [FieldOffset(0x12C)] public int State;
    }
    [StructLayout(LayoutKind.Explicit, Size = 0x38)]
    public unsafe struct LayoutLayer
    {
        [FieldOffset(0x00)] public IManagerBase IManagerBase;
        [FieldOffset(0x18)] public ushort LayerGroupId;
        [FieldOffset(0x1A)] public ushort FestivalId;
        [FieldOffset(0x1C)] public ushort FestivalSubId;
        [FieldOffset(0x1E)] public byte Flags;
        //[FieldOffset(0x1F)] public byte u1F;
        //[FieldOffset(0x20)] public ushort u20;
        [FieldOffset(0x28)] public StdMap<uint, Pointer<ILayoutInstance>> Instances;
    }
}
