using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using Dalamud.Utility.Numerics;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.Scheduler;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TitleEdit.Data.BGM;
using TitleEdit.Data.Lobby;
using TitleEdit.Data.Persistence;
using TitleEdit.Utility;

namespace TitleEdit.PluginServices.Lobby
{
    public unsafe partial class LobbyService
    {


        [Signature("E8 ?? ?? ?? ?? 48 8B CF E8 ?? ?? ?? ?? 48 8B CF E8 ?? ?? ?? ?? E8")]
        private readonly delegate* unmanaged<AgentLobby*, void> removeTitleScreenUi = null!;

        [Signature("E8 ?? ?? ?? ?? 48 8D 4F ?? 44 89 77")]
        private readonly delegate* unmanaged<ScheduleManagement*, IntPtr, void> cancelScheduledTask = null!;

        private readonly string[] recolorableAddons = { "_TitleRights", "_TitleMenu", "_TitleRevision" };

        private IntPtr titleCutsceneStructAddress;

        private delegate bool PickTitleLogo(IntPtr atkUnit);

        private Hook<PickTitleLogo> pickTitleLogoHook = null!;

        public bool TitleCutsceneIsLoaded
        {
            get => Marshal.ReadByte(titleCutsceneStructAddress, 0x98) == 1;
            set => Marshal.WriteInt32(titleCutsceneStructAddress, 0x98, value ? 1 : 0);
        }

        private bool lastCutsceneStatus = false;

        private TitleScreenExpansion? overridenTitleScreenType = null;

        private bool titleScreenLoaded = false;

        private bool ShouldModifyTitleScreen => titleScreenLoaded && titleScreenLocationModel.TitleScreenOverride == null;

        private bool shouldAnimateDawntrailLogo = false;

        private bool shouldAdvanceTitleLogoAnimation = false;
        private float advanceTitleLogoAnimationDuration = 0;

        private bool reloadingTitleScreenUi = false;
        private bool reloadingTitleScreen = false;

        private Queue<Action> titleMenuFinalizeActions = [];
        private Queue<Action> cutsceneStoppedActions = [];


        //These might be inneficient but we don't call them often so maybe it's fine or maybe it's smart and caches but I doubt
        private UiColorModel TitleScreenColors
        {
            get
            {
                UiColorModel color;
                if (titleScreenLocationModel.UiColor.Expansion == UiColorExpansion.Unspecified ||
                    Services.ConfigurationService.OverridePresetTitleScreenColor)
                {
                    color = Services.ConfigurationService.TitleScreenColor;
                }
                else
                {
                    color = titleScreenLocationModel.UiColor;
                }
                return color.Expansion == UiColorExpansion.Custom ? color : UiColors.GetColorModelByExpansion(color.Expansion);
            }
        }

        private TitleScreenLogo TitleScreenLogoOption
        {
            get
            {
                if (titleScreenLocationModel.TitleScreenLogo == TitleScreenLogo.Unspecified ||
                    Services.ConfigurationService.OverridePresetTitleScreenLogo)
                {
                    return Services.ConfigurationService.TitleScreenLogo;
                }
                else
                {
                    return titleScreenLocationModel.TitleScreenLogo;
                }
            }
        }

        private void HookTitle()
        {
            //Some struct holding information about title screen cutscene that plays on DT title
            titleCutsceneStructAddress = Services.SigScanner.GetStaticAddressFromSig("48 8D 05 ?? ?? ?? ?? 48 83 C4 ?? C3 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 83 3D ?? ?? ?? ?? ?? 75 ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ?? C7 05");

            pickTitleLogoHook = Hook<PickTitleLogo>("40 57 48 83 EC ?? 48 8B F9 E8 ?? ?? ?? ?? 80 78", PickTitleLogoDetour);


            // Used when unloading TitleScreen menu
            Services.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "_TitleMenu", TitleMenuFinalize);
            // To animate/skip animation of title logo
            Services.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "_TitleLogo", TitleLogoPostSetup);

