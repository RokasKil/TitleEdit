using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System;
using System.Runtime.InteropServices;
using TitleEdit.Data.Lobby;
using TitleEdit.Utility;

namespace TitleEdit.PluginServices.Lobby
{
    public unsafe partial class LobbyService
    {


        [Signature("E8 ?? ?? ?? ?? 48 8B CF E8 ?? ?? ?? ?? 48 8B CF E8 ?? ?? ?? ?? E8")]
        private readonly delegate* unmanaged<AgentLobby*, void> removeTitleScreenUi = null!;

        [Signature("E8 ?? ?? ?? ?? 48 8D 4F ?? 44 89 77")]
        private readonly delegate* unmanaged<IntPtr, IntPtr, void> cancelScheduledTask = null!;

        public bool TitleCutsceneIsLoaded
        {
            get => Marshal.ReadByte(titleCutsceneStructAddress, 0x98) == 1;
            set => Marshal.WriteInt32(titleCutsceneStructAddress, 0x98, value ? 1 : 0);
        }

        // This needs to be set to 0 for logos to load properly, haven't seen it being read anywhere else
        public bool PreDawntrailLogoFlag
        {
            get => Marshal.ReadByte(lobbyStructAddress, 0x38) == 1;
            set => Marshal.WriteInt32(lobbyStructAddress, 0x38, value ? 1 : 0);
        }

        public TitleScreenExpansion CurrentTitleScreenType
        {
            get => (TitleScreenExpansion)Marshal.ReadInt32(lobbyStructAddress, 0x34);
            set => Marshal.WriteInt32(lobbyStructAddress, 0x34, (int)value);
        }


        private IntPtr titleCutsceneStructAddress;

        private void HookTitle()
        {
            //Some struct holding information about title screen cutscene that plays on DT title
            titleCutsceneStructAddress = Services.SigScanner.GetStaticAddressFromSig("48 8D 05 ?? ?? ?? ?? 48 83 C4 ?? C3 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 83 3D ?? ?? ?? ?? ?? 75 ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ?? C7 05");

            // Used when unloading TitleScreen menu
            Services.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "_TitleLogo", TitleLogoFinalize);
        }

        private void EnteringTitleScreen()
        {
            titleScreenLocationModel = GetTitleLocation();
            Services.Log.Debug($"Got title screen {titleScreenLocationModel.TerritoryPath}");
            // have to set it to false or some logos won't load because ???
            PreDawntrailLogoFlag = false;
            if (titleScreenLocationModel.TitleScreenOverride != null)
            {
                CurrentTitleScreenType = titleScreenLocationModel.TitleScreenOverride.Value;
            }
            else if (CurrentTitleScreenType == TitleScreenExpansion.Dawntrail)
            {
                CurrentTitleScreenType = TitleScreenExpansion.Endwalker;
            }
        }

        private void TitleLogoFinalize(AddonEvent type, AddonArgs args)
        {
            Services.Log.Debug("TitleLogo PreFinalize");
        }

        private void DisposeTitle()
        {
            Services.AddonLifecycle.UnregisterListener(TitleLogoFinalize);
        }
    }
}
