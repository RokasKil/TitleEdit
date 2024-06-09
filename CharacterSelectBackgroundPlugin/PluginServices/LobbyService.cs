using CharacterSelectBackgroundPlugin.Data.Character;
using CharacterSelectBackgroundPlugin.Data.Layout;
using CharacterSelectBackgroundPlugin.Data.Lobby;
using CharacterSelectBackgroundPlugin.Data.Persistence;
using CharacterSelectBackgroundPlugin.Utility;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Common.Math;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using CameraManager = FFXIVClientStructs.FFXIV.Client.Game.Control.CameraManager;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using World = Lumina.Excel.GeneratedSheets.World;

namespace CharacterSelectBackgroundPlugin.PluginServices
{
    public unsafe class LobbyService : AbstractService
    {
        [Signature("40 53 48 83 EC ?? 44 0F BF C1")]
        private readonly delegate* unmanaged<ushort, void> setTimeNative = null!;
        [Signature("E8 ?? ?? ?? ?? 33 C9 E8 ?? ?? ?? ?? 48 8B 0D")]
        private readonly delegate* unmanaged<IntPtr, ushort, void> pickSongNative = null!;

        private delegate int OnCreateSceneDelegate(string territoryPath, uint p2, IntPtr p3, uint p4, IntPtr p5, int p6, uint p7);
        private delegate byte LobbyUpdateDelegate(GameLobbyType mapId, int time);
        private delegate ulong SelectCharacterDelegate(uint characterIndex, char p2);
        private delegate ulong SelectCharacter2Delegate(IntPtr p1);
        private unsafe delegate void SetCameraCurveMidPointDelegate(LobbyCameraExpanded* self, float value);
        private unsafe delegate void CalculateCameraCurveLowAndHighPointDelegate(LobbyCameraExpanded* self, float value);
        private delegate void SetCharSelectCurrentWorldDelegate(ulong p1);
        private delegate void SomeEnvManagerThingyDelegate(ulong p1, uint p2, float p3);
        private delegate ulong WeatherThingyDelegate(ulong p1, byte weatherId);
        private delegate void CharSelectSetWeatherDelegate();
        private delegate IntPtr PlayMusicDelegate(IntPtr self, string filename, float volume, uint fadeTime);
        private delegate IntPtr CreateBattleCharacterDelegate(IntPtr objectManager, uint index, bool assignCompanion);
        private delegate void CharSelectWorldPreviewEventHandlerDelegate(ulong p1, ulong p2, ulong p3, uint p4);
        private delegate void LobbySceneLoadedDelegate(ulong p1, int p2, float p3, ushort p4, uint p5, uint p6, uint p7);

        private readonly Hook<OnCreateSceneDelegate> createSceneHook;
        private readonly Hook<LobbyUpdateDelegate> lobbyUpdateHook;
        private readonly Hook<SelectCharacterDelegate> selectCharacterHook;
        private readonly Hook<SelectCharacter2Delegate> selectCharacter2Hook;
        private readonly Hook<SetCameraCurveMidPointDelegate> setCameraCurveMidPointHook;
        private readonly Hook<CalculateCameraCurveLowAndHighPointDelegate> calculateCameraCurveLowAndHighPointHook;
        private readonly Hook<SetCharSelectCurrentWorldDelegate> setCharSelectCurrentWorldHook;
        private readonly Hook<CharSelectSetWeatherDelegate> charSelectSetWeatherHook;
        private readonly Hook<PlayMusicDelegate> playMusicHook;
        private readonly Hook<CreateBattleCharacterDelegate> createBattleCharacterHook;
        private readonly Hook<CharSelectWorldPreviewEventHandlerDelegate> charSelectWorldPreviewEventHandlerHook;
        private readonly Hook<LobbySceneLoadedDelegate> lobbySceneLoadedHook;

        private GameLobbyType lastLobbyUpdateMapId = GameLobbyType.None;
        private GameLobbyType lastSceneType = GameLobbyType.None;
        private ulong lastContentId;
        private LocationModel locationModel;

