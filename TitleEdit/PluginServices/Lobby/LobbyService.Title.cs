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

        //probably should merge all the lobbyStruct stuff into an actual struct?

        // This needs to be set to 0 for logos to load properly, haven't seen it being read anywhere else
        public bool PreDawntrailLogoFlag
        {
            get => Marshal.ReadByte(lobbyStructAddress, 0x38) == 1;
            set => Marshal.WriteByte(lobbyStructAddress, 0x38, (byte)(value ? 1 : 0));
        }

        // What cinematic in title screen is currently playing
        public TitleScreenMovie CurrentTitleScreenMovieType
        {
            get => (TitleScreenMovie)Marshal.ReadInt32(lobbyStructAddress, 0x10);
            set => Marshal.WriteInt32(lobbyStructAddress, 0x10, (int)value);
        }

        public TitleScreenExpansion CurrentTitleScreenType
        {
            get => (TitleScreenExpansion)Marshal.ReadInt32(lobbyStructAddress, 0x34);
            set => Marshal.WriteInt32(lobbyStructAddress, 0x34, (int)value);
        }


        private IntPtr titleCutsceneStructAddress;

        private TitleScreenExpansion? overridenTitleScreenType = null;

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
                overridenTitleScreenType = CurrentTitleScreenType;
                CurrentTitleScreenType = titleScreenLocationModel.TitleScreenOverride.Value;
            }
            else if (CurrentTitleScreenType == TitleScreenExpansion.Dawntrail)
            {
                overridenTitleScreenType = CurrentTitleScreenType;
                CurrentTitleScreenType = TitleScreenExpansion.Endwalker;
            }
        }

        // Restore the overriden title screen and movie
        private void LeavingTitleScreen(bool idled)
        {
            if (overridenTitleScreenType != null)
            {
                if (LobbyUiStage != LobbyUiStage.Movie || idled)
                {
                    Services.Log.Debug($"LeavingTitleScreen, restoring {overridenTitleScreenType}, {AgentLobby->IdleTime}");
                    CurrentTitleScreenType = overridenTitleScreenType.Value;
                    if (idled)
                    {
                        Services.Log.Debug($"LeavingTitleScreen set current movie");
                        CurrentTitleScreenMovieType = CurrentTitleScreenType switch
                        {
                            TitleScreenExpansion.ARealmReborn => TitleScreenMovie.ARealmReborn,
                            TitleScreenExpansion.Heavensward => TitleScreenMovie.Heavensward,
                            TitleScreenExpansion.Stormblood => TitleScreenMovie.Stormblood,
                            TitleScreenExpansion.Shadowbringers => TitleScreenMovie.Shadowbringers,
                            TitleScreenExpansion.Endwalker => TitleScreenMovie.Endwalker,
                            TitleScreenExpansion.Dawntrail => TitleScreenMovie.Dawntrail,
                            _ => TitleScreenMovie.ARealmReborn
                        };
                    }
                }
                overridenTitleScreenType = null;
            }
        }

        private void TitleLogoFinalize(AddonEvent type, AddonArgs args)
        {
            Services.Log.Debug("TitleLogo PreFinalize");
        }

        private void DisposeTitle()
        {
            LeavingTitleScreen(false);
            Services.AddonLifecycle.UnregisterListener(TitleLogoFinalize);
        }
    }
}
