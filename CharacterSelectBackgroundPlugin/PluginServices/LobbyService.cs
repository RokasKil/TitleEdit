using CharacterSelectBackgroundPlugin.Data;
using CharacterSelectBackgroundPlugin.Utils;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System;
using System.Runtime.InteropServices;

namespace CharacterSelectBackgroundPlugin.PluginServices
{
    public unsafe class LobbyService : IDisposable
    {

        [Signature("C6 81 ?? ?? ?? ?? ?? 8B 02 89 41 60")]
        private readonly delegate* unmanaged<LobbyCamera*, float[], float[], float, IntPtr> fixOnNative = null!;

        private delegate int OnCreateSceneDelegate(string territoryPath, uint p2, IntPtr p3, uint p4, IntPtr p5, int p6, uint p7);
        private delegate byte LobbyUpdateDelegate(GameLobbyType mapId, int time);
        private delegate byte SelectCharacterDelegate(uint characterIndex, char unk);
        private delegate byte SelectCharacter2Delegate(IntPtr self);
        private unsafe delegate void SetCameraCurveMidPointDelegate(LobbyCameraExpanded* self, float value);
        private delegate void SetCharSelectCurrentWorldDelegate(ulong unk);

        private readonly Hook<OnCreateSceneDelegate> createSceneHook;
        //private readonly Hook<OnPlayMusic> _playMusicHook;
        //private readonly Hook<OnFixOn> _fixOnHook;
        private readonly Hook<LobbyUpdateDelegate> lobbyUpdateHook;
        private readonly Hook<SelectCharacterDelegate> selectCharacterHook;
        private readonly Hook<SelectCharacter2Delegate> selectCharacter2Hook;
        private readonly Hook<SetCameraCurveMidPointDelegate> setCameraCurveMidPointHook;
        private readonly Hook<SetCharSelectCurrentWorldDelegate> setCharSelectCurrentWorldHook;

        private readonly IntPtr lobbyCurrentMapAddress;
        private GameLobbyType lastLobbyUpdateMapId = GameLobbyType.Movie;
        private ulong lastContentId;
        private LocationModel locationModel = LocationService.DefaultLocation;

        private bool resetScene = false;
        private bool resetCamera = false;
        public short CurrentLobbyMap
        {
            get => Marshal.ReadInt16(lobbyCurrentMapAddress);
            set => Marshal.WriteInt16(lobbyCurrentMapAddress, value);
        }

        public LobbyService()
        {
            Services.GameInteropProvider.InitializeFromAttributes(this);

            lobbyCurrentMapAddress = Services.SigScanner.GetStaticAddressFromSig("0F B7 05 ?? ?? ?? ?? 49 8B CE");

            createSceneHook = Services.GameInteropProvider.HookFromSignature<OnCreateSceneDelegate>("E8 ?? ?? ?? ?? 66 89 1D ?? ?? ?? ?? E9 ?? ?? ?? ??", OnCreateSceneDetour);
            lobbyUpdateHook = Services.GameInteropProvider.HookFromSignature<LobbyUpdateDelegate>("E8 ?? ?? ?? ?? EB 1C 3B CF", LobbyUpdateDetour);
            // Happends on character list hover - probably don't need to set player pos here cause covered by setCharSelectCurrentWorldHook
            selectCharacterHook = Services.GameInteropProvider.HookFromSignature<SelectCharacterDelegate>("E8 ?? ?? ?? ?? 0F B6 D8 84 C0 75 ?? 49 8B CD", SelectCharacterDetour);
            // Happens on world list hover
            selectCharacter2Hook = Services.GameInteropProvider.HookFromSignature<SelectCharacter2Delegate>("40 53 48 83 EC ?? 41 83 C8 ?? 4C 8D 15", SelectCharacter2Detour);
            setCameraCurveMidPointHook = Services.GameInteropProvider.HookFromSignature<SetCameraCurveMidPointDelegate>("0F 57 C0 0F 2F C1 73 ?? F3 0F 11 89", SetCameraCurveMidPointDetour);
            setCharSelectCurrentWorldHook = Services.GameInteropProvider.HookFromSignature<SetCharSelectCurrentWorldDelegate>("E8 ?? ?? ?? ?? 49 8B CD 48 8B 7C 24", SetCharSelectCurrentWorldDetour);

            Enable();

            Services.ClientState.Login += ResetState;
            Services.Framework.Update += Tick;
        }