        private string? lastBgmPath;

        private bool resetScene = false;
        private bool resetCamera = false;

        private float lastYaw = 0;
        private float lastPitch = 0;
        private float lastDistance = 0;
        private bool rotationJustRecorded;
        private float cameraYOffset = 0;

        private readonly IntPtr lobbyCurrentMapAddress;

        private bool creatingCharSelectGameObjects = false;
        public short CurrentLobbyMap
        {
            get => Marshal.ReadInt16(lobbyCurrentMapAddress);
            set => Marshal.WriteInt16(lobbyCurrentMapAddress, value);
        }

        // Probably some lobby instance
        // method at E8 ?? ?? ?? ?? 33 C9 E8 ?? ?? ?? ?? 48 8B 0D picks a song from an array of 7 entries
        // ["", <arr title>, <char select>, <hw title>, <sb title>, <shb title>, <ew title>]
        // calls the method hooked at playMusicHook with selected path and stores the model at 0x18 with the index being stored at 0x20
        // on subsequent calls it checks if we need to reset by comparing offset 0x20 with provided music index
        // we abuse that by setting it back to 0
        private readonly IntPtr* lobbyBgmBasePointerAddress;
        public uint CurrentLobbyMusicIndex
        {
            get => (uint)Marshal.ReadInt32(*lobbyBgmBasePointerAddress, 0x20);
            set => Marshal.WriteInt32(*lobbyBgmBasePointerAddress, 0x20, (int)value);
        }

        public LobbyService()
        {
            Services.GameInteropProvider.InitializeFromAttributes(this);

            // Points to a value that says what Type of lobby map is being displayer
            lobbyCurrentMapAddress = Utils.GetStaticAddressFromSigOrThrow("0F B7 05 ?? ?? ?? ?? 49 8B CE");
            // Points to a value that indicates the current lobby bgm Type that's playing, we maniplate this to force bgm change alongside playMusicHook
            lobbyBgmBasePointerAddress = (IntPtr*)Utils.GetStaticAddressFromSigOrThrow("48 8B 35 ?? ?? ?? ?? 88 46");


            // Called when creating a new scene in lobby (main menu, character select, character creation) - Used to switch out the level that loads and reset stuff
            createSceneHook = Hook<OnCreateSceneDelegate>("E8 ?? ?? ?? ?? 66 89 1D ?? ?? ?? ?? E9 ?? ?? ?? ??", OnCreateSceneDetour);

            // Lobby manager update (I think) - we use this as a point to change the value at lobbyCurrentMapAddress to reload the scene
            lobbyUpdateHook = Hook<LobbyUpdateDelegate>("E8 ?? ?? ?? ?? EB 1C 3B CF", LobbyUpdateDetour);

            // Happends on character list hover - update character positions, mount, redraw the scene if needed
            selectCharacterHook = Hook<SelectCharacterDelegate>("E8 ?? ?? ?? ?? 0F B6 D8 84 C0 75 ?? 49 8B CD", SelectCharacterDetour);

            // Happens on world list hover - we update the previewd character's position and mount
            selectCharacter2Hook = Hook<SelectCharacter2Delegate>("40 53 48 83 EC ?? 41 83 C8 ?? 4C 8D 15", SelectCharacter2Detour);

            // Sets the middle point of the camera Y position curve (made out of 3 point), called every frame, by default doesn't accept negartive values, we fix that
            // and adjust by head offset so camera gets centered better
            setCameraCurveMidPointHook = Hook<SetCameraCurveMidPointDelegate>("0F 57 C0 0F 2F C1 73 ?? F3 0F 11 89", SetCameraCurveMidPointDetour);
            // Sets the low and high point of the camera Y position curve we adjust with head offset here so camera stays centered better
            calculateCameraCurveLowAndHighPointHook = Hook<CalculateCameraCurveLowAndHighPointDelegate>("F3 0F 10 81 ?? ?? ?? ?? F3 0F 11 89", CalculateCameraCurveLowAndHighPointDetour);

            // Called when you select a new world in character select or cancel selection so it reload the current - we use it make sure characters get created with a companion slots, initialze their positions and mounts
            setCharSelectCurrentWorldHook = Hook<SetCharSelectCurrentWorldDelegate>("E8 ?? ?? ?? ?? 49 8B CD 48 8B 7C 24", SetCharSelectCurrentWorldDetour);

            // Called when game does some lobby weather setting - we use it as an indicator to set scene details like weather, time and layout
            // Called on scene load and on displayed character switch
            charSelectSetWeatherHook = Hook<CharSelectSetWeatherDelegate>("0F B7 0D ?? ?? ?? ?? 8D 41", CharSelectSetWeatherDetour);

            // Called when lobby music needs to be changed - we force call the game to call it by resetting the CurrentLobbyMusicIndex pointer
            playMusicHook = Hook<PlayMusicDelegate>("E8 ?? ?? ?? ?? 48 89 47 18 89 5F 20", PlayMusicDetour);

            // Called when the game is making a new character - if set by other hooks we force the flag to include a companionObject so we can display a mount
            createBattleCharacterHook = Hook<CreateBattleCharacterDelegate>("E8 ?? ?? ?? ?? 83 F8 ?? 74 ?? 8B D0", CreateBattleCharacterDetour);

            // Happens on world list hover when loading a world - we use it make sure characters get created with a companion slots (maybe makes selectCharacter2Hook redundant)
            charSelectWorldPreviewEventHandlerHook = Hook<CharSelectWorldPreviewEventHandlerDelegate>("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 41 83 FE ?? 0F 8C", CharSelectWorldPreviewEventHandlerDetour);

            // Some LobbySceneLoaded thingy, called once a new level is loaded we use it to restore camera position
            lobbySceneLoadedHook = Hook<LobbySceneLoadedDelegate>("E8 ?? ?? ?? ?? 41 0F B7 CC C6 05", LobbySceneLoadedDetour);

            EnableHooks();

            Services.ClientState.Login += ResetState;
            Services.Framework.Update += Tick;
            locationModel = GetNothingSelectedLocation();
        }

