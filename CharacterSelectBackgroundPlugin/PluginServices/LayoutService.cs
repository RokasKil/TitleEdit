using CharacterSelectBackgroundPlugin.Data.Layout;
using CharacterSelectBackgroundPlugin.Utility;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.Interop;
using System;
using System.Collections.Generic;

namespace CharacterSelectBackgroundPlugin.PluginServices
{
    public unsafe class LayoutService : AbstractService
    {

        [Signature("48 89 5C 24 ?? 57 48 83 EC ?? 8B FA 48 8B D9 83 FA ?? 75")]
        private readonly delegate* unmanaged<VfxLayoutInstance*, int, void> setVfxLayoutInstanceVfxTriggerIndex = null!;
        public LayoutManagerExpanded* LayoutManager => (LayoutManagerExpanded*)LayoutWorld.Instance()->ActiveLayout;
        public bool LayoutInitialized => LayoutManager->InitState == 7;

        public delegate void LayoutInstanceSetActiveDelegate(ILayoutInstance* layout, bool active);

        public unsafe event LayoutInstanceSetActiveDelegate? OnLayoutInstanceSetActive;
        public event Action? OnLayoutChange;
        private bool territoryChanged;
        public Dictionary<IntPtr, Hook<LayoutInstanceSetActiveDelegate>> ActiveHooks { get; set; } = [];

        public LayoutService()
        {
            Services.GameInteropProvider.InitializeFromAttributes(this);
            Services.Framework.Update += Tick;
            Services.ClientState.Logout += OnLogout;
            Services.ClientState.TerritoryChanged += TerritoryChanged;
            TerritoryChanged(Services.ClientState.TerritoryType);

        }

        private unsafe void Tick(IFramework framework)
        {
            if (Services.ClientState.LocalPlayer != null)
            {
                if (territoryChanged && LayoutInitialized)
                {
                    territoryChanged = false;
                    MakeSetActiveHooks();
                }
            }

        }
        private void TerritoryChanged(ushort territoryId)
        {
            territoryChanged = true;
            OnLayoutChange?.Invoke();
        }

        public void OnLogout()
        {
            ClearSetActiveHooks();
        }

        public unsafe void MakeSetActiveHooks()
        {
            ClearSetActiveHooks();
            var vTables = new HashSet<IntPtr>();
            var layoutManger = (LayoutManagerExpanded*)(LayoutWorld.Instance()->ActiveLayout);

            Services.Log.Debug($"[MakeSetActiveHooks] Got {(IntPtr)layoutManger:X} layoutmanager {layoutManger->InitState}");
            foreach (var entry in layoutManger->Layers)
            {
                var layer = entry.Item2.Value;
                foreach (var instanceEntry in layer->Instances)
                {
                    ForEachInstanceAndDescendants(instanceEntry.Item2, instance => vTables.Add((IntPtr)instance.Value->VTable));
                }
            }
            Services.Log.Debug($"[MakeSetActiveHooks] Got {vTables.Count} vTables");
            foreach (var pVTable in vTables)
            {
                var setActiveAddress = *(IntPtr*)(pVTable + 0x1f8);
                var setActiveAddressVF54 = *(IntPtr*)(pVTable + 0x1b0);
                Services.Log.Debug($"{pVTable:X} - {setActiveAddress:X} - {setActiveAddressVF54:X}");
                var hook = Services.GameInteropProvider.HookFromAddress<LayoutInstanceSetActiveDelegate>(setActiveAddress, (layout, active) => LayoutInstanceSetActiveDetour(pVTable, layout, active));
                hook.Enable();
                ActiveHooks[pVTable] = hook;
            }
        }

        public void ClearSetActiveHooks()
        {
            foreach (var item in ActiveHooks)
            {
                item.Value.Dispose();
            }
            ActiveHooks.Clear();
        }

        private unsafe void LayoutInstanceSetActiveDetour(IntPtr pVTable, ILayoutInstance* layout, bool active)
        {
            OnLayoutInstanceSetActive?.Invoke(layout, active);
            ActiveHooks[pVTable].Original(layout, active);
        }


        public unsafe void ForEachInstance(Action<Pointer<ILayoutInstance>> action)
        {
            ForEachInstance(LayoutManager, action);
        }

        public unsafe void ForEachInstance(LayoutManagerExpanded* manager, Action<Pointer<ILayoutInstance>> action)
        {
            foreach (var entry in manager->Layers)
            {
                var layer = entry.Item2.Value;
                foreach (var instanceEntry in layer->Instances)
                {
                    ForEachInstanceAndDescendants(instanceEntry.Item2, action);
                }
            }
        }

        public unsafe void ForEachInstanceAndDescendants(ILayoutInstance* instance, Action<Pointer<ILayoutInstance>> action)
        {
            action(instance);
            if (instance->Id.Type == InstanceType.Prefab || instance->Id.Type == InstanceType.Prefab2)
            {
                var prefabInstance = (PrefabLayoutInstance*)instance;
                foreach (var instanceData in prefabInstance->Instances.Instances.Span)
                {
                    ForEachInstanceAndDescendants(instanceData.Value->Instance, action);
                }
            }
        }

        public void SetVfxLayoutInstanceVfxTriggerIndex(VfxLayoutInstance* instance, int index)
        {
            setVfxLayoutInstanceVfxTriggerIndex(instance, index);
        }

        public override void Dispose()
        {
            base.Dispose();
            ClearSetActiveHooks();
            Services.Framework.Update -= Tick;
            Services.ClientState.Logout -= OnLogout;
            Services.ClientState.TerritoryChanged -= TerritoryChanged;
        }
    }
}
