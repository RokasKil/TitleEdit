using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.Scheduler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using TitleEdit.Data.BGM;
using TitleEdit.Data.Lobby;
using TitleEdit.Utility;

namespace TitleEdit.PluginServices.Lobby
{
    public unsafe partial class LobbyService
    {
        [Signature("E8 ?? ?? ?? ?? 48 8D 4F ?? 44 89 77")]
        private readonly delegate* unmanaged<ScheduleManagement*, IntPtr, void> cancelScheduledTask = null!;

        //Some struct holding information about title screen cutscene that plays on DT title
        private IntPtr titleCutsceneStructAddress;

        // Was cutscene loaded/loading last frame
        private bool lastCutsceneStatus = false;

        // what title screen we overrode with current location model
        // Used to restore on exit or when idling triggers a movie
        private TitleScreenExpansion? overridenTitleScreenType = null;

        // Have we loaded something yet, should we modify camera and other stuff
        private bool titleScreenLoaded = false;

        // Called to check if we need to modify anything, if TitleScreenOverride is set we let the game handle things
        private bool ShouldModifyTitleScreen => titleScreenLoaded && titleScreenLocationModel.TitleScreenOverride == null;

        // Are we reloading title screen
        private bool reloadingTitleScreen = false;

        // Should we reload once we hit LobbyUIStage.TitleScreenOnLoadingStage2 since it's not safe to do so on TitleScreenOnLoadingStage1
        private bool shouldReloadTitleScreenOnLoadingStage2 = false;

        private Queue<Action> cutsceneStoppedActions = [];

        public bool CanReloadTitleScreen => (new LobbyUiStage[] {
            //LobbyUiStage.LoadingSplashScreen,
            //LobbyUiStage.EnteringTitleScreen,
            //LobbyUiStage.LoadingTitleScreen1,
            LobbyUiStage.LoadingTitleScreen2,
            LobbyUiStage.TitleScreen,
            //LobbyUiStage.LoadingDataCenter,
            LobbyUiStage.TitleScreenMovies,
            LobbyUiStage.TitleScreenOptions,
            LobbyUiStage.TitleScreenLicense,
            LobbyUiStage.TitleScreenConfiguration,
            LobbyUiStage.TitleScreenInstallationDetails
        }).Contains(LobbyUiStage);

        public bool TitleCutsceneIsLoaded
        {
            get => Marshal.ReadByte(titleCutsceneStructAddress, 0x98) == 1;
            set => Marshal.WriteInt32(titleCutsceneStructAddress, 0x98, value ? 1 : 0);
        }

        private void ScanTitleAddressess()
        {
            //Some struct holding information about title screen cutscene that plays on DT title
            titleCutsceneStructAddress = Services.SigScanner.GetStaticAddressFromSig("48 8D 05 ?? ?? ?? ?? 48 83 C4 ?? C3 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 83 3D ?? ?? ?? ?? ?? 75 ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ?? C7 05");
        }

        private void InitTitle()
        {
            lastCutsceneStatus = TitleCutsceneIsLoaded;
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
        public void ReloadTitleScreen(bool force = false)
        {
            if (CanReloadTitleScreen || force)
            {
                Services.Log.Debug("[ReloadTitleScreen] reloading");
                reloadingTitleScreen = true;
                OnCutsceneStopped(() =>
                {
                    Services.Log.Debug("[OnTitleMenuFinalize] OnTitleMenuFinalize");
                    OnTitleMenuFinalize(ExecuteTitleScreenReload);
                    removeTitleScreenUi(AgentLobby);
                });
                Services.LobbyService.StopCutscene();
            }
            else if (LobbyUiStage == LobbyUiStage.LoadingTitleScreen1)
            {
                Services.Log.Debug("[ReloadTitleScreen] shouldReloadTitleScreenOnLoadingStage2 = true");
                shouldReloadTitleScreenOnLoadingStage2 = true;
            }
        }

        //Execute action if cutscene is unloaded or when it cleans up
        private void OnCutsceneStopped(Action action)
        {
            if (TitleCutsceneIsLoaded)
            {
                Services.Log.Debug("[OnCutsceneStopped] queueing");
                cutsceneStoppedActions.Enqueue(action);
            }
            else
            {
                Services.Log.Debug("[OnCutsceneStopped] running instant");
                action();
            }
        }

        private void StopCutscene()
        {
            if (TitleCutsceneIsLoaded)
            {
                Services.Log.Debug("[StopCutscene] stopping");
                // at offset 30 is the cutscene path which I assume acts as a key?
                cancelScheduledTask(ScheduleManagement.Instance(), *(IntPtr*)(titleCutsceneStructAddress + 0x30));
            }
        }

        private void ExecuteTitleScreenReload()
        {
            Services.Log.Debug("[ExecuteTitleScreenReload] reloading");
            LobbyUiStage = LobbyUiStage.InitialLobbyLoading;
            LobbyInfo->CurrentLobbyMusicIndex = LobbySong.None;
            reloadingTitleScreen = false;
            reloadingTitleScreenUi = false; // just in case
        }

        private void DisposeTitle()
        {
            LeavingTitleScreen(false);
        }
    }
}
