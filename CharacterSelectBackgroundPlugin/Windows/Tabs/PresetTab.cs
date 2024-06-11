using CharacterSelectBackgroundPlugin.Data.BGM;
using CharacterSelectBackgroundPlugin.Data.Character;
using CharacterSelectBackgroundPlugin.Data.Lobby;
using CharacterSelectBackgroundPlugin.Data.Persistence;
using CharacterSelectBackgroundPlugin.Utility;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiFileDialog;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Action = System.Action;

namespace CharacterSelectBackgroundPlugin.Windows.Tabs
{
    internal class PresetTab : ITab
    {
        public string Title => "Presets";

        private PresetModel preset = new();
        private string currentPreset = "";
        private bool makingNewPreset = false;

        private readonly Dictionary<uint, string> mounts;
        private readonly Dictionary<byte, string> weathers;

        private string mountSearchValue = "";
        private string bgmSearchValue = "";

        private FileDialogManager fileDialogManager = new();

        private bool modal;
        private string modalTitle = "";
        private Action? modalContent = null;

        private Exception? lastException = null;
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

        public void Draw()
        {
            fileDialogManager.Draw();
            if (modal)
            {
                ImGui.OpenPopup($"{modalTitle}##{Title}");
                if (ImGui.BeginPopupModal($"{modalTitle}##{Title}", ref modal, ImGuiWindowFlags.NoNav | ImGuiWindowFlags.AlwaysAutoResize))
                {
                    modalContent?.Invoke();
                    ImGui.EndPopup();
                }
            }
            DrawPresetListControls();
            ImGui.Separator();
            DrawPresetControls();
            DrawPresetActions();
        }

