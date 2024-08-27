using TitleEdit.Data.Layout;
using TitleEdit.Data.Lobby;
using TitleEdit.Extensions;
using TitleEdit.Utility;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using System;
using System.Collections.Generic;

namespace TitleEdit.PluginServices.Lobby
{
    public unsafe partial class LobbyService
    {

        [Signature("40 53 48 83 EC ?? 44 0F BF C1")]
        private readonly delegate* unmanaged<ushort, void> setTimeNative = null!;

        private delegate void CharSelectSetWeatherDelegate();

        private Hook<CharSelectSetWeatherDelegate> charSelectSetWeatherHook = null!;

        private bool forceUpdateCharacter;
        private void HookLayout()
        {
            if (setTimeNative == null)
            {
                throw new Exception("Failed to find setTimeNative");
            }
            // Called when game does some lobby weather setting - we use it as an indicator to set scene details like weather, time and layout
            // Called on scene load and on displayed character switch
            charSelectSetWeatherHook = Hook<CharSelectSetWeatherDelegate>("48 83 EC ?? 0F B7 05", CharSelectSetWeatherDetour);
        }

        private void CharSelectSetWeatherDetour()
        {
            charSelectSetWeatherHook.Original();
            SetLayoutInfo();
            if (forceUpdateCharacter)
            {
                UpdateCharacter(true);
                forceUpdateCharacter = false;
                forceUpdateCharacter = false;
            }
            Services.Log.Debug($"CharSelectSetWeatherDetour {EnvManager.Instance()->ActiveWeather}");

        }

        private void SetLayoutInfo()
        {
            if (CurrentLobbyMap == GameLobbyType.CharaSelect)
            {
                fixed (uint* pFestivals = chracterSelectLocationModel.Festivals)
                {
                    Services.LayoutService.LayoutManager->SetActiveFestivals((GameMain.Festival*)pFestivals);
                }
                EnvManager.Instance()->ActiveWeather = chracterSelectLocationModel.WeatherId;
                SetTime(chracterSelectLocationModel.TimeOffset);
                Services.Log.Debug($"SetWeather to {EnvManager.Instance()->ActiveWeather}");
                if (chracterSelectLocationModel.Active != null && chracterSelectLocationModel.Inactive != null)
                {
                    List<ulong> unknownUUIDs = new();
                    Services.LayoutService.ForEachInstance(instance =>
                    {
                        if (chracterSelectLocationModel.Active.Contains(instance.Value->UUID()))
                        {
                            SetActive(instance.Value, true);
                        }
                        else if (chracterSelectLocationModel.Inactive.Contains(instance.Value->UUID()))
                        {
                            SetActive(instance.Value, false);
                        }
                        else
                        {
                            unknownUUIDs.Add(instance.Value->UUID());
                        }
                    });
                    if (unknownUUIDs.Count > 0)
                    {
                        Services.Log.Debug($"{unknownUUIDs.Count} UUIDs not found in the layout data");
                    }
                }
                else
                {
                    Services.Log.Warning($"Layout data was null for {lastContentId:X16}");
                }
            }
        }

        private void SetActive(ILayoutInstance* instance, bool active)
        {
            if (instance->Id.Type == InstanceType.Vfx)
            {
                SetIndex((VfxLayoutInstance*)instance);
                instance->SetActiveVf54(active);
            }
            else
            {
                instance->SetActive(active);
            }
        }

        private void SetIndex(VfxLayoutInstance* instance)
        {
            if (chracterSelectLocationModel.VfxTriggerIndexes.TryGetValue(instance->ILayoutInstance.UUID(), out var index))
            {
                Services.LayoutService.SetVfxLayoutInstanceVfxTriggerIndex(instance, index);
            }
        }

        private void SetTime(ushort time) => setTimeNative(time);
    }
}
