using TitleEdit.Data.Character;
using TitleEdit.Data.Lobby;
using TitleEdit.Data.Persistence;
using TitleEdit.Utility;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System;
using System.Collections.Generic;
using System.Text;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using World = Lumina.Excel.GeneratedSheets.World;

namespace TitleEdit.PluginServices.Lobby
{
    public unsafe partial class LobbyService
    {
        private delegate ulong SelectCharacterDelegate(uint characterIndex, char p2);
        private delegate ulong SelectCharacter2Delegate(nint p1);
        private delegate void UpdateCharaSelectDisplayDelegate(IntPtr agentLobby, byte p2, byte p3);
        private delegate nint CreateBattleCharacterDelegate(nint objectManager, uint index, bool assignCompanion);
        private delegate void SetCharSelectCurrentWorldDelegate(ulong p1);
        private delegate void CharSelectWorldPreviewEventHandlerDelegate(ulong p1, ulong p2, ulong p3, uint p4);

        private Hook<SelectCharacterDelegate> selectCharacterHook = null!;
        private Hook<SelectCharacter2Delegate> selectCharacter2Hook = null!;
        private Hook<UpdateCharaSelectDisplayDelegate> updateCharaSelectDisplayHook = null!;
        private Hook<CreateBattleCharacterDelegate> createBattleCharacterHook = null!;
        private Hook<SetCharSelectCurrentWorldDelegate> setCharSelectCurrentWorldHook = null!;
        private Hook<CharSelectWorldPreviewEventHandlerDelegate> charSelectWorldPreviewEventHandlerHook = null!;

        public Character* CurrentCharacter => CharaSelectCharacterList.GetCurrentCharacter();


        private bool creatingCharSelectGameObjects = false;
        private ulong lastContentId;

        // Set every tick when CurrentCharacter is not null and reset when going from nothing selected to displaying a character (because we're restoring the angle specifically from camera)
        private float lastCharacterRotation = 0;

        private void HookCharacter()
        {
            /// I think the bit for selecting character got inlined... :(
            // Happends on character list hover - update character Position, mount create mount if needed, change the scene if needed
            /// TODO: check if new sig works, I don't see any method calling this anymore
            selectCharacterHook = Hook<SelectCharacterDelegate>("48 89 5C 24 ?? 57 48 83 EC ?? 48 8B 1D ?? ?? ?? ?? 0F B6 FA 83 F9", SelectCharacterDetour);

            // Happens on world list hover - update character Position, mount create mount if needed, change the scene if needed
            /// TODO: check if new sig works, I don't see any method calling this anymore
            selectCharacter2Hook = Hook<SelectCharacter2Delegate>("40 53 48 83 EC ?? 33 D2 4C 8D 15", SelectCharacter2Detour);

            updateCharaSelectDisplayHook = Hook<UpdateCharaSelectDisplayDelegate>("E8 ?? ?? ?? ?? 84 C0 74 ?? C6 86 ?? ?? ?? ?? ?? 48 8B 8C 24", UpdateCharaSelectDisplayDetour);

            // Called when the game is making a new character - if set by other hooks we force the flag to include a companionObject so we can display a mount
            createBattleCharacterHook = Hook<CreateBattleCharacterDelegate>("E8 ?? ?? ?? ?? 8B D0 41 89 44", CreateBattleCharacterDetour);

            // Called when you select a new world in character select or cancel selection so it reload the current
            // we use it make sure characters get created with a companion slots,
            // set the selected character cause SE doesn't do that (???) and initialize it
            setCharSelectCurrentWorldHook = Hook<SetCharSelectCurrentWorldDelegate>("E8 ?? ?? ?? ?? 49 8B CD 4C 8B 74 24", SetCharSelectCurrentWorldDetour);

            // Happens on world list hover when loading a world - we use it make sure characters get created with a companion slots (maybe makes selectCharacter2Hook redundant)
            charSelectWorldPreviewEventHandlerHook = Hook<CharSelectWorldPreviewEventHandlerDelegate>("E8 ?? ?? ?? ?? 49 8B CD E8 ?? ?? ?? ?? 41 0F B6 85", CharSelectWorldPreviewEventHandlerDetour);
        }

