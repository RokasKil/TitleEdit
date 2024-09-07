using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TitleEdit.Data.BGM;
using TitleEdit.Data.Character;
using TitleEdit.Data.Lobby;
using TitleEdit.Data.Persistence;
using TitleEdit.Utility;

namespace TitleEdit.Windows.Tabs
{
    //TODO: split this class a bit
    internal class PresetTab : AbstractTab
    {
        public override string Title => "Presets";

        private PresetModel preset = new();
        private string currentPreset = "";
        private bool makingNewPreset = false;
        private bool liveEditing = false;
        private LocationType liveEditingLocationType;

        private readonly Dictionary<uint, string> mounts;
        private readonly Dictionary<byte, string> weathers;

        private FileDialogManager fileDialogManager = new();

        public PresetTab()
        {
            mounts = Services.DataManager.GetExcelSheet<Mount>()!.ToDictionary(mount => mount.RowId, mount =>
            {
                if (mount.RowId == 0)
                {
                    return "0 - None";
                }
                return $"{mount.RowId} - " + string.Join(" ", mount.Singular.RawString.Split().Select(part =>
                {
                    if (part.Length > 0)
                    {
                        return part[0].ToString().ToUpper() + part.Substring(1);
                    }
                    return part;
                }));
            });

            weathers = Services.DataManager.GetExcelSheet<Weather>()!.ToDictionary(weather => (byte)weather.RowId, weather => $"{weather.RowId} - {(!string.IsNullOrEmpty(weather.Name) ? weather.Name : "Unknown")}");
        }

        public override void Draw()
        {
            base.Draw();
            fileDialogManager.Draw();
            DrawPresetListControls();
            ImGui.Separator();
            DrawPresetControls();
            DrawPresetActions();
        }

        private string GetPresetEntryName(PresetModel preset)
        {
            var prefix = preset.LocationModel.LocationType switch
            {
                LocationType.CharacterSelect => "C",
                LocationType.TitleScreen => "T",
                _ => "?"
            };
            return $"[{prefix}] {preset.Name}";
        }

        private void DrawPresetListControls()
        {
            string currentPresetName;
            if (makingNewPreset)
            {
                currentPresetName = "Unsaved preset";
                ImGui.PushStyleColor(ImGuiCol.Text, GuiUtils.WarningColor);
            }
            else if (currentPreset == "")
            {
                currentPresetName = "";
            }
            else
            {
                currentPresetName = GetPresetEntryName(Services.PresetService.Presets.GetValueOrDefault(currentPreset));
            }
            bool stylePopped = false;

            GuiUtils.FilterCombo($"##{Title}##presetCombo", currentPresetName, () =>
            {
                if (makingNewPreset)
                {
                    ImGui.PopStyleColor();
                    stylePopped = true;
                }
                foreach (var entry in Services.PresetService.EditablePresetEnumerator)
                {
                    if (GuiUtils.FilterSelectable($"{GetPresetEntryName(entry.Value)}##{Title}##{entry.Key}", entry.Key == currentPreset))
                    {
                        SelectPreset(entry.Key);
                        return true;
                    }
                    GuiUtils.DrawPresetTooltip(entry.Key);
                }
                return false;
            });
            if (makingNewPreset && !stylePopped)
            {
                ImGui.PopStyleColor();
            }
            ImGui.SameLine();
            ImGui.BeginDisabled(Services.ClientState.LocalPlayer == null || makingNewPreset);
            if (ImGuiComponents.IconButton($"##{Title}##NewPreset", FontAwesomeIcon.Plus))
            {
                NewPreset();
            }
            ImGui.EndDisabled();
            GuiUtils.HoverTooltip("Create new preset", ImGuiHoveredFlags.AllowWhenDisabled);
            ImGui.SameLine();
            ImGui.BeginDisabled(makingNewPreset || currentPreset == "");
            if (ImGuiComponents.IconButton($"##{Title}##Duplicate", FontAwesomeIcon.Copy))
            {
                DuplicatePreset(currentPreset);
            }
            ImGui.EndDisabled();
            GuiUtils.HoverTooltip("Duplicate preset");
            ImGui.BeginDisabled(makingNewPreset || currentPreset == "");
            if (ImGui.Button($"Export to clipboard##{Title}"))
            {
                try
                {
                    ImGui.SetClipboardText(Services.PresetService.ExportText(currentPreset!));
                }
                catch (Exception ex)
                {
                    SetupError(ex);
                }

            }
            ImGui.SameLine();
            if (ImGui.Button($"Export to file##{Title}"))
            {
                fileDialogManager.SaveFileDialog($"Export preset##{Title}", ".json", preset.FileName, ".json", ExportPreset, null, true);
            }
            ImGui.EndDisabled();
            if (ImGui.Button($"Import from clipboard##{Title}"))
            {
                try
                {
                    SelectPreset(Services.PresetService.ImportText(ImGui.GetClipboardText()));
                    SetupImportSuccess();
                }
                catch (Exception ex)
                {
                    SetupError(ex);
                }
            }
            ImGui.SameLine();
            if (ImGui.Button($"Import from file##{Title}"))
            {
                fileDialogManager.OpenFileDialog($"Import preset##{Title}", ".json", ImportPreset, 1, null, true);
            }
        }