        private void SetCharSelectCurrentWorldDetour(ulong unk)
        {
            setCharSelectCurrentWorldHook.Original(unk);
            Services.Log.Debug("SetCharSelectCurrentWorldDetour");

            var charaSelectCharacterList = CharaSelectCharacterList.Instance();
            var clientObjectManager = ClientObjectManager.Instance();
            if (charaSelectCharacterList != null && clientObjectManager != null)
            {

                for (int i = 0; i < charaSelectCharacterList->CharacterMappingSpan.Length; i++)
                {
                    if (charaSelectCharacterList->CharacterMappingSpan[i].ContentId == 0)
                    {
                        break;
                    }
                    var contentId = charaSelectCharacterList->CharacterMappingSpan[i].ContentId;
                    var clientObjectIndex = charaSelectCharacterList->CharacterMappingSpan[i].ClientObjectIndex;
                    Services.Log.Debug($"{charaSelectCharacterList->CharacterMappingSpan[i].ContentId:X} to {charaSelectCharacterList->CharacterMappingSpan[i].ClientObjectIndex}");
                    var pos = Services.LocationService.GetLocationModel(contentId).Position;
                    var gameObject = clientObjectManager->GetObjectByIndex((ushort)clientObjectIndex);
                    gameObject->SetPosition(pos.X, pos.Y, pos.Z);
                    Services.Log.Debug($"{(IntPtr)gameObject:X} set to {pos}");
                }
                //Set current character cause SE forgot to do this ??
                *(CharaSelectCharacterList.StaticAddressPointers.ppGetCurrentCharacter) = GetCurrentCharacter();
                Services.Log.Debug($"Set current char to {(IntPtr)(*(CharaSelectCharacterList.StaticAddressPointers.ppGetCurrentCharacter)):X}");

            }
            else
            {
                Services.Log.Warning($"[SetCharSelectCurrentWorldDetour] failed to get instance {(IntPtr)charaSelectCharacterList:X} {(IntPtr)clientObjectManager:X}");
            }
        }

        public void Tick(IFramework framework)
        {
            if (lastContentId != 0 && CharaSelectCharacterList.GetCurrentCharacter() == null)
            {
                ResetState();
                resetScene = true;
            }
        }

        public unsafe void ResetState()
        {
            lastContentId = 0;
            locationModel = LocationService.DefaultLocation;
            resetCamera = true;


        }

        public void FixOn(LobbyCamera* camera, float[] cameraPos, float[] focusPos, float fovY)
        {
            if (fixOnNative == null)
                throw new InvalidOperationException("FixOn signature wasn't found!");

            fixOnNative(camera, cameraPos, focusPos, fovY);
        }

        private int OnCreateSceneDetour(string territoryPath, uint p2, IntPtr p3, uint p4, IntPtr p5, int p6, uint p7)
        {
            //Log($"HandleCreateScene {p1} {p2} {p3.ToInt64():X} {p4} {p5.ToInt64():X} {p6} {p7}");
            //_titleCameraNeedsSet = false;
            //_amForcingTime = false;
            //_amForcingWeather = false;
            Services.Log.Debug($"Loading Scene {lastLobbyUpdateMapId}");
            if (resetCamera)
            {
                var cameraManager = CameraManager.Instance();
                if (cameraManager != null)
                {
                    LobbyCameraExpanded* camera = (LobbyCameraExpanded*)cameraManager->LobbCamera;
                    camera->lowPoint.value = 1.4350828f;
                    camera->midPoint.value = 0.85870504f;
                    camera->highPoint.value = 0.6742642f;
                    camera->LobbyCamera.Camera.CameraBase.SceneCamera.LookAtVector = FFXIVClientStructs.FFXIV.Common.Math.Vector3.Zero;
                }

                Services.Log.Debug($"Reset Lobby camera");
                resetCamera = false;
            }
            if (lastLobbyUpdateMapId == GameLobbyType.CharaSelect)
            {
                //RefreshCurrentTitleEditScreen();

                territoryPath = locationModel.TerritoryPath;
                Services.Log.Debug($"Loading char select screen: {territoryPath}");
                var returnVal = createSceneHook.Original(territoryPath, p2, p3, p4, p5, p6, p7);

                //var camera = CameraManager.Instance()->LobbCamera;
                //if (lastContentId == 0 && camera != null)
                //{
                //    FixOn(camera, Vector3.Zero.ToArray(), new Vector3(0, 0.8580103f, 0).ToArray(), 1);
                //}
                //ForceWeather(_currentScreen.WeatherId, 5000);
                //ForceTime(_currentScreen.TimeOffset, 5000);
                //FixOn(_currentScreen.CameraPos, _currentScreen.FixOnPos, 1);
                // SetRevisionStringVisibility(_configuration.DisplayVersionText);
                return returnVal;
            }
            return createSceneHook.Original(territoryPath, p2, p3, p4, p5, p6, p7);
        }

