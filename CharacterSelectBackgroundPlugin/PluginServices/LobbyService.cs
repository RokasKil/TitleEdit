using CharacterSelectBackgroundPlugin.Data;
using CharacterSelectBackgroundPlugin.Data.Layout;
using CharacterSelectBackgroundPlugin.Utility;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace CharacterSelectBackgroundPlugin.PluginServices
{
    public unsafe class LobbyService : AbstractService
    {
        [Signature("40 53 48 83 EC ?? 44 0F BF C1")]
        private readonly delegate* unmanaged<ushort, void> setTimeNative = null!;

        private delegate int OnCreateSceneDelegate(string territoryPath, uint p2, IntPtr p3, uint p4, IntPtr p5, int p6, uint p7);
        private delegate byte LobbyUpdateDelegate(GameLobbyType mapId, int time);
        private delegate ulong SelectCharacterDelegate(uint characterIndex, char p2);
        private delegate ulong SelectCharacter2Delegate(IntPtr p1);
        private unsafe delegate void SetCameraCurveMidPointDelegate(LobbyCameraExpanded* self, float value);
        private delegate void SetCharSelectCurrentWorldDelegate(ulong p1);
        private delegate void SomeEnvManagerThingyDelegate(ulong p1, uint p2, float p3);
        private delegate ulong WeatherThingyDelegate(ulong p1, byte weatherId);
        private delegate void CharSelectSetWeatherDelegate();
        private delegate IntPtr PlayMusicDelegate(IntPtr self, string filename, float volume, uint fadeTime);
        private delegate IntPtr CreateBattleCharacterDelegate(IntPtr objectManager, uint index, bool assignCompanion);
        private delegate void CharSelectWorldPreviewEventHandlerDelegate(ulong p1, ulong p2, ulong p3, uint p4);

        private readonly Hook<OnCreateSceneDelegate> createSceneHook;
        private readonly Hook<LobbyUpdateDelegate> lobbyUpdateHook;
        private readonly Hook<SelectCharacterDelegate> selectCharacterHook;
        private readonly Hook<SelectCharacter2Delegate> selectCharacter2Hook;
        private readonly Hook<SetCameraCurveMidPointDelegate> setCameraCurveMidPointHook;
        private readonly Hook<SetCharSelectCurrentWorldDelegate> setCharSelectCurrentWorldHook;
        private readonly Hook<CharSelectSetWeatherDelegate> charSelectSetWeatherHook;
        private readonly Hook<PlayMusicDelegate> playMusicHook;
        private readonly Hook<CreateBattleCharacterDelegate> createBattleCharacterHook;
        private readonly Hook<CharSelectWorldPreviewEventHandlerDelegate> charSelectWorldPreviewEventHandlerHook;

        private GameLobbyType lastLobbyUpdateMapId = GameLobbyType.None;
        private ulong lastContentId;
        private LocationModel locationModel = LocationService.DefaultLocation;

        private string? lastBgmPath;

        private bool resetScene = false;
        private bool resetCamera = false;

        private bool renderingSelected = false;

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
        // calls the method hooked at playMusicHook with selected path and stores the result at 0x18 with the index being stored at 0x20
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

            // Points to a value that says what type of lobby map is being displayer
            lobbyCurrentMapAddress = Utils.GetStaticAddressFromSigOrThrow("0F B7 05 ?? ?? ?? ?? 49 8B CE");
            // Points to a value that indicates the current lobby bgm type that's playing, we maniplate this to force bgm change alongside playMusicHook
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
            setCameraCurveMidPointHook = Hook<SetCameraCurveMidPointDelegate>("0F 57 C0 0F 2F C1 73 ?? F3 0F 11 89", SetCameraCurveMidPointDetour);

            // Called when you select a new world in character select or cancel selection so it reload the current - we use it make sure characters get created with a companion slots, initialze their positions and mounts
            setCharSelectCurrentWorldHook = Hook<SetCharSelectCurrentWorldDelegate>("E8 ?? ?? ?? ?? 49 8B CD 48 8B 7C 24", SetCharSelectCurrentWorldDetour);

            // Called when game does some lobby weather setting - we use it as an indicator to set scene details like weather, time and layout 
            charSelectSetWeatherHook = Hook<CharSelectSetWeatherDelegate>("0F B7 0D ?? ?? ?? ?? 8D 41", CharSelectSetWeatherDetour);

            // Called when lobby music needs to be changed - we force call the game to call it by resetting the CurrentLobbyMusicIndex pointer
            playMusicHook = Hook<PlayMusicDelegate>("E8 ?? ?? ?? ?? 48 89 47 18 89 5F 20", PlayMusicDetour);

            // Called when the game is making a new character - if set by other hooks we force the flag to include a companionObject so we can display a mount
            createBattleCharacterHook = Hook<CreateBattleCharacterDelegate>("E8 ?? ?? ?? ?? 83 F8 ?? 74 ?? 8B D0", CreateBattleCharacterDetour);

            // Happens on world list hover when loading a world - we use it make sure characters get created with a companion slots (maybe makes selectCharacter2Hook redundant)
            charSelectWorldPreviewEventHandlerHook = Hook<CharSelectWorldPreviewEventHandlerDelegate>("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 41 83 FE ?? 0F 8C", CharSelectWorldPreviewEventHandlerDetour);

            EnableHooks();

            Services.ClientState.Login += ResetState;
            Services.Framework.Update += Tick;
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
            Services.Log.Debug($"CharSelectSetWeatherDetour {EnvManager.Instance()->ActiveWeather}");
            if (CurrentLobbyMap == (short)GameLobbyType.CharaSelect)
            {
                fixed (uint* pFestivals = locationModel.Festivals)
                {
                    Services.LayoutService.LayoutManager->layoutManager.SetActiveFestivals(pFestivals);
                }
                EnvManager.Instance()->ActiveWeather = locationModel.WeatherId;
                setTime(locationModel.TimeOffset);
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
                    var location = Services.LocationService.GetLocationModel(contentId);
                    var gameObject = clientObjectManager->GetObjectByIndex((ushort)clientObjectIndex);
                    if (gameObject != null)
                    {
                        gameObject->SetPosition(location.Position.X, location.Position.Y, location.Position.Z);

                        if (gameObject->IsCharacter()) //Probably useless check?
                        {
                            CharacterExpanded* character = (CharacterExpanded*)gameObject;
                            character->movementMode = locationModel.MovementMode;
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

        public void Tick(IFramework framework)
        {
            var currentChar = CharaSelectCharacterList.GetCurrentCharacter();
            if (lastContentId != 0 && currentChar == null)
            {
                ResetState();
                resetScene = true;
            }
            // We do a slight polling cause it's simpler than figuring when exactly are mounts and stuff are good to draw
            if (CurrentLobbyMap == (short)GameLobbyType.CharaSelect && currentChar != null)
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
            }
        }

        public unsafe void ResetState()
        {
            lastContentId = 0;
            locationModel = LocationService.DefaultLocation;
            resetCamera = true;
        }

        public void setTime(ushort time)
        {
            if (setTimeNative == null)
                throw new InvalidOperationException("SetTime signature wasn't found!");

            setTimeNative(time);
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
                if ((!locationModel.BgmPath.IsNullOrEmpty() && lastBgmPath != locationModel.BgmPath) || (locationModel.BgmPath.IsNullOrEmpty() && lastBgmPath != LocationService.DefaultLocation.BgmPath))
                {
                    CurrentLobbyMusicIndex = 0;
                }
                //SetWeather();
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
                CurrentLobbyMap = (short)GameLobbyType.None;
            }

            return lobbyUpdateHook.Original(mapId, time);
        }

        //SE's implenetation does nothing if value is below 0 which breaks the camera when character is in negative Y
        private unsafe void SetCameraCurveMidPointDetour(LobbyCameraExpanded* self, float value)
        {
            self->midPoint.value = value;
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

            var agentLobby = AgentLobby.Instance();
            if (character != null && agentLobby != null)
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
                    ((CharacterExpanded*)character)->movementMode = locationModel.MovementMode;
                    if (locationModel.Mount.MountId != 0)
                    {
                        if (character->Mount.MountId == 0)
                        {
                            SetupMount(character, locationModel);
                        }
                    }
                }
            }

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
        public override void Dispose()
        {
            base.Dispose();
            Services.ClientState.Login -= ResetState;
        }
    }
}