        private void DrawPresetControls()
        {
            var buttonPosX = ImGui.GetWindowContentRegionMin().X + (ImGui.CalcTextSize("Character state")).X + (16 * ImGui.GetFontSize()) + ImGui.GetStyle().ItemInnerSpacing.X + ImGui.GetStyle().ItemSpacing.X;


            ImGui.BeginDisabled(!makingNewPreset && currentPreset == "");
            // Preset metadata
            ImGui.InputText($"Name##{Title}", ref preset.Name, 256);
            ImGui.InputText($"Author##{Title}", ref preset.Author, 256);
            using (ImRaii.Disabled(liveEditing))
            {
                GuiUtils.Combo($"Preset Type##{Title}", ref preset.LocationModel.LocationType);
            }

            // World stuff
            // Zone
            var territory = Services.DataManager.GetExcelSheet<TerritoryType>()!.GetRow(preset.LocationModel.TerritoryTypeId);
            if (territory != null)
            {
                ImGui.TextUnformatted($"Zone: {territory.RowId} - {territory.PlaceNameRegion.Value?.Name} > {territory.PlaceName.Value?.Name}");
            }
            else
            {
                ImGui.TextUnformatted($"Zone: Unknown");
            }
            if (Services.ClientState.LocalPlayer != null)
            {
                ImGui.SameLine();
                ImGui.SetCursorPosX(buttonPosX);
                if (ImGui.Button($"Apply current##{Title}##Zone"))
                {
                    LoadCurrentTerritory();
                }
                var currentTerritory = Services.DataManager.GetExcelSheet<TerritoryType>()!.GetRow(Services.ClientState.TerritoryType);

                if (currentTerritory != null)
                {
                    GuiUtils.HoverTooltip($"Current zone: {currentTerritory.RowId} - {currentTerritory.PlaceNameRegion.Value?.Name} > {currentTerritory.PlaceName.Value?.Name}");
                }
                else
                {
                    GuiUtils.HoverTooltip($"Zone: Unknown");
                }
            }

            // Weather
            var weatherId = weathers.ContainsKey(preset.LocationModel.WeatherId) ? preset.LocationModel.WeatherId : 2;
            GuiUtils.Combo($"Weather##{Title}", weathers[preset.LocationModel.WeatherId], () =>
            {
                foreach (var rowId in Services.WeatherService.GetWeathers(preset.LocationModel.TerritoryPath))
                {
                    if (weathers.TryGetValue(rowId, out var weather))
                    {
                        if (ImGui.Selectable($"{weather}##{Title}##{rowId}", rowId == preset.LocationModel.WeatherId))
                        {
                            preset.LocationModel.WeatherId = rowId;
                            UpdateLiveEdit();
                            Services.LobbyService.UpdateLiveEditWeather(liveEditingLocationType);
                            return true;
                        }
                    }

                }
                return false;
            });
            if (Services.ClientState.LocalPlayer != null)
            {
                ImGui.SameLine();
                ImGui.SetCursorPosX(buttonPosX);
                if (ImGui.Button($"Apply current##{Title}##Weather"))
                {
                    preset.LocationModel.WeatherId = Services.WeatherService.WeatherId;
                }
                GuiUtils.HoverTooltip($"Current weather: {weathers[Services.WeatherService.WeatherId]}");
            }

            // Time
            // time is stored as a 2 byte integer with HHMM structure
            var time = (int)preset.LocationModel.TimeOffset / 100 * 60 + preset.LocationModel.TimeOffset % 100;
            if (!liveEditing)
            {
                if (ImGui.SliderInt($"Time##{Title}", ref time, 0, 24 * 60 - 1, $"{time / 60:00}:{(time % 60):00}", ImGuiSliderFlags.AlwaysClamp | ImGuiSliderFlags.NoInput))
                {
                    preset.LocationModel.TimeOffset = (ushort)(time / 60 * 100 + time % 60);
                }
            }
            else
            {
                if (ImGui.DragInt($"Time##LiveEdit##{Title}", ref time, 2, int.MinValue, int.MaxValue, $"{time / 60:00}:{(time % 60):00}", ImGuiSliderFlags.NoInput))
                {
                    time = (ushort)(Utils.Modulo(time, 24 * 60 - 1));
                    preset.LocationModel.TimeOffset = (ushort)(time / 60 * 100 + time % 60);
                    UpdateLiveEdit();
                    Services.LobbyService.UpdateLiveEditTime(liveEditingLocationType);
                }
            }
            if (Services.ClientState.LocalPlayer != null)
            {
                var currentTime = Services.LocationService.TimeOffset;
                ImGui.SameLine();
                ImGui.SetCursorPosX(buttonPosX);
                if (ImGui.Button($"Apply current##{Title}##Time"))
                {
                    preset.LocationModel.TimeOffset = (ushort)currentTime;
                }
                GuiUtils.HoverTooltip($"Current time: {time / 60:00}:{(time % 60):00}");
            }

            // Song
            var bgmId = Services.BgmService.Bgms.ContainsKey(preset.LocationModel.BgmId) ? preset.LocationModel.BgmId : 0;
            GuiUtils.FilterCombo($"Song##{Title}", Services.BgmService.Bgms[bgmId].DisplayName, () =>
            {

                foreach (var entry in Services.BgmService.Bgms)
                {
                    if (GuiUtils.FilterSelectable($"{entry.Value.DisplayName}##{Title}##{entry.Key}", entry.Key == bgmId))
                    {
                        preset.LocationModel.BgmId = entry.Key;
                        preset.LocationModel.BgmPath = entry.Value.FilePath;

                        UpdateLiveEdit();
                        Services.LobbyService.UpdateLiveEditBgm(liveEditingLocationType);
                        return true;
                    }
                    DrawBgmTooltip(entry.Value);

                }
                return false;

            });
            DrawBgmTooltip(Services.BgmService.Bgms[bgmId]);
            if (Services.ClientState.LocalPlayer != null)
            {
                ImGui.SameLine();
                ImGui.SetCursorPosX(buttonPosX);
                if (ImGui.Button($"Apply current##{Title}##BGM"))
                {
                    preset.LocationModel.BgmId = Services.BgmService.CurrentSongId;
                    preset.LocationModel.BgmPath = Services.DataManager.GetExcelSheet<BGM>()!.GetRow((uint)Services.BgmService.CurrentSongId)?.File.ToString();
                }
                if (Services.BgmService.Bgms.TryGetValue(Services.BgmService.CurrentSongId, out var bgm))
                {
                    GuiUtils.HoverTooltip($"Current song: {bgm.DisplayName}");
                }
                else
                {
                    GuiUtils.HoverTooltip($"Current song: unknown");
                }
            }

            //Character stuff
            if (preset.LocationModel.LocationType == LocationType.CharacterSelect)
            {
                //Position
                if (!liveEditing)
                {
                    ImGui.InputFloat3($"Position##{Title}", ref preset.LocationModel.Position);
                }
                else if (ImGui.DragFloat3($"Position##LiveEdit##{Title}", ref preset.LocationModel.Position, 0.1f))
                {
                    UpdateLiveEdit();
                    Services.LobbyService.UpdateLiveEditCharacterPosition();
                }
                if (Services.ClientState.LocalPlayer != null)
                {
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(buttonPosX);
                    if (ImGui.Button($"Apply current##{Title}##Position"))
                    {
                        preset.LocationModel.Position = Services.ClientState.LocalPlayer!.Position;
                    }
                    var pos = Services.ClientState.LocalPlayer!.Position;
                    GuiUtils.HoverTooltip($"Current Position: ({pos.X:F2}; {pos.Y:F2}; {pos.Z:F2})");
                }

                // Rotation
                if (!liveEditing)
                {
                    GuiUtils.AngleSlider($"Rotation##{Title}", ref preset.LocationModel.Rotation);
                }
                else if (GuiUtils.AngleDrag($"Rotation##LiveEdit##{Title}", ref preset.LocationModel.Rotation))
                {
                    UpdateLiveEdit();
                    Services.LobbyService.UpdateLiveEditCharacterRotation();
                }
                if (Services.ClientState.LocalPlayer != null)
                {
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(buttonPosX);
                    if (ImGui.Button($"Apply current##{Title}##Rotation"))
                    {
                        preset.LocationModel.Rotation = Services.ClientState.LocalPlayer!.Rotation;
                    }
                    GuiUtils.HoverTooltip($"Current rotation: {Services.ClientState.LocalPlayer!.Rotation / (float)Math.PI * 180.0f:F2}");
                }

                // Character MovementMode
                if (GuiUtils.Combo($"Character state##{Title}", ref preset.LocationModel.MovementMode))
                {
                    UpdateLiveEdit();
                    Services.LobbyService.UpdateLiveEditCharacterState();
                }
                if (Services.ClientState.LocalPlayer != null)
                {
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(buttonPosX); // should in theory Position it the same as it would normally
                    unsafe
                    {
                        var character = (CharacterExpanded*)Services.ClientState.LocalPlayer.Address;
                        if (ImGui.Button($"Apply current##{Title}##moveType"))
                        {
                            preset.LocationModel.MovementMode = character->MovementMode;
                        }
                        GuiUtils.HoverTooltip($"Current mode: {character->MovementMode}");
                    }
                }

                // Mount
                var mountId = mounts.ContainsKey(preset.LocationModel.Mount.MountId) ? preset.LocationModel.Mount.MountId : 0;
                var mountLabel = preset.LocationModel.Mount.LastLocationMount ? "Last used mount" : mounts[mountId];
                if (GuiUtils.FilterCombo($"Mount##{Title}", mountLabel, () =>
                {

                    if (GuiUtils.FilterSelectable($"Last used mount##{Title}", preset.LocationModel.Mount.LastLocationMount))
                    {
                        preset.LocationModel.Mount = new() { LastLocationMount = true };
                        return true;
                    }
                    GuiUtils.HoverTooltip("The mount that was summoned when the character logged off, can be unmounted");

                    foreach (var entry in mounts)
                    {
                        if (Services.MountService.Mounts.Contains(entry.Key))
                        {
                            if (GuiUtils.FilterSelectable($"{entry.Value}##{Title}##{entry.Key}", !preset.LocationModel.Mount.LastLocationMount && entry.Key == mountId))
                            {
                                preset.LocationModel.Mount = new()
                                {
                                    MountId = entry.Key
                                };
                                // TODO: fetch chocobo data if chocobo was selected
                                return true;
                            }
                        }
                    }
                    return false;
                }))
                {
                    UpdateLiveEdit();
                    Services.LobbyService.UpdateLiveEditCharacterMount();
                }
                ImGuiComponents.HelpMarker("You can only select mounts you have unlocked across all of your characters\nTry logging into each character if you think any are missing");
                if (Services.ClientState.LocalPlayer != null)
                {
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(buttonPosX);
                    unsafe
                    {
                        var character = (CharacterExpanded*)Services.ClientState.LocalPlayer.Address;
                        if (ImGui.Button($"Apply current##{Title}##Mount"))
                        {
                            Services.LocationService.SetMount(ref preset.LocationModel, &character->Character);
                        }
                        GuiUtils.HoverTooltip($"Current mount: {mounts[character->Character.Mount.MountId]}" + (character->Character.Mount.MountId == 1 ? "\nThis will also save your chocobo's gear and color" : ""));
                    }
                }

                //Setting overrides
                if (GuiUtils.Combo($"Camera follow mode override##{Title}", ref preset.CameraFollowMode))
                {
                    UpdateLiveEdit();

                }
            }
            else if (preset.LocationModel.LocationType == LocationType.TitleScreen)
            {
                //Position
                if (!liveEditing)
                {
                    ImGui.InputFloat3($"Camera Position##{Title}", ref preset.LocationModel.CameraPosition);
                }
                else if (ImGui.DragFloat3($"Camera Position##LiveEdit##{Title}", ref preset.LocationModel.CameraPosition, 0.1f))
                {
                    UpdateLiveEdit();
                }
                if (Services.ClientState.LocalPlayer != null)
                {
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(buttonPosX);
                    unsafe
                    {
                        if (ImGui.Button($"Apply current##{Title}##Camera Position"))
                        {
                            preset.LocationModel.CameraPosition = Services.CameraService.CurrentCamera->Camera.SceneCamera.Position;
                        }
                        var pos = Services.CameraService.CurrentCamera->Camera.SceneCamera.Position;
                        GuiUtils.HoverTooltip($"Current camera Position: ({pos.X:F2}; {pos.Y:F2}; {pos.Z:F2}) ");
                    }
                }

                if (!liveEditing)
                {
                    GuiUtils.AngleSlider($"Yaw##{Title}", ref preset.LocationModel.Yaw);
                    GuiUtils.AngleSlider($"Pitch##{Title}", ref preset.LocationModel.Pitch);
                    GuiUtils.AngleSlider($"Roll##{Title}", ref preset.LocationModel.Roll);
                }
                else
                {
                    if (GuiUtils.AngleDrag($"Yaw##{Title}", ref preset.LocationModel.Yaw))
                    {
                        UpdateLiveEdit();
                    }
                    if (GuiUtils.AngleDrag($"Pitch##{Title}", ref preset.LocationModel.Pitch))
                    {
                        UpdateLiveEdit();
                    }
                    if (GuiUtils.AngleDrag($"Roll##{Title}", ref preset.LocationModel.Roll))
                    {
                        UpdateLiveEdit();
                    }
                }
                if (Services.ClientState.LocalPlayer != null)
                {
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(buttonPosX);
                    unsafe
                    {
                        if (ImGui.Button($"Apply current##{Title}##Camera Orientation"))
                        {
                            preset.LocationModel.Yaw = Services.CameraService.CurrentCamera->Yaw;
                            preset.LocationModel.Pitch = Services.CameraService.CurrentCamera->Pitch;
                            preset.LocationModel.Roll = Services.CameraService.CurrentCamera->Roll;
                        }
                        var orientation = Services.CameraService.CurrentCamera->Orientation;
                        GuiUtils.HoverTooltip($"Current camera Orientation: ({orientation.X:F2}; {orientation.Y:F2}; {orientation.Z:F2}) ");
                    }
                }
                if (ImGui.SliderFloat($"FOV##{Title}", ref preset.LocationModel.Fov, 0.01f, 3f))
                {
                    UpdateLiveEdit();
                }
                if (GuiUtils.Combo($"Logo##{Title}", ref preset.LocationModel.TitleScreenLogo))
                {

                    UpdateLiveEdit();
                    if (liveEditing)
                    {
                        Services.LobbyService.ReloadTitleScreenUi();
                    }
                }
                if (GuiUtils.DrawUiColorPicker("Menu color", Title, ref preset.LocationModel.UiColor))
                {
                    UpdateLiveEdit();
                    if (liveEditing)
                    {
                        Services.LobbyService.RecolorTitleScreenUi();
                    }
                }

            }

            using (var color = ImRaii.PushColor(ImGuiCol.Text, GuiUtils.WarningColor))
            {
                if (ImGui.Checkbox($"Experimental: Save world layout##{Title}", ref preset.LocationModel.SaveLayout))
                {
                    UpdateLiveEdit();
                    LiveEditReloadScene();
                }
                color.Pop();

                ImGuiComponents.HelpMarker("Experimental feature to save world layout (e.g. changes that happen when you progress MSQ).\nWorks fairly well out in the world but often has issues in instances.");
                color.Push(ImGuiCol.Text, GuiUtils.WarningColor);
                if (preset.LocationModel.SaveLayout)
                {
                    if (ImGui.Checkbox($"Experimental: Save world layout vfx objects##{Title}", ref preset.LocationModel.UseVfx))
                    {
                        UpdateLiveEdit();
                        LiveEditReloadScene();
                    }
                    color.Pop();
                    ImGuiComponents.HelpMarker("The game often leaves \'dead\' vfx objects that will play on load that will play on load.\nUnchecking this option should help with this specific issue.");
                    color.Push(ImGuiCol.Text, GuiUtils.WarningColor);
                    if (preset.LocationModel.Active?.Count == 0)
                    {
                        ImGui.TextWrapped("There is no layout data collected for this preset, you need to go to the zone of this preset and click Apply Current next to the zone name");
                    }
                }
            }
            if (ImGui.Checkbox($"Save festivals##{Title}", ref preset.LocationModel.SaveFestivals))
            {
                UpdateLiveEdit();
                LiveEditReloadScene();
            }
            ImGuiComponents.HelpMarker("Festivals are event decorations");

            ImGui.TextUnformatted($"Preset file: {preset.FileName}");
            using (var color = ImRaii.PushColor(ImGuiCol.Text, GuiUtils.WarningColor))
            {
                if (liveEditing)
                {
                    ImGui.TextUnformatted($"Live editing mode enabled!");
                }
            }
            ImGui.EndDisabled();
        }