        private byte LobbyUpdateDetour(GameLobbyType mapId, int time)
        {
            lastLobbyUpdateMapId = mapId;
            Services.Log.Verbose($"mapId {mapId}");
            if (resetScene)
            {
                resetScene = false;
                CurrentLobbyMap = (short)GameLobbyType.Movie;
            }

            return lobbyUpdateHook.Original(mapId, time);
        }

        //SE's implenetation does nothing if value is below 0 which breaks the camera when character is in negative Y
        private unsafe void SetCameraCurveMidPointDetour(LobbyCameraExpanded* self, float value)
        {
            self->midPoint.value = value;
        }

        private unsafe byte SelectCharacter2Detour(IntPtr self)
        {
            var result = selectCharacter2Hook.Original(self);
            UpdateCharacter();
            return result;
        }

        private unsafe byte SelectCharacterDetour(uint characterIndex, char unk)
        {
            var result = selectCharacterHook.Original(characterIndex, unk);
            UpdateCharacter();
            return result;
        }

        private unsafe FFXIVClientStructs.FFXIV.Client.Game.Character.Character* GetCurrentCharacter()
        {

            var agentLobby = AgentLobby.Instance();
            var charaSelectCharacterList = CharaSelectCharacterList.Instance();
            var clientObjectManager = ClientObjectManager.Instance();
            if (agentLobby != null && charaSelectCharacterList != null && clientObjectManager != null)
            {
                if (agentLobby->HoveredCharacterIndex == -1)
                {
                    return null;
                }
                var clientObjectIndex = charaSelectCharacterList->CharacterMappingSpan[agentLobby->HoveredCharacterIndex].ClientObjectIndex;
                if (clientObjectIndex == -1)
                {
                    Services.Log.Warning($"[getCurrentCharacter] clientObjectIndex -1 for {agentLobby->HoveredCharacterIndex}");
                    return null;
                }
                return (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)clientObjectManager->GetObjectByIndex((ushort)clientObjectIndex);
            }
            else
            {
                Services.Log.Warning($"[getCurrentCharacter] failed to get instance  {(IntPtr)agentLobby:X} {(IntPtr)charaSelectCharacterList:X} {(IntPtr)clientObjectManager:X}");

            }
            return null;
        }

        private unsafe void UpdateCharacter()
        {
            var character = CharaSelectCharacterList.GetCurrentCharacter();

            if (character != null)
            {
                var agentLobby = AgentLobby.Instance();
                if (agentLobby != null)
                {
                    var contentId = agentLobby->LobbyData.CharaSelectEntries.Get((ulong)agentLobby->HoveredCharacterIndex).Value->ContentId;
                    if (lastContentId != contentId)
                    {
                        lastContentId = contentId;

                        var newLocationModel = Services.LocationService.GetLocationModel(contentId);
                        if (!newLocationModel.Equals(locationModel))
                        {
                            locationModel = newLocationModel;
                            resetScene = true;
                        }
                        Services.Log.Debug($"Setting character postion {(IntPtr)character:X}");
                        character->GameObject.SetPosition(locationModel.Position.X, locationModel.Position.Y, locationModel.Position.Z);

                    }
                }


            }
            else
            {
                Services.Log.Info("Character was null :(");
            }

        }
        public void Enable()
        {
            createSceneHook.Enable();
            lobbyUpdateHook.Enable();
            selectCharacterHook.Enable();
            selectCharacter2Hook.Enable();
            setCameraCurveMidPointHook.Enable();
            setCharSelectCurrentWorldHook.Enable();
        }

        public void Dispose()
        {
            createSceneHook?.Dispose();
            lobbyUpdateHook?.Dispose();
            selectCharacterHook?.Dispose();
            selectCharacter2Hook?.Dispose();
            setCameraCurveMidPointHook?.Dispose();
            setCharSelectCurrentWorldHook?.Dispose();
            Services.ClientState.Login -= ResetState;
        }
    }
}
