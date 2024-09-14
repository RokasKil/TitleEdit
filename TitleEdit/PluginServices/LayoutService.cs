using Dalamud.Game.ClientState.Conditions;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Group;
using FFXIVClientStructs.Interop;
using System;
using System.Collections.Generic;
#if CALC_LAYOUT_UPDATE
using System.Diagnostics;
#endif
using TitleEdit.Data.Layout;
using TitleEdit.Utility;

namespace TitleEdit.PluginServices
{
    // This needs rewriting/optimizing
    public class LayoutService : AbstractService
    {
        public unsafe delegate void LayoutInstanceSetActiveDelegate(ILayoutInstance* instance, bool active);
        public unsafe delegate void VfxLayoutInstanceSetVfxTriggerIndexDelegate(VfxLayoutInstance* vfxInstance, int index);
        public unsafe delegate void LayoutWorldInitManagerDelegate(IntPtr p1, uint p2, IntPtr p3, uint p4, uint p5, IntPtr p6, uint p7);

        private Hook<VfxLayoutInstanceSetVfxTriggerIndexDelegate> vfxLayoutInstanceSetVfxTriggerIndexHook = null!;
        private Hook<LayoutWorldInitManagerDelegate> layoutWorldInitManagerHook = null!;

        public unsafe event LayoutInstanceSetActiveDelegate? OnLayoutInstanceSetActive;
        public unsafe event VfxLayoutInstanceSetVfxTriggerIndexDelegate? OnVfxLayoutInstanceSetVfxTriggerIndex;
        public event Action? OnLayoutChange;

        public unsafe LayoutManager* LayoutManager => LayoutWorld.Instance()->ActiveLayout;
        public unsafe bool LayoutInitialized => LayoutManager->InitState == 7;
        public unsafe uint LayoutTerritoryId => LayoutManager->TerritoryTypeId;
        public unsafe uint LayoutLayerFilterKey => LayoutManager->LayerFilterKey;

        private bool territoryChanged;
        public Dictionary<IntPtr, Hook<LayoutInstanceSetActiveDelegate>> ActiveHooks { get; set; } = [];

        private bool ShouldEnableHooks => Services.ConfigurationService.TrackPlayerLocation &&
            Services.ConfigurationService.SaveLayout &&
            (!Services.Condition.Any(ConditionFlag.BoundByDuty) || Services.ConfigurationService.SaveLayoutInInstance);

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
        }

        public override void Init()
        {
            unsafe
            {
                vfxLayoutInstanceSetVfxTriggerIndexHook = Hook<VfxLayoutInstanceSetVfxTriggerIndexDelegate>("48 89 5C 24 ?? 57 48 83 EC ?? 8B FA 48 8B D9 83 FA ?? 75", SetVfxLayoutInstanceVfxTriggerIndexDetour);
                layoutWorldInitManagerHook = Hook<LayoutWorldInitManagerDelegate>("E8 ?? ?? ?? ?? 8B 4C 24 ?? 33 DB", LayoutWorldInitManagerDetour);

            }
            Services.Framework.Update += Tick;
            Services.ClientState.Logout += OnLogout;
            TerritoryChanged();
            EnableHooks();
        }

        private unsafe void LayoutWorldInitManagerDetour(nint p1, uint p2, nint p3, uint p4, uint p5, nint p6, uint p7)
        {
            layoutWorldInitManagerHook.Original(p1, p2, p3, p4, p5, p6, p7);
            TerritoryChanged();
        }

        private unsafe void SetVfxLayoutInstanceVfxTriggerIndexDetour(VfxLayoutInstance* vfxInstance, int index)
        {
            vfxLayoutInstanceSetVfxTriggerIndexHook?.Original(vfxInstance, index);
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

        private void TerritoryChanged()
        {
            Services.Log.Debug($"[TerritoryChanged]");
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
                if (ShouldEnableHooks)
                {
                    hook.Enable();
                }
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


        public void SettingsUpdated()
        {
            foreach (var hook in ActiveHooks.Values)
            {
                if (ShouldEnableHooks)
                {
                    hook.Enable();
                }
                else
                {
                    hook.Disable();
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            ClearSetActiveHooks();
            Services.Framework.Update -= Tick;
            Services.ClientState.Logout -= OnLogout;
        }
    }
}
