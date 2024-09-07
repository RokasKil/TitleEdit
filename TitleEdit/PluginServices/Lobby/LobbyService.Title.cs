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

        private IntPtr titleCutsceneStructAddress;

        private bool lastCutsceneStatus = false;

        private TitleScreenExpansion? overridenTitleScreenType = null;

        private bool titleScreenLoaded = false;

        private bool ShouldModifyTitleScreen => titleScreenLoaded && titleScreenLocationModel.TitleScreenOverride == null;

        private bool reloadingTitleScreen = false;

        private bool shouldReloadTitleScreenOnLoadingStage2 = false;

        private Queue<Action> cutsceneStoppedActions = [];

        private bool CanReloadTitleScreen => (new LobbyUiStage[] {
            //LobbyUiStage.LoadingSplashScreen,
            //LobbyUiStage.EnteringTitleScreen,
            //LobbyUiStage.LoadingTitleScreen1,
            LobbyUiStage.LoadingTitleScreen2,
            LobbyUiStage.TitleScreen,
            LobbyUiStage.LoadingDataCenter,
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

        private void HookTitle()
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
                OnTitleMenuFinalize(() =>
                {
                    Services.Log.Debug("[OnTitleMenuFinalize] OnTitleMenuFinalize");
                    OnCutsceneStopped(ExecuteTitleScreenReload);
                    Services.LobbyService.StopCutscene();
                });
                removeTitleScreenUi(AgentLobby);
            }
            else if (LobbyUiStage == LobbyUiStage.LoadingTitleScreen1)
            {
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