            Services.AddonLifecycle.RegisterListener(AddonEvent.PreSetup, "_TitleLogo", TitleLogoPreSetup);
            // Register UI stuff we want to recolor
            foreach (var addon in recolorableAddons)
            {
                Services.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, addon, RecolorablePostSetup);
            }
        }

        private void TitleLogoPreSetup(AddonEvent type, AddonArgs args)
        {
            var addon = (AtkUnitBase*)Services.GameGui.GetAddonByName("_TitleLogo");
            if (addon == null || addon->UldManager.NodeListCount < 3) return;
            var node = (AtkImageNode*)addon->UldManager.NodeList[2];
            if (node == null) return;
            Services.Log.Debug($"TitleLogo image pre setup part id {node->PartId}");
        }

        private void TickTitle()
        {
            if (!TitleCutsceneIsLoaded && lastCutsceneStatus)
            {
                foreach (var action in cutsceneStoppedActions)
                {
                    action();
                }
                cutsceneStoppedActions.Clear();
            }
            lastCutsceneStatus = TitleCutsceneIsLoaded;
        }

        private void TitleLogoPostSetup(AddonEvent type, AddonArgs args)
        {
            if (shouldAnimateDawntrailLogo)
            {
                AnimateDawntrailLogo();
                shouldAnimateDawntrailLogo = false;
            }
            if (shouldAdvanceTitleLogoAnimation)
            {
                AdvanceTitleLogoAnimation();
                shouldAdvanceTitleLogoAnimation = false;
            }
            // Title screen logos have two parts one with japanese subtitle and one without
            // They all default to part 1 which is the english version even on client set to JP language
            // but shb one won't if the title isn't set to shb
            // it's set from some updateAnimation call somewhere in the setup process
            // we just manually set it here because I didn't want to figure that out
            if (ShouldModifyTitleScreen && TitleScreenLogoOption == TitleScreenLogo.Shadowbringers)
            {
                SetTitleScreenLogoPartId();
            }
        }

        private void SetTitleScreenLogoPartId()
        {
            // Iterating through all nodes cause there's like 5 image nodes used in the animation for shadowbringers
            var addon = (AtkUnitBase*)Services.GameGui.GetAddonByName("_TitleLogo");
            Utils.IterateNodes(addon->RootNode, node =>
            {
                if (node->Type == NodeType.Image)
                {
                    var imageNode = (AtkImageNode*)node;
                    imageNode->PartId = 1;
                }
            });
        }

        private void AnimateDawntrailLogo()
        {
            var addon = (AtkUnitBase*)Services.GameGui.GetAddonByName("_TitleLogo");
            if (addon == null || addon->UldManager.NodeListCount < 2) return;
            var node = addon->UldManager.NodeList[0];
            if (node == null) return;
            Services.Log.Debug("Animating dawntrail logo");
            // Values taken by observing what the game calls, no clue what they mean :)
            node->Timeline->PlayAnimation(AtkTimelineJumpBehavior.LoopForever, 0x65);
        }
        private void AdvanceTitleLogoAnimation()
        {
            var addon = (AtkUnitBase*)Services.GameGui.GetAddonByName("_TitleLogo");
            if (addon == null || addon->UldManager.NodeListCount < 2) return;
            var node = addon->UldManager.NodeList[0];
            if (node == null) return;
            Services.Log.Debug($"Advancing timeline animation by {advanceTitleLogoAnimationDuration}");
            // Values taken by observing what the game calls, no clue what they mean :)
            node->Timeline->FrameTime += advanceTitleLogoAnimationDuration;
        }

        private void RecolorablePostSetup(AddonEvent type, AddonArgs args)
        {
            if (ShouldModifyTitleScreen)
            {
                RecolorAddon((AtkUnitBase*)args.Addon);
            }
        }

        private void RecolorAddon(AtkUnitBase* addon)
        {
            if (addon != null && addon->RootNode != null)
            {
                Utils.IterateNodes(addon->RootNode, (node) =>
                {
                    if (node->Type == NodeType.Text)
                    {
                        Services.Log.Debug($"[RecolorablePostSetup] Recoloring text {(IntPtr)node:X} to {TitleScreenColors.Color} {TitleScreenColors.EdgeColor}");
                        var textNode = (AtkTextNode*)node;
                        textNode->TextColor = TitleScreenColors.Color.ToByteColor();
                        textNode->EdgeColor = TitleScreenColors.EdgeColor.ToByteColor();
                    }
                    else if (node->Type == NodeType.NineGrid)
                    {
                        var nineGridNode = (AtkNineGridNode*)node;
                        Services.Log.Debug($"[RecolorablePostSetup] Recoloring ninegrid {(IntPtr)node:X} to {TitleScreenColors.HighlightColor}");
                        var color = TitleScreenColors.HighlightColor - UiColors.HighlightColorApproximation;
                        nineGridNode->AddRed = nineGridNode->AddRed_2 = (short)(color.X * 255);
                        nineGridNode->AddGreen = nineGridNode->AddGreen_2 = (short)(color.Y * 255);
                        nineGridNode->AddBlue = nineGridNode->AddBlue_2 = (short)(color.Z * 255);
                        nineGridNode->Color.A = (byte)(color.W * 255);

                    }
                });
            }
        }

        private bool PickTitleLogoDetour(IntPtr atkUnit)
        {
            var addon = (AtkUnitBase*)atkUnit;
            if (!(addon->UldManager.NodeListCount < 3))
            {

                var node = (AtkImageNode*)addon->UldManager.NodeList[2];
                if (!(node == null))
                {
                    Services.Log.Debug($"TitleLogo image PickTitleLogoDetour part id {node->PartId}");
                }
                else
                {
                    Services.Log.Debug($"no image node PickTitleLogoDetour");

                }

            }
            else
            {
                Services.Log.Debug($"no addon {addon->UldManager.NodeListCount}");

            }

            // have to set it to false or some logos won't load because ???
            LobbyInfo->PreDawntrailLogoFlag = false;
            Services.Log.Debug($"[PickTitleLogoDetour] {atkUnit:X}");
            if (!ShouldModifyTitleScreen)
            {
                return pickTitleLogoHook.Original(atkUnit);
            }
            bool currentFreeTrial = LobbyInfo->FreeTrial;
            TitleScreenExpansion currentTitleScreenType = LobbyInfo->CurrentTitleScreenType;
            if (TitleScreenLogoOption == TitleScreenLogo.None)
            {
                // Don't load anything at all
                return false;
            }
            else if (TitleScreenLogoOption == TitleScreenLogo.FreeTrial)
            {
                LobbyInfo->FreeTrial = true;
            }
            else
            {
                LobbyInfo->CurrentTitleScreenType = TitleScreenLogoOption switch
                {
                    TitleScreenLogo.ARealmReborn => TitleScreenExpansion.ARealmReborn,
                    TitleScreenLogo.Heavensward => TitleScreenExpansion.Heavensward,
                    TitleScreenLogo.Stormblood => TitleScreenExpansion.Stormblood,
                    TitleScreenLogo.Shadowbringers => TitleScreenExpansion.Shadowbringers,
                    TitleScreenLogo.Endwalker => TitleScreenExpansion.Endwalker,
                    TitleScreenLogo.Dawntrail => TitleScreenExpansion.Dawntrail,
                    _ => LobbyInfo->CurrentTitleScreenType
                };
                if (LobbyInfo->CurrentTitleScreenType == TitleScreenExpansion.Stormblood)
                {
                    shouldAdvanceTitleLogoAnimation = true;
                    advanceTitleLogoAnimationDuration = 1.2f;
                }
                else if (LobbyInfo->CurrentTitleScreenType == TitleScreenExpansion.Endwalker)
                {
                    shouldAdvanceTitleLogoAnimation = true;
                    advanceTitleLogoAnimationDuration = 9.8f;
                }
                else if (LobbyInfo->CurrentTitleScreenType == TitleScreenExpansion.Dawntrail)
                {
                    shouldAnimateDawntrailLogo = true;
                }
            }

            Services.Log.Debug($"[PickTitleLogoDetour] set {LobbyInfo->CurrentTitleScreenType} {LobbyInfo->FreeTrial}");
            bool result = pickTitleLogoHook.Original(atkUnit);
            LobbyInfo->FreeTrial = currentFreeTrial;
            LobbyInfo->CurrentTitleScreenType = currentTitleScreenType;
            return result;
        }

        private void EnteringTitleScreen()
        {
            titleScreenLocationModel = GetTitleLocation();
            titleScreenLoaded = true;
            Services.Log.Debug($"[EnteringTitleScreen] Got title screen {titleScreenLocationModel.TerritoryPath}");
            if (titleScreenLocationModel.TitleScreenOverride != null)
            {
                overridenTitleScreenType = LobbyInfo->CurrentTitleScreenType;
                LobbyInfo->CurrentTitleScreenType = titleScreenLocationModel.TitleScreenOverride.Value;
            }
            else if (LobbyInfo->CurrentTitleScreenType == TitleScreenExpansion.Dawntrail)
            {
                overridenTitleScreenType = LobbyInfo->CurrentTitleScreenType;
                LobbyInfo->CurrentTitleScreenType = TitleScreenExpansion.Endwalker;
            }
        }

        // Restore the overriden title screen and movie
        private void LeavingTitleScreen(bool idled)
        {
            if (overridenTitleScreenType != null)
            {
                if (LobbyUiStage != LobbyUiStage.Movie || idled)
                {
                    Services.Log.Debug($"[LeavingTitleScreen] restoring {overridenTitleScreenType}, {AgentLobby->IdleTime}");
                    LobbyInfo->CurrentTitleScreenType = overridenTitleScreenType.Value;
                    if (idled)
                    {
                        LobbyInfo->CurrentTitleScreenMovieType = LobbyInfo->CurrentTitleScreenType switch
                        {
                            TitleScreenExpansion.ARealmReborn => TitleScreenMovie.ARealmReborn,
                            TitleScreenExpansion.Heavensward => TitleScreenMovie.Heavensward,
                            TitleScreenExpansion.Stormblood => TitleScreenMovie.Stormblood,
                            TitleScreenExpansion.Shadowbringers => TitleScreenMovie.Shadowbringers,
                            TitleScreenExpansion.Endwalker => TitleScreenMovie.Endwalker,
                            TitleScreenExpansion.Dawntrail => TitleScreenMovie.Dawntrail,
                            _ => TitleScreenMovie.ARealmReborn
                        };
                        Services.Log.Debug($"[LeavingTitleScreen] set current movie to {LobbyInfo->CurrentTitleScreenMovieType}");
                    }
                }
                overridenTitleScreenType = null;
            }
        }

        // Before a reload happens the UI needs to be unloaded and the cutscene if it's DT title screen
        public void ReloadTitleScreen()
        {
            reloadingTitleScreen = true;
            OnTitleMenuFinalize(() =>
            {
                OnCutsceneStopped(ExecuteTitleScreenReload);
                Services.LobbyService.StopCutscene();
            });
            removeTitleScreenUi(AgentLobby);
        }

        public void ReloadTitleScreenUi()
        {
            if (reloadingTitleScreen) return; // if a full reload is in progress ignore this call
            reloadingTitleScreenUi = true;
            OnTitleMenuFinalize(ExecuteTitleScreenUiReload);
            removeTitleScreenUi(AgentLobby);
        }

        public void RecolorTitleScreenUi()
        {
            foreach (var addon in recolorableAddons)
            {
                RecolorAddon((AtkUnitBase*)Services.GameGui.GetAddonByName(addon));
            }
        }

        //Execute action if _TitleMenu is unloaded or when it cleans up
        private void OnTitleMenuFinalize(Action action)
        {
            if (Services.GameGui.GetAddonByName("_TitleMenu") != IntPtr.Zero)
            {
                titleMenuFinalizeActions.Enqueue(action);
            }
            else
            {
                action();
            }
        }

        //Execute action if cutscene is unloaded or when it cleans up
        private void OnCutsceneStopped(Action action)
        {
            if (TitleCutsceneIsLoaded)
            {
                cutsceneStoppedActions.Enqueue(action);
            }
            else
            {
                action();
            }
        }

        private void StopCutscene()
        {
            if (TitleCutsceneIsLoaded)
            {
                // at offset 30 is the cutscene path which I assume acts as a key?
                cancelScheduledTask(ScheduleManagement.Instance(), *(IntPtr*)(titleCutsceneStructAddress + 0x30));
            }
        }

        private void TitleMenuFinalize(AddonEvent type, AddonArgs args)
        {
            foreach (var action in titleMenuFinalizeActions)
            {
                action();
            }
            titleMenuFinalizeActions.Clear();
        }

        private void ExecuteTitleScreenReload()
        {
            LobbyUiStage = LobbyUiStage.InitialLobbyLoading;
            LobbyInfo->CurrentLobbyMusicIndex = LobbySong.None;
            reloadingTitleScreen = false;
            reloadingTitleScreenUi = false; // just in case
        }

        private void ExecuteTitleScreenUiReload()
        {
            LobbyUiStage = LobbyUiStage.LoadingTitleScreen2;
            reloadingTitleScreenUi = false;
        }

        private void DisposeTitle()
        {
            LeavingTitleScreen(false);
            Services.AddonLifecycle.UnregisterListener(TitleMenuFinalize);
            Services.AddonLifecycle.UnregisterListener(RecolorablePostSetup);
            Services.AddonLifecycle.UnregisterListener(TitleLogoPostSetup);
        }
    }
}
