#define CALC_LAYOUT_UPDATE

using CharacterSelectBackgroundPlugin.Data.Layout;
using CharacterSelectBackgroundPlugin.Utility;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Group;
using FFXIVClientStructs.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CharacterSelectBackgroundPlugin.PluginServices
{
    // This needs rewriting/optimizing
    public class LayoutService : AbstractService
    {
        public unsafe LayoutManager* LayoutManager => LayoutWorld.Instance()->ActiveLayout;
        public unsafe bool LayoutInitialized => LayoutManager->InitState == 7;

        public unsafe delegate void LayoutInstanceSetActiveDelegate(ILayoutInstance* instance, bool active);
        public unsafe delegate void VfxLayoutInstanceSetVfxTriggerIndexDelegate(VfxLayoutInstance* vfxInstance, int index);

        private Hook<VfxLayoutInstanceSetVfxTriggerIndexDelegate> vfxLayoutInstanceSetVfxTriggerIndexHook;

        public unsafe event LayoutInstanceSetActiveDelegate? OnLayoutInstanceSetActive;
        public unsafe event VfxLayoutInstanceSetVfxTriggerIndexDelegate? OnVfxLayoutInstanceSetVfxTriggerIndex;
        public event Action? OnLayoutChange;

        private bool territoryChanged;
        public Dictionary<IntPtr, Hook<LayoutInstanceSetActiveDelegate>> ActiveHooks { get; set; } = [];

        private List<InstanceType> instanceTypes =
        [
            InstanceType.BgPart,
            InstanceType.Light,
            InstanceType.Vfx,
            InstanceType.SharedGroup
        ];
#if CALC_LAYOUT_UPDATE
        private Stopwatch updateSW = new();
#endif
        public double UpdateTime = 0;
        public LayoutService()
        {
            Services.GameInteropProvider.InitializeFromAttributes(this);
            unsafe
            {
                vfxLayoutInstanceSetVfxTriggerIndexHook = Hook<VfxLayoutInstanceSetVfxTriggerIndexDelegate>("48 89 5C 24 ?? 57 48 83 EC ?? 8B FA 48 8B D9 83 FA ?? 75", SetVfxLayoutInstanceVfxTriggerIndexDetour);
            }
        }

        public override void Init()
        {
            Services.Framework.Update += Tick;
            Services.ClientState.Logout += OnLogout;
            Services.ClientState.TerritoryChanged += TerritoryChanged;
            TerritoryChanged(Services.ClientState.TerritoryType);
            EnableHooks();
        }


        private unsafe void SetVfxLayoutInstanceVfxTriggerIndexDetour(VfxLayoutInstance* vfxInstance, int index)
        {
            vfxLayoutInstanceSetVfxTriggerIndexHook.Original(vfxInstance, index);
            OnVfxLayoutInstanceSetVfxTriggerIndex?.Invoke(vfxInstance, index);
        }

        private void Tick(IFramework framework)
        {
            if (Services.ClientState.IsLoggedIn)
            {
                if (territoryChanged && LayoutInitialized)
                {
                    territoryChanged = false;
                    MakeSetActiveHooks();
                }
            }
#if CALC_LAYOUT_UPDATE
            UpdateTime = updateSW.Elapsed.TotalMilliseconds;
            updateSW.Reset();
#endif
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

            Services.Log.Debug($"[MakeSetActiveHooks] Got {(IntPtr)LayoutManager:X} layoutmanager {LayoutManager->InitState}");
            ForEachInstance(instance => vTables.Add(*(IntPtr*)instance.Value));
            Services.Log.Debug($"[MakeSetActiveHooks] Got {vTables.Count} vTables");
            foreach (var pVTable in vTables)
            {
                var setActiveAddress = *(IntPtr*)(pVTable + 0x1f8);
                var setActiveAddressVF54 = *(IntPtr*)(pVTable + 0x1b0);
                Services.Log.Debug($"{pVTable:X} - {setActiveAddress:X} - {setActiveAddressVF54:X}");
                if (ActiveHooks.ContainsKey(setActiveAddress)) continue;
                var hook = Services.GameInteropProvider.HookFromAddress<LayoutInstanceSetActiveDelegate>(setActiveAddress, (layout, active) => LayoutInstanceSetActiveDetour(setActiveAddress, layout, active));
                ActiveHooks[setActiveAddress] = hook;
                hook.Enable();
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



        private unsafe void LayoutInstanceSetActiveDetour(IntPtr funcAddress, ILayoutInstance* instance, bool active)
        {
            if (instance->Layout == LayoutManager)
            {
                // SetActive might be outside of the main thread
                Services.Framework.RunOnFrameworkThread(() =>
                {

#if CALC_LAYOUT_UPDATE
                    updateSW.Start();
#endif
                    OnLayoutInstanceSetActive?.Invoke(instance, active);

#if CALC_LAYOUT_UPDATE
                    updateSW.Stop();
#endif
                });
            }
            ActiveHooks[funcAddress].Original(instance, active);
        }


        public unsafe void ForEachInstance(Action<Pointer<ILayoutInstance>> action)
        {
            ForEachInstance(LayoutManager, action);
        }

        public unsafe void ForEachInstance(LayoutManager* manager, Action<Pointer<ILayoutInstance>> action)
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
            if (instanceTypes.Contains(instance->Id.Type))
            {
                action(instance);
                if (instance->Id.Type == InstanceType.SharedGroup)
                {
                    var prefabInstance = (SharedGroupLayoutInstance*)instance;
                    foreach (var instanceData in prefabInstance->Instances.Instances.AsSpan())
                    {
                        ForEachInstanceAndDescendants(instanceData.Value->Instance, action);
                    }
                }
            }
        }

        public unsafe void SetVfxLayoutInstanceVfxTriggerIndex(VfxLayoutInstance* instance, int index) => vfxLayoutInstanceSetVfxTriggerIndexHook.Original(instance, index);

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