        public void DrawPresetActions()
        {
            ImGui.BeginDisabled(!makingNewPreset && currentPreset == "");
            if (ImGui.Button($"Save##{Title}"))
            {
                try
                {
                    if (!preset.LocationModel.SaveLayout)
                    {
                        preset.LocationModel.Active = [];
                        preset.LocationModel.Inactive = [];
                        preset.LocationModel.VfxTriggerIndexes = [];
                    }
                    currentPreset = Services.PresetService.Save(preset);
                    makingNewPreset = false;
                    preset = Services.PresetService.Presets[currentPreset];
                }
                catch (Exception ex)
                {
                    SetupError(ex);
                }
            }

            ImGui.SameLine();
            ImGui.BeginDisabled(((preset.LocationModel.LocationType == LocationType.TitleScreen && !Services.LobbyService.CanReloadTitleScreen) ||
                (preset.LocationModel.LocationType == LocationType.CharacterSelect && Services.LobbyService.CurrentLobbyMap != GameLobbyType.CharaSelect)));

            if (!liveEditing && ImGui.Button($"Live edit##{Title}"))
            {
                Services.LobbyService.StartLiveEdit(preset);
                liveEditing = true;
                liveEditingLocationType = preset.LocationModel.LocationType;
            }
            ImGui.EndDisabled();

            if (liveEditing && (
                    (preset.LocationModel.LocationType == LocationType.TitleScreen && !Services.LobbyService.CanReloadTitleScreen) ||
                    (preset.LocationModel.LocationType == LocationType.CharacterSelect && Services.LobbyService.CurrentLobbyMap != GameLobbyType.CharaSelect)
                ))
            {

                GuiUtils.HoverTooltip($"Only works in title or character select screen depending on the selected preset");
            }
            if (liveEditing && ImGui.Button($"Stop live edit##{Title}"))
            {
                StopLiveEdit();
            }
            ImGui.SameLine();
            if (makingNewPreset)
            {

                if (ImGui.Button($"Cancel##{Title}"))
                {
                    ClearPreset();
                }
            }
            else
            {

                if (ImGui.Button($"Delete##{Title}"))
                {
                    SetupDeleteConfirmation();
                }
            }

            ImGui.EndDisabled();
        }

