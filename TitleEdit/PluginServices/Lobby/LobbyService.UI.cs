using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using Dalamud.Utility.Numerics;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using TitleEdit.Data.Lobby;
using TitleEdit.Data.Persistence;
using TitleEdit.Utility;

namespace TitleEdit.PluginServices.Lobby
{
    public unsafe partial class LobbyService
    {
        [Signature("E8 ?? ?? ?? ?? 48 8B CF E8 ?? ?? ?? ?? 48 8B CF E8 ?? ?? ?? ?? E8")]
        private readonly delegate* unmanaged<AgentLobby*, void> removeTitleScreenUi = null!;
        private readonly string[] recolorableAddons = { "_TitleRights", "_TitleMenu", "_TitleRevision" };

        private delegate bool PickTitleLogo(IntPtr atkUnit);

        private Hook<PickTitleLogo> pickTitleLogoHook = null!;

        private bool shouldAnimateDawntrailLogo = false;

        private bool shouldAdvanceTitleLogoAnimation = false;
        private float advanceTitleLogoAnimationDuration = 0;

        private bool reloadingTitleScreenUi = false;

        private Queue<Action> titleMenuFinalizeActions = [];

        //These might be inneficient but we don't call them often so maybe it's fine or maybe it's smart and caches but I doubt
        private UiColorModel TitleScreenColors
        {
            get
            {
                UiColorModel color;
                if (titleScreenLocationModel.UiColor.Expansion == UiColorExpansion.Unspecified ||
                    (Services.ConfigurationService.OverridePresetTitleScreenColor && !liveEditTitleScreen))
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
                    (Services.ConfigurationService.OverridePresetTitleScreenLogo && !liveEditTitleScreen))
                {
                    return Services.ConfigurationService.TitleScreenLogo;
                }
                else
                {
                    return titleScreenLocationModel.TitleScreenLogo;
                }
            }
        }

        private void HookUi()
        {
            pickTitleLogoHook = Hook<PickTitleLogo>("40 57 48 83 EC ?? 48 8B F9 E8 ?? ?? ?? ?? 80 78", PickTitleLogoDetour);


            // Used when unloading TitleScreen menu
            Services.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "_TitleMenu", TitleMenuFinalize);
            // To animate/skip animation of title logo
            Services.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "_TitleLogo", TitleLogoPostSetup);

            // Register UI stuff we want to recolor
            foreach (var addon in recolorableAddons)
            {
                Services.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, addon, RecolorablePostSetup);
            }
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

        public void ReloadTitleScreenUi(bool force = false)
        {
            if ((CanReloadTitleScreen && !reloadingTitleScreenUi) || force)
            {
                if (reloadingTitleScreen) return; // if a full reload is in progress ignore this call
                reloadingTitleScreenUi = true;
                OnTitleMenuFinalize(ExecuteTitleScreenUiReload);
                removeTitleScreenUi(AgentLobby);
            }
        }

        public void RecolorTitleScreenUi(bool force = false)
        {
            if (CanReloadTitleScreen || force)
            {
                foreach (var addon in recolorableAddons)
                {
                    RecolorAddon((AtkUnitBase*)Services.GameGui.GetAddonByName(addon));
                }
            }
        }

        //Execute action if _TitleMenu is unloaded or when it cleans up
        private void OnTitleMenuFinalize(Action action)
        {
            if (Services.GameGui.GetAddonByName("_TitleMenu") != IntPtr.Zero)
            {
                Services.Log.Debug("[OnTitleMenuFinalize] queueing");
                titleMenuFinalizeActions.Enqueue(action);
            }
            else
            {
                Services.Log.Debug("[OnTitleMenuFinalize] running instant");
                action();
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

        private void ExecuteTitleScreenUiReload()
        {
            LobbyUiStage = LobbyUiStage.LoadingTitleScreen2;
            reloadingTitleScreenUi = false;
        }

        private void DisposeUi()
        {
            Services.AddonLifecycle.UnregisterListener(TitleMenuFinalize);
            Services.AddonLifecycle.UnregisterListener(RecolorablePostSetup);
            Services.AddonLifecycle.UnregisterListener(TitleLogoPostSetup);
        }
    }
}
