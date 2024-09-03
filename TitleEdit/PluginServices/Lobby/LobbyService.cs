using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using TitleEdit.Data.BGM;
using TitleEdit.Data.Lobby;
using TitleEdit.Data.Persistence;
using TitleEdit.Utility;

namespace TitleEdit.PluginServices.Lobby
{
    public unsafe partial class LobbyService : AbstractService
    {
        public AgentLobby* AgentLobby => FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentLobby.Instance();

        private delegate int CreateSceneDelegate(string territoryPath, uint p2, nint p3, uint p4, nint p5, int p6, uint p7);
        private delegate byte LobbyUpdateDelegate(GameLobbyType mapId, int time);
        private delegate void LoadLobbyScene(GameLobbyType mapId);
        private delegate void UpdateLobbyUiStage(AgentLobby* agentLobby);

        private readonly Hook<CreateSceneDelegate> createSceneHook;
        private readonly Hook<LobbyUpdateDelegate> lobbyUpdateHook;
        private readonly Hook<LoadLobbyScene> loadLobbySceneHook;
        private readonly Hook<UpdateLobbyUiStage> updateLobbyUiStageHook;

        // Probably some lobby instance
        private nint lobbyStructAddress;

        private GameLobbyType lastLobbyUpdateMapId = GameLobbyType.None;
        private GameLobbyType lastSceneType = GameLobbyType.None;
        private GameLobbyType loadingLobbyType = GameLobbyType.None;



        private bool resetScene = false;

        private readonly GameLobbyType* lobbyCurrentMapAddress;

        private LobbyUiStage lastLobbyUiStage = 0;

        public LobbyUiStage LobbyUiStage
        {
            get => (LobbyUiStage)AgentLobby->LobbyUIStage;
            set => AgentLobby->LobbyUIStage = (byte)value;
        }

        public GameLobbyType CurrentLobbyMap
        {
            get => *lobbyCurrentMapAddress;
            set => *lobbyCurrentMapAddress = value;
        }

        public LobbyInfo* LobbyInfo => (LobbyInfo*)lobbyStructAddress;

        public LobbyService()
        {
            Services.GameInteropProvider.InitializeFromAttributes(this);


            // Points to a struct with a value that indicates the current lobby bgm and current title screen type (among other things)
            lobbyStructAddress = Utils.GetStaticAddressFromSigOrThrow("66 0F 7F 05 ?? ?? ?? ?? 4C 89 35");

            // Points to a Value that says what Type of lobby map is being displayer
            lobbyCurrentMapAddress = (GameLobbyType*)Utils.GetStaticAddressFromSigOrThrow("0F B7 05 ?? ?? ?? ?? 48 8B CE");

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
            HookTitle();
        }

        public override void Init()
        {
            EnableHooks();

            Services.Framework.Update += Tick;
            if (CurrentCharacter != null)
            {
                characterSelectLocationModel = GetLocationForContentId(GetContentId());
            }
            else
            {
                characterSelectLocationModel = GetNothingSelectedLocation();
            }
            titleScreenLocationModel = GetTitleLocation();
            if (CurrentLobbyMap == GameLobbyType.CharaSelect)
            {
                resetScene = true;
                // Forcing a character update on layout load to set positions and stuff
                // Will be missing mount if there was no companion object created which is probably the case unless the plugin is being turned off and on again
                // I will not be fixing that cause fuck it
                forceUpdateCharacter = CurrentCharacter != null;
            }
        }

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
            }
            TickTitle();
        }



        private int CreateSceneDetour(string territoryPath, uint p2, nint p3, uint p4, nint p5, int p6, uint p7)
        {
            try
            {
                var lobbyType = loadingLobbyType == GameLobbyType.None ? lastLobbyUpdateMapId : loadingLobbyType;
                Services.Log.Debug($"Loading Scene {lobbyType}");
                if (lobbyType == GameLobbyType.CharaSelect)
                {
                    ResetLastCameraLookAtValues();
                    territoryPath = characterSelectLocationModel.TerritoryPath;
                    Services.Log.Debug($"Loading char select screen: {territoryPath}");
                    var returnVal = createSceneHook.Original(territoryPath, p2, p3, p4, p5, p6, p7);
                    if ((!characterSelectLocationModel.BgmPath.IsNullOrEmpty() && lastBgmPath != characterSelectLocationModel.BgmPath) ||
                        // TODO: check if this is fine
                        (characterSelectLocationModel.BgmPath.IsNullOrEmpty() && lastBgmPath != Services.PresetService.GetDefaultPreset(LocationType.CharacterSelect).LocationModel.BgmPath))
                    {
                        ResetSongIndex();
                    }
                    SetAllCharacterPostions();
                    return returnVal;
                }
                else if (lobbyType == GameLobbyType.Title && ShouldModifyTitleScreen)
                {
                    territoryPath = titleScreenLocationModel.TerritoryPath;
                    Services.Log.Debug($"Loading title screen: {territoryPath}");
                    var returnVal = createSceneHook.Original(territoryPath, p2, p3, p4, p5, p6, p7);
                    return returnVal;
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
                    }

                }
                return createSceneHook.Original(territoryPath, p2, p3, p4, p5, p6, p7);
            }
            finally
            {
                lastSceneType = lastLobbyUpdateMapId;
                loadingLobbyType = GameLobbyType.None;
            }

        }

        private byte LobbyUpdateDetour(GameLobbyType mapId, int time)
        {
            Services.Log.Verbose($"mapId {mapId}");

            // if resetScene is true or if switching between char select and char creation
            if (resetScene ||
                (mapId == GameLobbyType.CharaSelect && lastLobbyUpdateMapId == GameLobbyType.Aetherial) ||
                (mapId == GameLobbyType.Aetherial && lastLobbyUpdateMapId == GameLobbyType.CharaSelect))
            {
                if (mapId != GameLobbyType.Title)
                {
                    Services.Log.Debug("Resetting scene");
                    RecordCameraRotation();
                    CurrentLobbyMap = GameLobbyType.None;
                    resetScene = false;
                }
            }

            lastLobbyUpdateMapId = mapId;
            return lobbyUpdateHook.Original(mapId, time);
        }


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
                lastLobbyUiStage = LobbyUiStage;
                if (LobbyUiStage == LobbyUiStage.EnteringTitleScreen || LobbyUiStage == LobbyUiStage.LoadingSplashScreen)
                {
                    EnteringTitleScreen();
                }
                if (LobbyUiStage == LobbyUiStage.Movie || LobbyUiStage == LobbyUiStage.CharacterSelect)
                {
                    LeavingTitleScreen(idling);
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            DisposeTitle();
            Services.Framework.Update -= Tick;
            // Resetting the character select thingy on unload
            // If this thing causes any troubles we axe it and tell the users to not do it :)
            if (CurrentLobbyMap == GameLobbyType.CharaSelect)
            {
                //Techincally should be done from Agent Update but we can't have that here
                CurrentLobbyMap = GameLobbyType.None;
                ForcePlaySongIndex(LobbySong.CharacterSelect);
                ClearCameraModifications();
                ResetCharacters();
            }
        }
    }
}