        public void ExportPreset(bool confirmed, string path)
        {
            if (confirmed && currentPreset != null)
            {
                try
                {
                    Services.PresetService.Export(currentPreset, path);
                }
                catch (Exception ex)
                {
                    SetupError(ex);
                }

            }
        }
        public void ImportPreset(bool confirmed, List<string> path)
        {
            if (confirmed && currentPreset != null && path.Count == 1)
            {
                try
                {
                    SelectPreset(Services.PresetService.Import(path[0]));
                    SetupImportSuccess();
                }
                catch (Exception ex)
                {
                    SetupError(ex);
                }

            }
        }

        public void SelectPreset(string presetFileName)
        {
            if (Services.PresetService.Presets.TryGetValue(presetFileName, out var presetModel))
            {
                preset = presetModel;
                currentPreset = presetFileName;
                makingNewPreset = false;
                StopLiveEdit();
            }
        }

        public void ClearPreset()
        {
            preset = new();
            makingNewPreset = false;
            currentPreset = "";
            StopLiveEdit();
        }

        public void DuplicatePreset(string presetFileName)
        {
            if (Services.PresetService.Presets.TryGetValue(presetFileName, out var presetModel))
            {
                preset = presetModel;
                makingNewPreset = true;
                currentPreset = "";
                preset.FileName = "";
            }
            StopLiveEdit();
        }

