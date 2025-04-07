using Dalamud.Hooking;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using TitleEdit.Data.BGM;
using TitleEdit.Data.Layout;
using TitleEdit.Data.Lobby;
using TitleEdit.Data.Persistence;
using TitleEdit.Utility;

namespace TitleEdit.PluginServices.Lobby
{
    public unsafe partial class LobbyService : AbstractService
    {
        // Magic numbers that we use for default look at curve values when character is not available or need to reset it
        private static readonly Vector3 LookAtCurveMagicNumbers = new(1.4350828f, 0.85870504f, 0.6742642f);

        public AgentLobby* AgentLobby => FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentLobby.Instance();

        private delegate int CreateSceneDelegate(string territoryPath, uint territoryId, nint p3, uint layerFilterKey, nint festivals, int p6, uint contentFinderConditionId);

        private delegate byte LobbyUpdateDelegate(GameLobbyType mapId, int time);

        private delegate void LoadLobbyScene(GameLobbyType mapId);

        private delegate void UpdateLobbyUiStage(AgentLobby* agentLobby);

        private Hook<CreateSceneDelegate> createSceneHook = null!;
        private Hook<LobbyUpdateDelegate> lobbyUpdateHook = null!;
        private Hook<LoadLobbyScene> loadLobbySceneHook = null!;
        private Hook<UpdateLobbyUiStage> updateLobbyUiStageHook = null!;

        private nint lobbyStructAddress;
        private GameLobbyType* lobbyCurrentMapAddress;

        private nint layoutEventStructAddress;

        // Data used when loading new scenes and clearing the modification of old ones
        private GameLobbyType lastLobbyUpdateMapId = GameLobbyType.None;
        private GameLobbyType lastSceneType = GameLobbyType.None;
        private GameLobbyType loadingLobbyType = GameLobbyType.None;
        private GameLobbyType LobbyType => loadingLobbyType == GameLobbyType.None ? lastLobbyUpdateMapId : loadingLobbyType;

        // Should reload character select scene
        private bool resetCharacterSelectScene = false;


        // last and current LobbyUiStage, enum made based on my observation
        private LobbyUiStage lastLobbyUiStage = 0;

        public LobbyUiStage LobbyUiStage
        {
            get => (LobbyUiStage)AgentLobby->LobbyUIStage;
            set => AgentLobby->LobbyUIStage = (byte)value;
        }

        // variable showing the current Lobby type
        public GameLobbyType CurrentLobbyMap
        {
            get => *lobbyCurrentMapAddress;
            set => *lobbyCurrentMapAddress = value;
        }

        // Probably some lobby instance
        public LobbyInfo* LobbyInfo => (LobbyInfo*)lobbyStructAddress;

        // LayoutEventHandler struct
        public LayoutEventStruct* LayoutEventStruct => *(LayoutEventStruct**)layoutEventStructAddress;

        private bool initComplete;

        public LobbyService()
        {
            Services.GameInteropProvider.InitializeFromAttributes(this);

            // Points to a struct with a value that indicates the current lobby bgm and current title screen type (among other things)
            // No direct usages left, signature points to the getter and offset is to the LEA call
            lobbyStructAddress = Services.SigScanner.GetStaticAddressFromSig("E8 ?? ?? ?? ?? 33 ED BF", 0x60);

            // Points to a Value that says what Type of lobby map is being displayer
            lobbyCurrentMapAddress = (GameLobbyType*)Services.SigScanner.GetStaticAddressFromSig("0F B7 05 ?? ?? ?? ?? 48 8B CE");

            // After 7.2 patch if your game starts on a Solution 9 preset you will crash on login because
            // the game creates some EventHandlers and then tries to clean them up but fails in some way
            // I don't have the time to properly research it so don't know if the EventHandlers are new
            // or the cleanup failure is new, but something obviously has changed
            // This struct hold information about those EventHandlers including a flag if they're already loaded
            // We set that flag when we load a lobby scene to prevent them from ever existing and needing a cleanup
            // I don't know if this is a proper fix or if something else is wrong but this should prevent further crashes
            // And I'll maybe cycle back once I have the time to properly work on the plugin again
            layoutEventStructAddress = Services.SigScanner.GetStaticAddressFromSig("48 83 3D ?? ?? ?? ?? ?? 0F 85 ?? ?? ?? ?? 33 D2 48 89 5C 24 ?? 45 33 C0 8D 4A ?? E8 ?? ?? ?? ?? 48 8B D8 48 85 C0 0F 84 ?? ?? ?? ?? 33 C0 48 89 7C 24 ?? 33 D2 48 89 03 41 B8 ?? ?? ?? ?? 48 89 43 ?? 48 89 43 ?? 8D 48 ?? 66 89 43");
            ScanTitleAddressess();
        }

