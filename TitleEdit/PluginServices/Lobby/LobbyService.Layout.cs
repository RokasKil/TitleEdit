using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using System.Collections.Generic;
using TitleEdit.Data.Layout;
using TitleEdit.Data.Lobby;
using TitleEdit.Data.Persistence;
using TitleEdit.Extensions;
using TitleEdit.Utility;

namespace TitleEdit.PluginServices.Lobby
{
    public unsafe partial class LobbyService
    {

        [Signature("40 53 48 83 EC ?? 44 0F BF C1")]
        private readonly delegate* unmanaged<ushort, void> setTimeNative = null!;

        private delegate void CharSelectSetWeatherDelegate();

        private Hook<CharSelectSetWeatherDelegate> lobbySetWeatherHook = null!;

        private bool forceUpdateCharacter;
        private void HookLayout()
        {
            // Called when game does some lobby weather setting - we use it as an indicator to set scene details like weather, time and layout
            // Called on scene load and on displayed character switch
            lobbySetWeatherHook = Hook<CharSelectSetWeatherDelegate>("48 83 EC ?? 0F B7 05", LobbySetWeatherDetour);
        }

        private void LobbySetWeatherDetour()
        {
            lobbySetWeatherHook.Original();
            SetLayoutInfo();
            if (forceUpdateCharacter)
            {
                UpdateCharacter(true);
                forceUpdateCharacter = false;
            }
            Services.Log.Debug($"LobbySetWeatherDetour {Services.WeatherService.WeatherId}");

        }

        private void SetLayoutInfo()
        {
            LocationModel model;
            if (CurrentLobbyMap == GameLobbyType.CharaSelect)
            {
                model = characterSelectLocationModel;
            }
            else if (CurrentLobbyMap == GameLobbyType.Title && ShouldModifyTitleScreen)
            {
                model = titleScreenLocationModel;
            }
            else
            {
                return;
            }
            if (model.SaveFestivals && model.Festivals != null)
            {
                fixed (uint* pFestivals = model.Festivals)
                {
                    Services.LayoutService.LayoutManager->SetActiveFestivals((GameMain.Festival*)pFestivals);
                }
            }
            else
            {
                Services.Log.Debug("Unsetting festivals");
                fixed (uint* pFestivals = new uint[4])
                {
                    Services.LayoutService.LayoutManager->SetActiveFestivals((GameMain.Festival*)pFestivals);
                }
            }
            Services.WeatherService.WeatherId = model.WeatherId;
            ushort timetoset = (ushort)(model.TimeOffset == ushort.MaxValue ? Services.LocationService.TimeOffset : model.TimeOffset);
            Services.Log.Debug($"Setting time to: {timetoset}");
            SetTime(timetoset);
            Services.Log.Debug($"SetWeather to {Services.WeatherService.WeatherId}");
            if (model.SaveLayout && model.Active != null && model.Inactive != null)
            {
                List<ulong> unknownUUIDs = new();
                Services.LayoutService.ForEachInstance(instance =>
                {
                    if (model.Active.Contains(instance.Value->UUID()))
                    {
                        SetActive(instance.Value, true, model);
                    }
                    else if (model.Inactive.Contains(instance.Value->UUID()))
                    {
                        SetActive(instance.Value, false, model);
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
        }

        private void SetActive(ILayoutInstance* instance, bool active, LocationModel model)
        {
            if (instance->Id.Type == InstanceType.Vfx)
            {
                if (model.UseVfx)
                {
                    SetIndex((VfxLayoutInstance*)instance, model);
                    instance->SetActiveVf54(active);
                }
            }
            else
            {
                instance->SetActive(active);
            }
        }

        // Sets vfx trigger index, defines which track of the vfx to play (maybe)
        private void SetIndex(VfxLayoutInstance* instance, LocationModel model)
        {
            if (model.VfxTriggerIndexes.TryGetValue(instance->ILayoutInstance.UUID(), out var index) && index != instance->VfxTriggerIndex)
            {
                Services.LayoutService.SetVfxLayoutInstanceVfxTriggerIndex(instance, index);
            }
        }

        private void SetTime(ushort time) => setTimeNative(time);
    }
}