        public void NewPreset()
        {
            makingNewPreset = true;
            currentPreset = "";
            preset = new();
            preset.Author = Services.ClientState.LocalPlayer!.Name.ToString();
            preset.LocationModel.LocationType = LocationType.TitleScreen;
            LoadCurrentTerritory();
            preset.LocationModel.WeatherId = Services.WeatherService.WeatherId;
            preset.LocationModel.TimeOffset = Services.LocationService.TimeOffset;
            preset.LocationModel.BgmId = Services.BgmService.CurrentSongId;
            preset.LocationModel.BgmPath = Services.DataManager.GetExcelSheet<BGM>()!.GetRow((uint)Services.BgmService.CurrentSongId)?.File.ToString();
            preset.LocationModel.Position = Services.ClientState.LocalPlayer!.Position;
            preset.LocationModel.Rotation = Services.ClientState.LocalPlayer!.Rotation;
            preset.LocationModel.Fov = 1;
            preset.LocationModel.TitleScreenLogo = TitleScreenLogo.Dawntrail;
            unsafe
            {
                var character = (CharacterExpanded*)Services.ClientState.LocalPlayer!.Address;
                preset.LocationModel.MovementMode = character->MovementMode;
                Services.LocationService.SetMount(ref preset.LocationModel, &character->Character);

                preset.LocationModel.CameraPosition = Services.CameraService.CurrentCamera->Camera.SceneCamera.Position;
                preset.LocationModel.Yaw = Services.CameraService.CurrentCamera->Yaw;
                preset.LocationModel.Pitch = Services.CameraService.CurrentCamera->Pitch;
                preset.LocationModel.Roll = Services.CameraService.CurrentCamera->Roll;
            }
            StopLiveEdit();
        }