        public override void Init()
        {
            // Called when creating a new scene in lobby (main menu, character select, character creation) - Used to switch out the level that loads and reset stuff
            createSceneHook = Hook<CreateSceneDelegate>("E8 ?? ?? ?? ?? 66 89 1D ?? ?? ?? ?? E9 ?? ?? ?? ??", CreateSceneDetour);

            // Lobby manager update (I think) - we use this as a point to change the Value at lobbyCurrentMapAddress to reload the scene
            lobbyUpdateHook = Hook<LobbyUpdateDelegate>("40 56 57 41 56 48 81 EC ?? ?? ?? ?? 8B F9", LobbyUpdateDetour);

            // Called when going from title to char select or reverse
            // Important because in those scenarios the game first loads the level and only then sets CurrentLobbyMap value so we can't rely on that
            loadLobbySceneHook = Hook<LoadLobbyScene>("E8 ?? ?? ?? ?? B0 ?? 88 86", LoadLobbySceneDetour);

            updateLobbyUiStageHook = Hook<UpdateLobbyUiStage>(FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentLobby.Addresses.UpdateLobbyUIStage.String, UpdateLobbyUiStageDetour);

            HookLayout();
            HookCharacter();
            HookCamera();
            HookSong();
            HookUi();
            HookNpcs();

            InitTitle();
            EnableHooks();
            Services.Framework.Update += Tick;
            if (CurrentCharacter != null)
            {
                characterSelectLocationModel = GetLocationForContentId(CurrentContentId);
            }
            else
            {
                characterSelectLocationModel = GetNothingSelectedLocation();
            }

            titleScreenLocationModel = GetTitleLocation();
            Services.Log.Debug($"Initializing LobbyService {CurrentLobbyMap} {LobbyUiStage} {LobbyInfo->CurrentTitleScreenType} {LobbyInfo->FreeTrial}");
            if (CurrentLobbyMap == GameLobbyType.CharaSelect)
            {
                resetCharacterSelectScene = true;
                // Forcing a character update on layout load to set positions and stuff
                // Will be missing mount if there was no companion object created which is probably the case unless the plugin is being turned off and on again
                // I will not be fixing that cause fuck it
                forceUpdateCharacter = CurrentCharacter != null;
            }
            // LobbyUiStage is CommonBaseStage (1) when splash screen is being shown for some reason so we look at the addon
            else if ((Services.GameGui.GetAddonByName("Logo") != IntPtr.Zero && LobbyUiStage == LobbyUiStage.CommonBaseStage) ||
                     LobbyUiStage == LobbyUiStage.EnteringTitleScreen ||
                     LobbyUiStage == LobbyUiStage.LoadingSplashScreen)
            {
                EnteringTitleScreen();
            }
            else if (CanReloadTitleScreen || LobbyUiStage == LobbyUiStage.LoadingTitleScreen1)
            {
                ReloadTitleScreen();
            }

            initComplete = true;
        }

        // Called when going from title to char select or reverse
        // Important because in those scenarios the game first loads the level and only then sets CurrentLobbyMap value so we can't rely on that
        private void LoadLobbySceneDetour(GameLobbyType mapId)
        {
            Services.Log.Debug($"LoadLobbySceneDetour {mapId}");
            ResetCameraLookAtOnExitCharacterSelect();
            loadingLobbyType = mapId;
            loadLobbySceneHook.Original(mapId);
        }

        private void Tick(IFramework framework)
        {
            CharacterTick();
            CameraTick();
            if (CurrentLobbyMap == GameLobbyType.CharaSelect)
            {
                ModifyCamera();
            }
            else if (CurrentLobbyMap != GameLobbyType.None)
            {
                ClearCameraModifications();
                ClearCharacterSelectGroupCache();
            }

            TickTitle();
            TickNpcs();
            TickUi();
        }

