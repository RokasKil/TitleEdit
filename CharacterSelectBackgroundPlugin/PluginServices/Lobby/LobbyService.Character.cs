using CharacterSelectBackgroundPlugin.Data.Character;
using CharacterSelectBackgroundPlugin.Data.Lobby;
using CharacterSelectBackgroundPlugin.Data.Persistence;
using CharacterSelectBackgroundPlugin.Utility;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.Collections.Generic;
using System.Text;
using World = Lumina.Excel.GeneratedSheets.World;

namespace CharacterSelectBackgroundPlugin.PluginServices.Lobby
{
    public unsafe partial class LobbyService
    {

        private delegate ulong SelectCharacterDelegate(uint characterIndex, char p2);
        private delegate ulong SelectCharacter2Delegate(nint p1);
        private delegate nint CreateBattleCharacterDelegate(nint objectManager, uint index, bool assignCompanion);
        private delegate void SetCharSelectCurrentWorldDelegate(ulong p1);
        private delegate void CharSelectWorldPreviewEventHandlerDelegate(ulong p1, ulong p2, ulong p3, uint p4);

        private Hook<SelectCharacterDelegate> selectCharacterHook = null!;
        private Hook<SelectCharacter2Delegate> selectCharacter2Hook = null!;
        private Hook<CreateBattleCharacterDelegate> createBattleCharacterHook = null!;
        private Hook<SetCharSelectCurrentWorldDelegate> setCharSelectCurrentWorldHook = null!;
        private Hook<CharSelectWorldPreviewEventHandlerDelegate> charSelectWorldPreviewEventHandlerHook = null!;


        private bool creatingCharSelectGameObjects = false;
        private ulong lastContentId;

        private void HookCharacter()
        {
            // Happends on character list hover - update character Position, mount create mount if needed, change the scene if needed
            selectCharacterHook = Hook<SelectCharacterDelegate>("E8 ?? ?? ?? ?? 0F B6 D8 84 C0 75 ?? 49 8B CD", SelectCharacterDetour);

            // Happens on world list hover - update character Position, mount create mount if needed, change the scene if needed
            selectCharacter2Hook = Hook<SelectCharacter2Delegate>("40 53 48 83 EC ?? 41 83 C8 ?? 4C 8D 15", SelectCharacter2Detour);

            // Called when the game is making a new character - if set by other hooks we force the flag to include a companionObject so we can display a mount
            createBattleCharacterHook = Hook<CreateBattleCharacterDelegate>("E8 ?? ?? ?? ?? 83 F8 ?? 74 ?? 8B D0", CreateBattleCharacterDetour);

            // Called when you select a new world in character select or cancel selection so it reload the current
            // we use it make sure characters get created with a companion slots,
            // set the selected character cause SE doesn't do that (???) and initialize it
            setCharSelectCurrentWorldHook = Hook<SetCharSelectCurrentWorldDelegate>("E8 ?? ?? ?? ?? 49 8B CD 48 8B 7C 24", SetCharSelectCurrentWorldDetour);

            // Happens on world list hover when loading a world - we use it make sure characters get created with a companion slots (maybe makes selectCharacter2Hook redundant)
            charSelectWorldPreviewEventHandlerHook = Hook<CharSelectWorldPreviewEventHandlerDelegate>("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 41 83 FE ?? 0F 8C", CharSelectWorldPreviewEventHandlerDetour);
        }

        private void CharacterTick()
        {
            var currentChar = CharaSelectCharacterList.GetCurrentCharacter();
            if (lastContentId != 0 && currentChar == null)
            {
                NothingSelected();
            }
            // We do a slight polling cause it's simpler than figuring when exactly are mounts and stuff are good to draw
            if (CurrentLobbyMap == GameLobbyType.CharaSelect)
            {
                if (currentChar != null)
                {
                    if (currentChar->GameObject.RenderFlags != 0 && currentChar->GameObject.RenderFlags != 0x40 && currentChar->GameObject.IsReadyToDraw())
                    {
                        Services.Log.Debug($"Drawing character {(nint)currentChar:X} {currentChar->GameObject.RenderFlags:X}");
                        currentChar->GameObject.EnableDraw();
                        if (currentChar->IsMounted() && currentChar->CompanionObject != null && currentChar->CompanionObject->Character.GameObject.IsReadyToDraw())
                        {
                            Services.Log.Debug($"Drawing companion {(nint)currentChar->CompanionObject:X} {currentChar->CompanionObject->Character.GameObject.RenderFlags:X}");
                            currentChar->CompanionObject->Character.GameObject.EnableDraw();
                        }
                    }
                    // Tell camera to follow the character
                    CameraFollowCharacter(currentChar);
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

            *CharaSelectCharacterList.StaticAddressPointers.ppGetCurrentCharacter = GetCurrentCharacter();
            Services.Log.Debug($"Set current char to {(nint)(*CharaSelectCharacterList.StaticAddressPointers.ppGetCurrentCharacter):X}");
            UpdateCharacter(true);
        }
        public unsafe void NothingSelected()
        {
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
                Services.Log.Warning($"[getCurrentCharacter] failed to get instance  {(nint)agentLobby:X} {(nint)charaSelectCharacterList:X} {(nint)clientObjectManager:X}");

            }
            return null;
        }

        private unsafe void UpdateCharacter(bool forced = false)
        {
            var character = CharaSelectCharacterList.GetCurrentCharacter();

            var agentLobby = AgentLobby.Instance();
            if (character != null && agentLobby != null)
            {
                var contentId = agentLobby->LobbyData.CharaSelectEntries.Get((ulong)agentLobby->HoveredCharacterIndex).Value->ContentId;
                if (lastContentId != contentId || forced)
                {
                    lastContentId = contentId;

                    var newLocationModel = GetLocationForContentId(contentId);
                    if (!newLocationModel.Equals(locationModel))
                    {
                        locationModel = newLocationModel;
                        resetScene = true;
                    }
                    Services.Log.Debug($"Setting character postion {(nint)character:X}");
                    character->GameObject.SetPosition(locationModel.Position.X, locationModel.Position.Y, locationModel.Position.Z);
                    ((CharacterExpanded*)character)->MovementMode = locationModel.MovementMode;
                    if (character->Mount.MountId != locationModel.Mount.MountId)
                    {
                        SetupMount(character, locationModel);
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
    }
}