        private void LobbySceneLoadedDetour(ulong p1, int p2, float p3, ushort p4, uint p5, uint p6, uint p7)
        {
            Services.Log.Debug($"[LobbySceneLoaded] {p1:X} {p2:X} {p3} {p4:X} {p5:X} {p6:X} {p7:X}");
            lobbySceneLoadedHook.Original(p1, p2, p3, p4, p5, p6, p7);
            //SetLayoutInfo();
            SetCameraRotation();
        }

        public Dictionary<ulong, string> GetCurrentCharacterNames()
        {
            Dictionary<ulong, string> result = [];
            if (CurrentLobbyMap != (short)GameLobbyType.CharaSelect) return result;
            var agentLobby = AgentLobby.Instance();
            if (agentLobby != null)
            {
                var characterSelects = agentLobby->LobbyData.CharaSelectEntries.Span;
                foreach (var character in characterSelects)
                {
                    if (character.Value->ContentId != 0)
                    {
                        var world = Services.DataManager.GetExcelSheet<World>()?.GetRow(character.Value->HomeWorldId);
                        if (world != null)
                        {
                            result[character.Value->ContentId] = $"{Encoding.UTF8.GetString(character.Value->Name, 32).TrimEnd('\0')}@{world.Name}";
                        }
                    }
                }
            }
            return result;
        }

        private void CharSelectWorldPreviewEventHandlerDetour(ulong p1, ulong p2, ulong p3, uint p4)
        {
            creatingCharSelectGameObjects = true;
            charSelectWorldPreviewEventHandlerHook.Original(p1, p2, p3, p4);
            creatingCharSelectGameObjects = false;
        }