        private void UpdateCharaSelectDisplayDetour(nint agentLobby, byte p2, byte p3)
        {
            //Services.Log.Debug($"UpdateCharaSelectDisplayDetour {p2}, {p3}");
            var preUpdateCharacter = CurrentCharacter;
            updateCharaSelectDisplayHook.Original(agentLobby, p2, p3);
            if (preUpdateCharacter != CurrentCharacter)
            {
                Services.Log.Debug($"CurrentChar changed {(IntPtr)preUpdateCharacter:X} - {(IntPtr)CurrentCharacter:X}");
                if (lastContentId == 0)
                {
                    Services.Log.Debug($"Reseting last character rotation {lastCharacterRotation}");
                    lastCharacterRotation = 0;
                }
                UpdateCharacter();
            }
        }

        private void CharacterTick()
        {
            // We do polling cause it's simpler than figuring when exactly are mounts and stuff are good to draw
            if (CurrentLobbyMap == GameLobbyType.CharaSelect)
            {
                if (CurrentCharacter != null)
                {
                    // Don't record rotation when loading next scene
                    if (!rotationJustRecorded && !resetScene)
                    {
                        lastCharacterRotation = CurrentCharacter->Rotation;
                    }
                    if (CurrentCharacter->GameObject.RenderFlags != 0 && CurrentCharacter->GameObject.RenderFlags != 0x40 && CurrentCharacter->GameObject.IsReadyToDraw())
                    {
                        Services.Log.Debug($"Drawing character {(nint)CurrentCharacter:X} {CurrentCharacter->GameObject.RenderFlags:X}");
                        CurrentCharacter->GameObject.EnableDraw();
                        if (CurrentCharacter->IsMounted() && CurrentCharacter->CompanionObject != null && CurrentCharacter->CompanionObject->Character.GameObject.IsReadyToDraw())
                        {
                            Services.Log.Debug($"Drawing companion {(nint)CurrentCharacter->CompanionObject:X} {CurrentCharacter->CompanionObject->Character.GameObject.RenderFlags:X}");
                            CurrentCharacter->CompanionObject->Character.GameObject.EnableDraw();
                        }
                    }
                    // Tell camera to follow the character
                    CameraFollowCharacter(CurrentCharacter);
                }
                else
                {
                    // Tell camera to look at last recorded position
                    CameraLookAtLastPosition();
                }
            }
        }

        public Dictionary<ulong, string> GetCurrentCharacterNames()
        {
            Dictionary<ulong, string> result = [];
            if (CurrentLobbyMap != GameLobbyType.CharaSelect) return result;
            var agentLobby = AgentLobby.Instance();
            if (agentLobby != null)
            {
                var characterSelects = agentLobby->LobbyData.CharaSelectEntries.AsSpan();
                foreach (var character in characterSelects)
                {
                    if (character.Value->ContentId != 0)
                    {
                        var world = Services.DataManager.GetExcelSheet<World>()?.GetRow(character.Value->HomeWorldId);
                        if (world != null)
                        {
                            result[character.Value->ContentId] = $"{Encoding.UTF8.GetString(character.Value->Name).TrimEnd('\0')}@{world.Name}";
                        }
                    }
                }
            }
            return result;
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

            *CharaSelectCharacterList.StaticAddressPointers.ppGetCurrentCharacter = GetCurrentHoveredCharacter();

            Services.Log.Debug($"Set current char to {(nint)(*CharaSelectCharacterList.StaticAddressPointers.ppGetCurrentCharacter):X}");
            UpdateCharacter(true);
            RotateCharacter();
        }

        private void RotateCharacter()
        {
            if (CurrentCharacter != null)
            {
                CurrentCharacter->GameObject.SetRotation(locationModel.Rotation);
            }
        }

        // Used to reset state when unloading
        private void ResetCharacters()
        {
            var charaSelectCharacterList = CharaSelectCharacterList.Instance();
            var clientObjectManager = ClientObjectManager.Instance();
            if (charaSelectCharacterList != null && clientObjectManager != null)
            {
                for (int i = 0; i < charaSelectCharacterList->CharacterMapping.Length; i++)
                {
                    if (charaSelectCharacterList->CharacterMapping[i].ContentId == 0)
                    {
                        break;
                    }
                    var clientObjectIndex = charaSelectCharacterList->CharacterMapping[i].ClientObjectIndex;
                    var gameObject = clientObjectManager->GetObjectByIndex((ushort)clientObjectIndex);
                    if (gameObject != null)
                    {
                        gameObject->SetPosition(0, 0, 0);
                        ((Character*)gameObject)->Mount.CreateAndSetupMount(0, 0, 0, 0, 0, 0, 0);
                    }
                }
            }
        }