        private void LoadCurrentTerritory()
        {
            preset.LocationModel.TerritoryPath = Services.LocationService.TerritoryPath!;
            preset.LocationModel.TerritoryTypeId = Services.ClientState.TerritoryType;
            Services.LocationService.SetLayout(ref preset.LocationModel);
            var weathers = Services.WeatherService.GetWeathers(preset.LocationModel.TerritoryPath);
            if (!weathers.Contains(preset.LocationModel.WeatherId))
            {
                preset.LocationModel.WeatherId = weathers.FirstOrDefault((byte)2);
            }
        }


        //yoinked from https://github.com/lmcintyre/TitleEditPlugin/blob/main/TitleEdit/TitleEditPlugin.cs
        private void DrawBgmTooltip(BgmInfo bgm)
        {
            GuiUtils.HoverTooltip(() =>
            {
                ImGui.PushTextWrapPos(GuiUtils.GuiScale(400f));
                ImGui.TextColored(new Vector4(0, 1, 0, 1), "Song Info");
                ImGui.TextWrapped($"Title: {bgm.Title}");
                ImGui.TextWrapped(string.IsNullOrEmpty(bgm.Location) ? "Location: Unknown" : $"Location: {bgm.Location}");
                if (!string.IsNullOrEmpty(bgm.AdditionalInfo))
                    ImGui.TextWrapped($"Info: {bgm.AdditionalInfo}");
                ImGui.TextUnformatted($"File path: {bgm.FilePath}");
                ImGui.PopTextWrapPos();
            });
        }