        // Called when creating a new scene in lobby (main menu, character select, character creation) - Used to switch out the level that loads and reset stuff
        private int CreateSceneDetour(string territoryPath, uint territoryId, nint p3, uint layerFilterKey, nint festivals, int p6, uint contentFinderConditionId)
        {
            try
            {
                Services.Log.Debug($"Loading Scene {LobbyType}");
                Services.Log.Debug($"[CreateSceneDetour] {territoryPath} {territoryId} {p3} {layerFilterKey} {festivals:X} {p6} {contentFinderConditionId}");
                if (LobbyType is GameLobbyType.Title or GameLobbyType.CharaSelect)
                {
                    // There's a 3rd party plugin (HaselTweaks) that prefetches the layout when entering queue
                    // This causes a rare race condition where if title edit loads the same scene after the prefetch happened
                    // the game will pass null festivals and crash so we unload any prefetched scene just in case
                    LayoutWorld.UnloadPrefetchLayout();
                    InitializeHousingLayout(characterSelectLocationModel);

                    // Set this flag to true so the game doesn't load EventHandlers it can't later cleanup causing a crash on login screen
                    // Only seen this happen with initial title screen set to S9 (check the comment on signature scan for more info)
                    if (LayoutEventStruct != null)
                    {
                        Services.Log.Debug("[CreateSceneDetour] LayoutEventStruct->Loaded = true");
                        LayoutEventStruct->Loaded = true;
                    }
                    else
                    {
                        Services.Log.Warning("[CreateSceneDetour] LayoutEventStruct was null");
                    }
                }

                if (LobbyType == GameLobbyType.CharaSelect)
                {
                    ResetLastCameraLookAtValues();
                    territoryPath = characterSelectLocationModel.TerritoryPath;
                    territoryId = characterSelectLocationModel.LayoutTerritoryTypeId;
                    layerFilterKey = characterSelectLocationModel.LayoutLayerFilterKey;
                    Services.Log.Debug($"Loading char select screen: {territoryPath}");
                    var returnVal = createSceneHook.Original(territoryPath, territoryId, p3, layerFilterKey, festivals, p6, contentFinderConditionId);
                    ResetSongIndex();
                    SetAllCharacterPostions();
                    return returnVal;
                }
                else if (LobbyType == GameLobbyType.Title)
                {
                    if (ShouldModifyTitleScreen)
                    {
                        territoryPath = titleScreenLocationModel.TerritoryPath;
                        territoryId = titleScreenLocationModel.LayoutTerritoryTypeId;
                        layerFilterKey = titleScreenLocationModel.LayoutLayerFilterKey;
                        Services.Log.Debug($"Loading title screen: {territoryPath}");
                        var returnVal = createSceneHook.Original(territoryPath, territoryId, p3, layerFilterKey, festivals, p6, contentFinderConditionId);
                        return returnVal;
                    }
                }

                if (lastSceneType == GameLobbyType.CharaSelect)
                {
                    // always reset camera when leaving character select
                    ResetCameraLookAtOnExitCharacterSelect();
                    ResetSongIndex();
                    // making new char
                    if (lastLobbyUpdateMapId == GameLobbyType.Aetherial)
                    {
                        // The game doesn't call the function responsible for picking BGM when moving from char select to char creation
                        // Probably because it will already be playing the correct music
                        ForcePlaySongIndex(LobbySong.CharacterSelect);

                        // Clean out housing data
                        InitializeHousingLayout();
                    }
                }

                return createSceneHook.Original(territoryPath, territoryId, p3, layerFilterKey, festivals, p6, contentFinderConditionId);
            } finally
            {
                lastSceneType = LobbyType;
                loadingLobbyType = GameLobbyType.None;
            }
        }

        // Called every frame to process something in character select and on load for title screen
        private byte LobbyUpdateDetour(GameLobbyType mapId, int time)
        {
            // if resetCharacterSelectScene is true or if switching between char select and char creation
            if (resetCharacterSelectScene ||
                (mapId == GameLobbyType.CharaSelect && lastLobbyUpdateMapId == GameLobbyType.Aetherial) ||
                (mapId == GameLobbyType.Aetherial && lastLobbyUpdateMapId == GameLobbyType.CharaSelect))
            {
                if (mapId != GameLobbyType.Title)
                {
                    Services.Log.Debug("Resetting scene");

                    if (liveEditCharacterSelect && !liveEditCharacterSelectLoaded)
                    {
                        previousCharacterSelectModelRotation = characterSelectLocationModel.Rotation;
                        lastCharacterRotation = 0;
                        characterSelectLocationModel = GetNothingSelectedLocation();
                    }

                    RecordCameraRotation();
                    CurrentLobbyMap = GameLobbyType.None;
                    resetCharacterSelectScene = false;
                }
            }

            lastLobbyUpdateMapId = mapId;
            return lobbyUpdateHook.Original(mapId, time);
        }


