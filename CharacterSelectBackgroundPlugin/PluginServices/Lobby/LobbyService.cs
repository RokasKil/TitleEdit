using CharacterSelectBackgroundPlugin.Data.BGM;
using CharacterSelectBackgroundPlugin.Data.Lobby;
using CharacterSelectBackgroundPlugin.Utility;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

namespace CharacterSelectBackgroundPlugin.PluginServices.Lobby
{
    public unsafe partial class LobbyService : AbstractService
    {

        private delegate int OnCreateSceneDelegate(string territoryPath, uint p2, nint p3, uint p4, nint p5, int p6, uint p7);
        private delegate byte LobbyUpdateDelegate(GameLobbyType mapId, int time);

        private readonly Hook<OnCreateSceneDelegate> createSceneHook;
        private readonly Hook<LobbyUpdateDelegate> lobbyUpdateHook;

        private GameLobbyType lastLobbyUpdateMapId = GameLobbyType.None;
        private GameLobbyType lastSceneType = GameLobbyType.None;

        private string? lastBgmPath;

        private bool resetScene = false;

        private readonly GameLobbyType* lobbyCurrentMapAddress;

        public GameLobbyType CurrentLobbyMap
        {
            get => *lobbyCurrentMapAddress;
            set => *lobbyCurrentMapAddress = value;
        }

        public LobbyService()
        {
            Services.GameInteropProvider.InitializeFromAttributes(this);

            // Points to a Value that says what Type of lobby map is being displayer
            lobbyCurrentMapAddress = (GameLobbyType*)Utils.GetStaticAddressFromSigOrThrow("0F B7 05 ?? ?? ?? ?? 49 8B CE");

            // Called when creating a new scene in lobby (main menu, character select, character creation) - Used to switch out the level that loads and reset stuff
            createSceneHook = Hook<OnCreateSceneDelegate>("E8 ?? ?? ?? ?? 66 89 1D ?? ?? ?? ?? E9 ?? ?? ?? ??", OnCreateSceneDetour);

            // Lobby manager update (I think) - we use this as a point to change the Value at lobbyCurrentMapAddress to reload the scene
            lobbyUpdateHook = Hook<LobbyUpdateDelegate>("E8 ?? ?? ?? ?? EB 1C 3B CF", LobbyUpdateDetour);

            HookLayout();
            HookCharacter();
            HookCamera();
            HookSong();

            EnableHooks();

            Services.Framework.Update += Tick;
            locationModel = GetNothingSelectedLocation();
        }

        private void Tick(IFramework framework)
        {
            CharacterTick();
            if (CurrentLobbyMap == GameLobbyType.CharaSelect)
            {
                ModifyCamera();
            }
            else if (CurrentLobbyMap != GameLobbyType.None)
            {
                ClearCameraModifications();
            }
        }



        private int OnCreateSceneDetour(string territoryPath, uint p2, nint p3, uint p4, nint p5, int p6, uint p7)
        {
            try
            {
                Services.Log.Debug($"Loading Scene {lastLobbyUpdateMapId}");
                if (lastLobbyUpdateMapId == GameLobbyType.CharaSelect)
                {
                    ResetLastCameraLookAtValues();
                    territoryPath = locationModel.TerritoryPath;
                    Services.Log.Debug($"Loading char select screen: {territoryPath}");
                    var returnVal = createSceneHook.Original(territoryPath, p2, p3, p4, p5, p6, p7);
                    if (!locationModel.BgmPath.IsNullOrEmpty() && lastBgmPath != locationModel.BgmPath || locationModel.BgmPath.IsNullOrEmpty() && lastBgmPath != LocationService.DefaultLocation.BgmPath)
                    {
                        CurrentLobbyMusicIndex = 0;
                        ResetSongIndex();
                    }
                    return returnVal;
                }
                else if (lastSceneType == GameLobbyType.CharaSelect)
                {
                    // always reset camera when leaving character select
                    ResetCameraLookAtOnExitCharacterSelect();
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
            }

        }

        private byte LobbyUpdateDetour(GameLobbyType mapId, int time)
        {
            lastLobbyUpdateMapId = mapId;
            Services.Log.Verbose($"mapId {mapId}");

            if (resetScene)
            {
                RecordCameraRotation();
                resetScene = false;
                CurrentLobbyMap = GameLobbyType.None;
            }

            return lobbyUpdateHook.Original(mapId, time);
        }

        public override void Dispose()
        {
            base.Dispose();
            Services.Framework.Update -= Tick;
        }
    }
}