        private void SetupDeleteConfirmation() => SetupModal("Delete a preset", DrawDeleteConfirmation);

        private void DrawDeleteConfirmation()
        {
            ImGui.TextUnformatted("Are you sure you want to delete the preset, this is unrecoverable.");
            if (ImGui.Button("Yes"))
            {
                CloseModal();
                try
                {
                    Services.PresetService.Delete(currentPreset!);
                    ClearPreset();
                }
                catch (Exception ex)
                {
                    SetupError(ex);
                }
            }
            ImGui.SameLine();
            if (ImGui.Button("No")) CloseModal();
        }

        private void SetupImportSuccess() => SetupModal("Success", DrawImportSuccess);

        private void DrawImportSuccess()
        {
            ImGui.TextUnformatted($"Imported \"{preset.Name}\" successfully");
            if (ImGui.Button("Ok")) CloseModal();
        }

        private void StopLiveEdit()
        {
            if (liveEditing)
            {
                liveEditing = false;
                Services.LobbyService.StopLiveEdit(liveEditingLocationType);
            }
        }

        private void UpdateLiveEdit()
        {
            if (liveEditing)
            {
                Services.LobbyService.UpdateLiveEditPreset(preset);
            }
        }

        private void LiveEditReloadScene()
        {
            if (liveEditing)
            {
                if (liveEditingLocationType == LocationType.TitleScreen)
                {
                    Services.LobbyService.ReloadTitleScreen();
                }
                else if (liveEditingLocationType == LocationType.CharacterSelect)
                {
                    Services.LobbyService.ReloadCharacterSelect();
                }
            }
        }
    }

}