        // Called every frame to process lobby stuff
        private void UpdateLobbyUiStageDetour(AgentLobby* agentLobby)
        {
            var idling = AgentLobby->IdleTime + Framework.Instance()->FrameDeltaTimeMSInt > 60000;
            updateLobbyUiStageHook.Original(agentLobby);
            HandleLobbyUiStage(idling);
        }

        private void HandleLobbyUiStage(bool idling)
        {
            if (lastLobbyUiStage != LobbyUiStage)
            {
                Services.Log.Debug($"LobbyUiStage updated {lastLobbyUiStage} to {LobbyUiStage}, {CurrentLobbyMap}, {lastSceneType}, {loadingLobbyType}");
                if (LobbyUiStage is LobbyUiStage.EnteringTitleScreen or LobbyUiStage.LoadingSplashScreen)
                {
                    EnteringTitleScreen();
                }

                if (LobbyUiStage is LobbyUiStage.Movie or LobbyUiStage.CharacterSelect)
                {
                    LeavingTitleScreen(idling);
                }

                if (LobbyUiStage == LobbyUiStage.TitleScreen && lastLobbyUiStage == LobbyUiStage.LoadingTitleScreen2 && titleScreenLoaded)
                {
                    ShowToastNotification(titleScreenLocationModel.ToastNotificationText);
                }

                if (LobbyUiStage == LobbyUiStage.LoadingTitleScreen2 && shouldReloadTitleScreenOnLoadingStage2)
                {
                    shouldReloadTitleScreenOnLoadingStage2 = false;
                    ReloadTitleScreen();
                }

                lastLobbyUiStage = LobbyUiStage;
            }
        }

        public void ReloadCharacterSelect(bool force = false)
        {
            // Dawntrail title screen is a bitch and doesn't respect our CurrentLobbyMap value
            // You can even still move your camera around behind the cutscene which is neat but fuck you SE
            if ((CurrentLobbyMap == GameLobbyType.CharaSelect && !TitleCutsceneIsLoaded) || force)
            {
                resetCharacterSelectScene = true;
                previousCharacterSelectModelRotation = characterSelectLocationModel.Rotation;
                UpdateCharacter(true);
            }
        }

        private void ShowToastNotification(string message)
        {
            if (Services.ConfigurationService.DisplayTitleToast)
            {
                Services.NotificationManager.AddNotification(new()
                {
                    Content = message,
                    MinimizedText = message[..(!message.Contains('\n') ? message.Length : message.IndexOf('\n'))],
                    Title = "Scene loaded",
                    Type = NotificationType.Info
                });
            }
        }

        public override void Dispose()
        {
            DisableHooks();
            Services.Framework.Update -= Tick;
            if (initComplete)
            {
                // Resetting the character select thingy on unload
                // If this thing causes any troubles we axe it and tell the users to not do it :)
                if (CurrentLobbyMap == GameLobbyType.CharaSelect)
                {
                    //Techincally should be done from Agent Update but we can't have that here
                    CurrentLobbyMap = GameLobbyType.None;
                    ForcePlaySongIndex(LobbySong.CharacterSelect);
                    ClearCameraModifications();
                    ResetCharacters();
                    InitializeHousingLayout();
                }

                // Doing a scuffed title screen (Ui won't change) to prevent user from getting stuck in infinite loading screen
                // because of the DT cutscene being unloaded and the plugin restoring selected title screen to dawntrail
                if (CanReloadTitleScreen)
                {
                    ExecuteTitleScreenReload();
                    InitializeHousingLayout();
                }

                ResetCameraLookAtOnExitCharacterSelect();
            }

            base.Dispose();
            DisposeTitle();
            DisposeUi();
            DisposeNpcs();
        }
    }
}