        private IntPtr CreateBattleCharacterDetour(IntPtr objectManager, uint index, bool assignCompanion)
        {
            if (creatingCharSelectGameObjects)
            {
                Services.Log.Debug("[CreateBattleCharacterDetour] setting assignCompanion");
            }
            return createBattleCharacterHook.Original(objectManager, index, assignCompanion || creatingCharSelectGameObjects);
        }

        private unsafe void CharSelectSetWeatherDetour()
        {
            charSelectSetWeatherHook.Original();
            SetLayoutInfo();
            Services.Log.Debug($"CharSelectSetWeatherDetour {EnvManager.Instance()->ActiveWeather}");

        }

        private void SetCameraRotation()
        {
            var camera = GetCamera();
            if (camera != null)
            {
                camera->Yaw = Utils.NormalizeAngle(lastYaw);
                camera->Pitch = lastPitch;
                camera->LobbyCamera.Camera.Distance = lastDistance;
                camera->LobbyCamera.Camera.InterpDistance = lastDistance;
            }
            rotationJustRecorded = false;
            Services.Log.Debug($"Loaded rotation {lastYaw} {lastPitch} {lastDistance}");
            if (CharaSelectCharacterList.GetCurrentCharacter() != null)
            {
                if (camera != null)
                {
                    camera->Yaw = Utils.NormalizeAngle(camera->Yaw + locationModel.Rotation);
                }
                CharaSelectCharacterList.GetCurrentCharacter()->GameObject.Rotate(locationModel.Rotation);
            }

            Services.Log.Debug($"After load rotation {camera->Yaw} {camera->Pitch} {camera->LobbyCamera.Camera.Distance}");
        }

        private void SetLayoutInfo()
        {
            if (CurrentLobbyMap == (short)GameLobbyType.CharaSelect)
            {
                fixed (uint* pFestivals = locationModel.Festivals)
                {
                    Services.LayoutService.LayoutManager->layoutManager.SetActiveFestivals(pFestivals);
                }
                EnvManager.Instance()->ActiveWeather = locationModel.WeatherId;
                SetTime(locationModel.TimeOffset);
                Services.Log.Debug($"SetWeather to {EnvManager.Instance()->ActiveWeather}");
                if (locationModel.Active != null && locationModel.Inactive != null)
                {
                    List<ulong> unknownUUIDs = new();
                    Services.LayoutService.ForEachInstance(instance =>
                    {
                        if (locationModel.Active.Contains(instance.Value->UUID))
                        {
                            SetActive(instance.Value, true);
                        }
                        else if (locationModel.Inactive.Contains(instance.Value->UUID))
                        {
                            SetActive(instance.Value, false);
                        }
                        else
                        {
                            unknownUUIDs.Add(instance.Value->UUID);
                        }
                    });
                    if (unknownUUIDs.Count > 0)
                    {
                        Services.Log.Debug($"{unknownUUIDs.Count} UUIDs not found in the layout data");
                    }
                }
                else
                {
                    Services.Log.Warning($"Layout data was null for {lastContentId:X16}");
                }
            }
        }

        private void SetActive(ILayoutInstance* instance, bool active)
        {
            if (instance->Id.Type == InstanceType.Vfx)
            {
                SetIndex((VfxLayoutInstance*)instance);
                instance->SetActiveVF54(active);
            }
            else
            {
                instance->SetActive(active);
            }
        }

        private void SetIndex(VfxLayoutInstance* instance)
        {
            if (locationModel.VfxTriggerIndexes.TryGetValue(instance->ILayoutInstance.UUID, out var index))
            {
                Services.LayoutService.SetVfxLayoutInstanceVfxTriggerIndex(instance, index);
            }
        }

        // TODO: figure out looping on tracks that don't loop
        // Figure out new character creation music 
        private IntPtr PlayMusicDetour(IntPtr self, string filename, float volume, uint fadeTime)
        {
            Services.Log.Debug($"PlayMusicDetour {self.ToInt64():X} {filename} {volume} {fadeTime}");

            if (CurrentLobbyMap == (short)GameLobbyType.CharaSelect && !locationModel.BgmPath.IsNullOrEmpty())
            {
                Services.Log.Debug($"Setting music to {locationModel.BgmPath}");
                filename = locationModel.BgmPath;
            }
            lastBgmPath = filename;
            return playMusicHook.Original(self, filename, volume, fadeTime);
        }