        private void DrawPresetListControls()
        {
            var currentPresetName = makingNewPreset ? "Unsaved preset" : Services.PresetService.Presets.GetValueOrDefault(currentPreset).Name;
            if (makingNewPreset)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.8f, 0, 1));
            }
            bool stylePopped = false;
            GuiUtils.Combo($"##{Title}##presetCombo", currentPresetName, () =>
            {
                if (makingNewPreset)
                {
                    ImGui.PopStyleColor();
                    stylePopped = true;
                }
                foreach (var entry in Services.PresetService.Presets)
                {
                    if (ImGui.Selectable($"{entry.Value.Name}##{Title}##{entry.Key}", entry.Key == currentPreset))
                    {
                        SelectPreset(entry.Key);
                    }
                }
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
                        }
                    }

                }
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
            var time = (int)preset.LocationModel.TimeOffset;
            if (ImGui.SliderInt($"Time##{Title}", ref time, 0, 2399, $"{(int)time / 100:00}:{(int)(time % 100 / 100f * 60):00}", ImGuiSliderFlags.AlwaysClamp | ImGuiSliderFlags.NoInput))
            {
                preset.LocationModel.TimeOffset = (ushort)time;
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
                GuiUtils.HoverTooltip($"Current time: {currentTime / 100:00}:{(int)(currentTime % 100 / 100f * 60):00}");
            }

            // Song
            var bgmId = Services.BgmService.Bgms.ContainsKey(preset.LocationModel.BgmId) ? preset.LocationModel.BgmId : 0;
            GuiUtils.Combo($"Song##{Title}", Services.BgmService.Bgms[bgmId].DisplayName, (justOpened) =>
            {
                if (justOpened)
                {
                    ImGui.SetKeyboardFocusHere();
                    bgmSearchValue = "";
                }
                ImGui.InputText($"Search##{Title}##BGM", ref bgmSearchValue, 256);
                ImGui.Separator();
                if (ImGui.BeginChild($"Song##{Title}##Child", new Vector2(-1, 200), false, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    foreach (var entry in Services.BgmService.Bgms)
                    {
                        if (entry.Value.DisplayName.Contains(bgmSearchValue, StringComparison.OrdinalIgnoreCase))
                        {
                            if (ImGui.Selectable($"{entry.Value.DisplayName}##{Title}##{entry.Key}", entry.Key == bgmId))
                            {
                                preset.LocationModel.BgmId = entry.Key;
                                preset.LocationModel.BgmPath = entry.Value.FilePath;
                                ImGui.CloseCurrentPopup();
                            }
                            DrawBgmTooltip(entry.Value);
                        }
                    }
                    ImGui.EndChild();
                }
            }, ImGuiComboFlags.HeightLargest);
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
            //Position
            ImGui.InputFloat3("Position", ref preset.LocationModel.Position);
            if (Services.ClientState.LocalPlayer != null)
            {
                ImGui.SameLine();
                ImGui.SetCursorPosX(buttonPosX);
                if (ImGui.Button($"Apply current##{Title}##Position"))
                {
                    preset.LocationModel.Position = Services.ClientState.LocalPlayer!.Position;
                }
                var pos = Services.ClientState.LocalPlayer!.Position;
                ImGui.PushFont(UiBuilder.MonoFont);
                GuiUtils.HoverTooltip($"Current Position: ({pos.X:F2}; {pos.Y:F2}; {pos.Z:F2}) ");
                ImGui.PopFont();
            }

            // Rotation
            var rotation = preset.LocationModel.Rotation / (float)Math.PI * 180.0f;
            if (ImGui.SliderFloat("Rotation", ref rotation, -180, 180, "%.2f", ImGuiSliderFlags.AlwaysClamp))
            {

                preset.LocationModel.Rotation = rotation / 180.0f * (float)Math.PI;
            }
            ImGuiComponents.HelpMarker("CTRL+click to enter Value manually");
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
            GuiUtils.Combo($"Character state##{Title}", preset.LocationModel.MovementMode.ToString(), () =>
            {
                foreach (var mode in Enum.GetValues<MovementMode>())
                {
                    if (ImGui.Selectable($"{mode}##{Title}", preset.LocationModel.MovementMode == mode))
                    {
                        preset.LocationModel.MovementMode = mode;
                    }
                }
            });
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
            var mountLabel = preset.LastLocationMount ? "Last used mount" : mounts[mountId];
            GuiUtils.Combo($"Mount##{Title}", mountLabel, (justOpened) =>
            {
                if (justOpened)
                {
                    ImGui.SetKeyboardFocusHere();
                    mountSearchValue = "";
                }
                ImGui.InputText($"Search##{Title}##Mount", ref mountSearchValue, 256);
                ImGui.Separator();
                if (ImGui.BeginChild($"Mount##{Title}##Child", new Vector2(-1, 200)))
                {
                    if ("Last used mount".Contains(mountSearchValue, StringComparison.OrdinalIgnoreCase))
                    {
                        if (ImGui.Selectable($"Last used mount##{Title}", preset.LastLocationMount))
                        {
                            preset.LocationModel.Mount = new();
                            preset.LastLocationMount = true;
                            ImGui.CloseCurrentPopup();
                        }
                        GuiUtils.HoverTooltip("The mount that was summoned when the chracter logged off, can be unmounted");
                    }

                    foreach (var entry in mounts)
                    {
                        if (Services.MountService.Mounts.Contains(entry.Key) && entry.Value.Contains(mountSearchValue, StringComparison.OrdinalIgnoreCase))
                        {
                            if (ImGui.Selectable($"{entry.Value}##{Title}##{entry.Key}", !preset.LastLocationMount && entry.Key == mountId))
                            {
                                preset.LocationModel.Mount = new()
                                {
                                    MountId = entry.Key
                                };
                                // TODO: fetch chocobo data if chocobo was selected
                                preset.LastLocationMount = false;
                                ImGui.CloseCurrentPopup();
                            }
                        }
                    }
                    ImGui.EndChild();
                }
            }, ImGuiComboFlags.HeightLargest);
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
            GuiUtils.Combo($"Camera follow mode override##{Title}", preset.CameraFollowMode.ToString(), () =>
            {
                foreach (var mode in Enum.GetValues<CameraFollowMode>())
                {
                    if (ImGui.Selectable($"{mode}##{Title}", preset.CameraFollowMode == mode))
                    {
                        preset.CameraFollowMode = mode;
                    }
                }
            });
            ImGui.TextUnformatted($"Preset file: {preset.FileName}");
        }

        public void DrawPresetActions()
        {
            if (ImGui.Button($"Save##{Title}"))
            {
                try
                {
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
            ImGui.BeginDisabled(Services.LobbyService.CurrentLobbyMap != GameLobbyType.CharaSelect);

            if (ImGui.Button($"Apply##{Title}"))
            {
                Services.LobbyService.Apply(preset);
            }
            ImGui.EndDisabled();
            if (Services.LobbyService.CurrentLobbyMap != GameLobbyType.CharaSelect)
            {
                GuiUtils.HoverTooltip("Only works in character select", ImGuiHoveredFlags.AllowWhenDisabled);
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
            }
        }

        public void ClearPreset()
        {
            preset = new();
            makingNewPreset = false;
            currentPreset = "";
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
        }

        public void NewPreset()
        {
            makingNewPreset = true;
            currentPreset = "";
            preset = new();
            preset.Author = Services.ClientState.LocalPlayer!.Name.ToString();
            LoadCurrentTerritory();
            preset.LocationModel.WeatherId = Services.WeatherService.WeatherId;
            preset.LocationModel.TimeOffset = Services.LocationService.TimeOffset;
            preset.LocationModel.BgmId = Services.BgmService.CurrentSongId;
            preset.LocationModel.BgmPath = Services.DataManager.GetExcelSheet<BGM>()!.GetRow((uint)Services.BgmService.CurrentSongId)?.File.ToString();
            preset.LocationModel.Position = Services.ClientState.LocalPlayer!.Position;
            preset.LocationModel.Rotation = Services.ClientState.LocalPlayer!.Rotation;
            unsafe
            {
                var character = (CharacterExpanded*)Services.ClientState.LocalPlayer!.Address;
                preset.LocationModel.MovementMode = character->MovementMode;
                Services.LocationService.SetMount(ref preset.LocationModel, &character->Character);
            }
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

        private void SetupDeleteConfirmation()
        {
            modal = true;
            modalTitle = "Delete a preset";
            modalContent = DrawDeleteConfirmation;
        }

        private void DrawDeleteConfirmation()
        {
            ImGui.TextUnformatted("Are you sure you want to delete the preset, this is unrecoverable.");
            if (ImGui.Button("Yes"))
            {
                modal = false;
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
            if (ImGui.Button("No")) modal = false;
        }

        private void SetupImportSuccess()
        {
            modal = true;
            modalTitle = "Success";
            modalContent = DrawImportSuccess;
        }

        private void DrawImportSuccess()
        {
            ImGui.TextUnformatted($"Imported \"{preset.Name}\" successfully");
            if (ImGui.Button("Ok")) modal = false;
        }

        private void SetupError(Exception ex)
        {
            lastException = ex;
            modal = true;
            modalTitle = "Error";
            modalContent = DrawError;
        }

        private void DrawError()
        {
            ImGui.TextUnformatted("Error encountered: ");
            ImGui.TextUnformatted($"{lastException?.Message}");
            if (ImGui.Button("Ok")) modal = false;
        }

    }

}