        private void NothingSelected()
        {
            Services.Log.Debug("Nothing selected");
            lastContentId = 0;
            var newLocationModel = GetNothingSelectedLocation();
            if (!newLocationModel.Equals(locationModel))
            {
                locationModel = GetNothingSelectedLocation();
                resetScene = true;

            }
        }

        private void CharSelectWorldPreviewEventHandlerDetour(ulong p1, ulong p2, ulong p3, uint p4)
        {
            creatingCharSelectGameObjects = true;
            charSelectWorldPreviewEventHandlerHook.Original(p1, p2, p3, p4);
            creatingCharSelectGameObjects = false;
        }

        private nint CreateBattleCharacterDetour(nint objectManager, uint index, bool assignCompanion)
        {
            if (creatingCharSelectGameObjects)
            {
                Services.Log.Debug("[CreateBattleCharacterDetour] setting assignCompanion");
            }
            return createBattleCharacterHook.Original(objectManager, index, assignCompanion || creatingCharSelectGameObjects);
        }


        private ulong SelectCharacter2Detour(nint p1)
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
            UpdateCharacter();
            return result;
        }

        //Get current hovered character by it's content id because the index is set to 100 when flipping through worlds
        private Character* GetCurrentHoveredCharacter()
        {

            var agentLobby = AgentLobby.Instance();
            var charaSelectCharacterList = CharaSelectCharacterList.Instance();
            var clientObjectManager = ClientObjectManager.Instance();
            if (agentLobby != null && charaSelectCharacterList != null && clientObjectManager != null)
            {
                if (agentLobby->HoveredCharacterContentId == 0)
                {
                    return null;
                }
                for (var i = 0; i < charaSelectCharacterList->CharacterMapping.Length; i++)
                {
                    if (charaSelectCharacterList->CharacterMapping[i].ContentId == agentLobby->HoveredCharacterContentId)
                    {
                        return (Character*)clientObjectManager->GetObjectByIndex((ushort)charaSelectCharacterList->CharacterMapping[i].ClientObjectIndex);
                    }
                }
            }
            else
            {
                Services.Log.Warning($"[getCurrentCharacter] failed to get instance  {(nint)agentLobby:X} {(nint)charaSelectCharacterList:X} {(nint)clientObjectManager:X}");

            }
            return null;
        }

        private void UpdateCharacter(bool forced = false)
        {
            if (CurrentCharacter != null)
            {
                var contentId = GetContentId();
                if (lastContentId != contentId || forced)
                {
                    lastContentId = contentId;

                    var newLocationModel = GetLocationForContentId(contentId);
                    if (!newLocationModel.Equals(locationModel))
                    {
                        locationModel = newLocationModel;
                        resetScene = true;
                    }
                    Services.Log.Debug($"Setting character postion {(nint)CurrentCharacter:X}");
                    CurrentCharacter->GameObject.SetPosition(locationModel.Position.X, locationModel.Position.Y, locationModel.Position.Z);
                    ((CharacterExpanded*)CurrentCharacter)->MovementMode = locationModel.MovementMode;
                    if (CurrentCharacter->Mount.MountId != locationModel.Mount.MountId)
                    {
                        SetupMount(CurrentCharacter, locationModel);
                    }
                }
            }
            else
            {
                NothingSelected();
            }
        }

        private ulong GetContentId()
        {
            var agentLobby = AgentLobby.Instance();
            if (agentLobby != null)
            {
                Services.Log.Debug($"Getting content id of {agentLobby->HoveredCharacterIndex} : {agentLobby->HoveredCharacterContentId:X} : {agentLobby->LobbyData.CharaSelectEntries.LongCount}");
                return agentLobby->HoveredCharacterContentId;//agentLobby->LobbyData.CharaSelectEntries[agentLobby->HoveredCharacterIndex].Value->ContentId;
            }
            return 0;
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
    }
}