        private void SetCharSelectCurrentWorldDetour(ulong p1)
        {
            creatingCharSelectGameObjects = true;
            setCharSelectCurrentWorldHook.Original(p1);
            creatingCharSelectGameObjects = false;
            Services.Log.Debug("SetCharSelectCurrentWorldDetour");
            foreach (var entry in GetCurrentCharacterNames())
            {
                Services.CharactersService.PutCharacter(entry.Key, entry.Value);
            }
            Services.CharactersService.SaveCharacters();

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
                    var location = GetLocationForContentId(contentId);
                    var gameObject = clientObjectManager->GetObjectByIndex((ushort)clientObjectIndex);
                    if (gameObject != null)
                    {
                        gameObject->SetPosition(location.Position.X, location.Position.Y, location.Position.Z);

                        if (gameObject->IsCharacter()) //Probably useless check?
                        {
                            CharacterExpanded* character = (CharacterExpanded*)gameObject;
                            character->MovementMode = locationModel.MovementMode;
                            if (location.Mount.MountId != 0)
                            {
                                SetupMount(&character->Character, locationModel);
                            }
                            else if (!gameObject->IsCharacter())
                            {

                                Services.Log.Debug($"{(IntPtr)gameObject:X} is not character?");
                            }
                        }
                        Services.Log.Debug($"{(IntPtr)gameObject:X} set to {location.Position} {location.Rotation}");
                    }
                    else
                    {
                        Services.Log.Debug("Gameobject was null?");
                    }
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

        private void Tick(IFramework framework)
        {
            var currentChar = CharaSelectCharacterList.GetCurrentCharacter();
            if (lastContentId != 0 && currentChar == null)
            {
                ResetState();
                resetScene = true;
            }
            // We do a slight polling cause it's simpler than figuring when exactly are mounts and stuff are good to draw
            var camera = GetCamera();
            if (CurrentLobbyMap == (short)GameLobbyType.CharaSelect)
            {
                if (currentChar != null)
                {
                    if (currentChar->GameObject.RenderFlags != 0 && currentChar->GameObject.RenderFlags != 0x40 && currentChar->GameObject.IsReadyToDraw())
                    {
                        Services.Log.Debug($"Drawing character {(IntPtr)currentChar:X} {currentChar->GameObject.RenderFlags:X}");
                        currentChar->GameObject.EnableDraw();
                        if (currentChar->IsMounted() && currentChar->CompanionObject != null && currentChar->CompanionObject->Character.GameObject.IsReadyToDraw())
                        {
                            Services.Log.Debug($"Drawing companion {(IntPtr)currentChar->CompanionObject:X} {currentChar->CompanionObject->Character.GameObject.RenderFlags:X}");
                            currentChar->CompanionObject->Character.GameObject.EnableDraw();
                        }
                    }


                    if (camera != null)
                    {

                        var drawObject = (CharacterBase*)currentChar->GameObject.GetDrawObject();
                        var cameraFollowMode = GetCameraFollowMode();
                        Vector3 lookAt;
                        if (drawObject != null && currentChar->GameObject.RenderFlags == 0 && cameraFollowMode == CameraFollowMode.ModelPosition)
                        {
                            lookAt = drawObject->Skeleton->Transform.Position;
                        }
                        else
                        {
                            lookAt = currentChar->GameObject.Position;
                        }
                        lookAt.Y = camera->LobbyCamera.Camera.CameraBase.SceneCamera.LookAtVector.Y;
                        camera->LobbyCamera.Camera.CameraBase.SceneCamera.LookAtVector = OffsetPosition(lookAt);
                    }

                }
                if (camera != null)
                {
                    camera->midPoint.position = 10;
                    camera->highPoint.position = 20;
                    camera->LobbyCamera.Camera.MaxDistance = 20;
                }
            }
            else if (CurrentLobbyMap != (short)GameLobbyType.None)
            {
                if (camera != null)
                {
                    camera->midPoint.position = 3.3f;
                    camera->highPoint.position = 5.5f;
                    camera->LobbyCamera.Camera.MaxDistance = 5.5f;
                }
                rotationJustRecorded = false;
                lastYaw = 0;
                lastPitch = 0;
                lastDistance = 3.3f;
            }
        }

        private CameraFollowMode GetCameraFollowMode()
        {
            return locationModel.CameraFollowMode == CameraFollowMode.Inherit ? Services.ConfigurationService.CameraFollowMode : locationModel.CameraFollowMode; ;
        }


        public unsafe void ResetState()
        {
            lastContentId = 0;
            locationModel = GetNothingSelectedLocation();
            resetCamera = true;
        }

        private void SetTime(ushort time)
        {
            if (setTimeNative == null)
                throw new InvalidOperationException("SetTime signature wasn't found!");

            setTimeNative(time);
        }

        private void PickSong(ushort songIndex)
        {
            if (pickSongNative == null)
                throw new InvalidOperationException("SetTime signature wasn't found!");

            pickSongNative(*lobbyBgmBasePointerAddress, songIndex);
        }

        private int OnCreateSceneDetour(string territoryPath, uint p2, IntPtr p3, uint p4, IntPtr p5, int p6, uint p7)
        {
            //Log($"HandleCreateScene {p1} {p2} {p3.ToInt64():X} {p4} {p5.ToInt64():X} {p6} {p7}");
            //_titleCameraNeedsSet = false;
            //_amForcingTime = false;
            //_amForcingWeather = false;
            try
            {

                Services.Log.Debug($"Loading Scene {lastLobbyUpdateMapId}");
                var camera = GetCamera();
                if (resetCamera)
                {
                    if (camera != null)
                    {
                        camera->lowPoint.value = 1.4350828f + locationModel.Position.Y;
                        camera->midPoint.value = 0.85870504f + locationModel.Position.Y;
                        camera->highPoint.value = 0.6742642f + locationModel.Position.Y;
                        camera->LobbyCamera.Camera.CameraBase.SceneCamera.LookAtVector = new(locationModel.Position.X, locationModel.Position.Y, locationModel.Position.Z);
                    }

                    Services.Log.Debug($"Reset Lobby camera");
                    resetCamera = false;
                }
                if (lastLobbyUpdateMapId == GameLobbyType.CharaSelect)
                {

                    territoryPath = locationModel.TerritoryPath;
                    Services.Log.Debug($"Loading char select screen: {territoryPath}");
                    var returnVal = createSceneHook.Original(territoryPath, p2, p3, p4, p5, p6, p7);
                    if ((!locationModel.BgmPath.IsNullOrEmpty() && lastBgmPath != locationModel.BgmPath) || (locationModel.BgmPath.IsNullOrEmpty() && lastBgmPath != LocationService.DefaultLocation.BgmPath))
                    {
                        CurrentLobbyMusicIndex = 0;
                    }
                    return returnVal;
                }
                else if (lastSceneType == GameLobbyType.CharaSelect)
                {
                    // always reset camera when leaving character select
                    rotationJustRecorded = false;
                    if (camera != null)
                    {
                        camera->LobbyCamera.Camera.CameraBase.SceneCamera.LookAtVector = Vector3.Zero;
                    }
                    // making new char
                    if (lastLobbyUpdateMapId == GameLobbyType.Aetherial)
                    {
                        // The game doesn't call the function responsible for picking BGM when moving from char select to char creation
                        // Probably because it will already be playing the correct music
                        CurrentLobbyMusicIndex = 0;
                        PickSong(2);
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
                // Prevent overwriting camera location when going through characters rapidly (switching while a scene is still loading)
                if (!rotationJustRecorded)
                {
                    var camera = GetCamera();
                    if (camera != null)
                    {
                        lastYaw = camera->Yaw;
                        lastPitch = camera->Pitch;
                        lastDistance = camera->LobbyCamera.Camera.Distance;
                        rotationJustRecorded = true;
                        if (CharaSelectCharacterList.GetCurrentCharacter() != null)
                        {
                            lastYaw -= CharaSelectCharacterList.GetCurrentCharacter()->GameObject.Rotation;
                        }
                        lastYaw = Utils.NormalizeAngle(lastYaw);
                        Services.Log.Debug($"Recorded rotation {lastYaw} {lastPitch} {lastDistance}");
                    }
                }
                resetScene = false;
                CurrentLobbyMap = (short)GameLobbyType.None;
            }

            return lobbyUpdateHook.Original(mapId, time);
        }

        //SE's implenetation does nothing if value is below 0 which breaks the camera when character is in negative Y
        private void SetCameraCurveMidPointDetour(LobbyCameraExpanded* self, float value)
        {
            if (GetCameraFollowMode() == CameraFollowMode.ModelPosition)
            {
                cameraYOffset = Services.BoneService.GetHeadOffset(CharaSelectCharacterList.GetCurrentCharacter());
            }
            else
            {
                cameraYOffset = 0;
            }
            self->midPoint.value = value + cameraYOffset;
        }

        private void CalculateCameraCurveLowAndHighPointDetour(LobbyCameraExpanded* self, float value)
        {
            calculateCameraCurveLowAndHighPointHook.Original(self, value + cameraYOffset);
        }

        private ulong SelectCharacter2Detour(IntPtr p1)
        {
            Services.Log.Debug($"SelectCharacter2Detour");
            var result = selectCharacter2Hook.Original(p1);
            UpdateCharacter();
            return result;
        }

        private ulong SelectCharacterDetour(uint characterIndex, char p2)
        {
            Services.Log.Debug($"SelectCharacterDetour");
            var result = selectCharacterHook.Original(characterIndex, p2);
            Services.Log.Debug($"{result}");
            UpdateCharacter();
            return result;
        }

        private unsafe Character* GetCurrentCharacter()
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
                return (Character*)clientObjectManager->GetObjectByIndex((ushort)clientObjectIndex);
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

            var agentLobby = AgentLobby.Instance();
            if (character != null && agentLobby != null)
            {
                var contentId = agentLobby->LobbyData.CharaSelectEntries.Get((ulong)agentLobby->HoveredCharacterIndex).Value->ContentId;
                if (lastContentId != contentId)
                {
                    lastContentId = contentId;

                    var newLocationModel = GetLocationForContentId(contentId);
                    if (!newLocationModel.Equals(locationModel))
                    {
                        locationModel = newLocationModel;
                        resetScene = true;
                    }
                    Services.Log.Debug($"Setting character postion {(IntPtr)character:X}");
                    character->GameObject.SetPosition(locationModel.Position.X, locationModel.Position.Y, locationModel.Position.Z);
                    ((CharacterExpanded*)character)->MovementMode = locationModel.MovementMode;
                    if (character->Mount.MountId != locationModel.Mount.MountId)
                    {
                        SetupMount(character, locationModel);
                    }
                }
            }
        }

        public LocationModel GetLocationForContentId(ulong contentId)
        {
            var displayOverrideIdx = Services.ConfigurationService.DisplayTypeOverrides.FindIndex((entry) => entry.Key == contentId);
            DisplayTypeOption displayOption;
            LocationModel model;
            if (displayOverrideIdx != -1)
            {
                displayOption = Services.ConfigurationService.DisplayTypeOverrides[displayOverrideIdx].Value;
            }
            else
            {
                displayOption = Services.ConfigurationService.GlobalDisplayType;
            }
            if (displayOption.Type == DisplayType.LastLocation)
            {
                model = Services.LocationService.GetLocationModel(contentId);
            }
            else if (displayOption.Type == DisplayType.AetherialSea)
            {
                model = LocationService.DefaultLocation;
            }
            else
            {
                if (displayOption.PresetPath != null && Services.PresetService.Presets.TryGetValue(displayOption.PresetPath, out var preset))
                {
                    model = preset.LocationModel;
                    if (preset.LastLocationMount)
                    {
                        locationModel.Mount = Services.LocationService.GetLocationModel(contentId).Mount;
                    }
                    locationModel.CameraFollowMode = preset.CameraFollowMode;
                }
                else
                {
                    Services.Log.Error($"Preset \"{displayOption.PresetPath}\" not found");
                    model = LocationService.DefaultLocation;
                }
            }

            model.Position = OffsetPosition(model.Position);
            return model;
        }

        public LocationModel GetNothingSelectedLocation()
        {
            var displayOption = Services.ConfigurationService.NoCharacterDisplayType;
            LocationModel model;
            if (displayOption.Type == DisplayType.AetherialSea || displayOption.Type == DisplayType.LastLocation)
            {
                model = LocationService.DefaultLocation;
            }
            else
            {
                if (displayOption.PresetPath != null && Services.PresetService.Presets.TryGetValue(displayOption.PresetPath, out var preset))
                {
                    model = preset.LocationModel;
                    locationModel.CameraFollowMode = preset.CameraFollowMode;
                }
                else
                {
                    Services.Log.Error($"Preset \"{displayOption.PresetPath}\" not found");
                    model = LocationService.DefaultLocation;
                }
            }
            model.Position = OffsetPosition(model.Position);
            return model;
        }


        public void Apply(PresetModel preset)
        {
            var character = CharaSelectCharacterList.GetCurrentCharacter();
            var agentLobby = AgentLobby.Instance();
            locationModel = preset.LocationModel;
            locationModel.CameraFollowMode = preset.CameraFollowMode;
            locationModel.Position = OffsetPosition(locationModel.Position);
            Services.Log.Debug("Applying location model");
            if (character != null && agentLobby != null)
            {
                var contentId = agentLobby->LobbyData.CharaSelectEntries.Get((ulong)agentLobby->HoveredCharacterIndex).Value->ContentId;
                if (preset.LastLocationMount)
                {
                    locationModel.Mount = Services.LocationService.GetLocationModel((ulong)contentId).Mount;
                }
                Services.Log.Debug($"Setting character postion {(IntPtr)character:X}");
                character->GameObject.SetPosition(locationModel.Position.X, locationModel.Position.Y, locationModel.Position.Z);
                ((CharacterExpanded*)character)->MovementMode = locationModel.MovementMode;

                if (character->Mount.MountId != locationModel.Mount.MountId)
                {
                    SetupMount(character, locationModel);
                }


            }
            else
            {
                resetCamera = true;
            }
            resetScene = true;
        }

        private Vector3 OffsetPosition(Vector3 position)
        {
            // There is some weird issue when rapidly switching the camera while the world is loading 
            // and the camera focus is at (0,0,0)
            // that causes incredibly loud and persistent noises to start playing
            // we work around that by imperceivably offsetting the position

            if (position.X == 0) position.X = 0.001f;
            if (position.Y == 0) position.Y = 0.001f;
            if (position.Z == 0) position.Z = 0.001f;
            return position;
        }

        private void SetupMount(Character* character, LocationModel location)
        {
            character->Mount.CreateAndSetupMount(
                (short)locationModel.Mount.MountId,
                locationModel.Mount.BuddyModelTop,
                locationModel.Mount.BuddyModelBody,
                locationModel.Mount.BuddyModelLegs,
                locationModel.Mount.BuddyStain,
                0, 0);
        }

        private LobbyCameraExpanded* GetCamera()
        {
            var cameraManager = CameraManager.Instance();
            return cameraManager != null ? (LobbyCameraExpanded*)cameraManager->LobbCamera : null;
        }

        public override void Dispose()
        {
            base.Dispose();
            Services.Framework.Update -= Tick;
            Services.ClientState.Login -= ResetState;
        }
    }
}
