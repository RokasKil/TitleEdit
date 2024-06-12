using CharacterSelectBackgroundPlugin.Data.Persistence;
using CharacterSelectBackgroundPlugin.Utility;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace CharacterSelectBackgroundPlugin.Windows.Tabs
{
    internal class DisplayTypeTab : ITab
    {
        public string Title => "Display";

        private ulong currentContentId = 0;

        public void Draw()
        {
            DrawSelectPresetCombo("Nothing selected setting", Services.ConfigurationService.NoCharacterDisplayType, (result) => Services.ConfigurationService.NoCharacterDisplayType = result, false);
            ImGuiComponents.HelpMarker("Preset that is used when no character is selected");
            DrawSelectPresetCombo("Global character setting", Services.ConfigurationService.GlobalDisplayType, (result) => Services.ConfigurationService.GlobalDisplayType = result);
            ImGui.Separator();
            ImGui.TextUnformatted("Character overrides");
            var usedIds = new HashSet<ulong>(Services.ConfigurationService.DisplayTypeOverrides.Count);
            if (Services.ConfigurationService.DisplayTypeOverrides.Count > 0)
            {
                ImGui.BeginTable($"Display Type Overrides##{Title}", 3);
                for (int i = 0; i < Services.ConfigurationService.DisplayTypeOverrides.Count; i++)
                {
                    var entry = Services.ConfigurationService.DisplayTypeOverrides[i];
                    usedIds.Add(entry.Key);
                    ImGui.TableNextColumn();
                    var label = Services.CharactersService.Characters.TryGetValue(entry.Key, out var charName) ? charName : entry.Key.ToString("X");
                    ImGui.TextUnformatted(label);
                    ImGui.TableNextColumn();

                    ImGui.SetNextItemWidth(16f * ImGui.GetFontSize());
                    DrawSelectPresetCombo($"##Character override##{i}", entry.Value, (result) => Services.ConfigurationService.DisplayTypeOverrides[i] = new(entry.Key, result));
                    ImGui.TableNextColumn();
                    if (ImGuiComponents.IconButton($"##{Title}##{i}##Delete", FontAwesomeIcon.Trash))
                    {
                        Services.ConfigurationService.DisplayTypeOverrides.RemoveRange(i, 1);
                        Services.ConfigurationService.Save();
                        i--;
                    }
                }
                ImGui.EndTable();

            }
            var characterLabel = Services.CharactersService.Characters.TryGetValue(currentContentId, out var characterName) ? characterName : "";
            GuiUtils.Combo("##Character override", characterLabel, () =>
            {
                bool empty = true;
                foreach (var entry in Services.CharactersService.Characters)
                {
                    if (usedIds.Contains(entry.Key)) continue;
                    empty = false;
                    if (ImGui.Selectable($"{entry.Value}##{Title}##Character override##{entry.Key}", entry.Key == currentContentId))
                    {
                        currentContentId = entry.Key;
                    }
                }
                if (empty)
                {
                    ImGui.TextUnformatted("All characters configured");
                }
            });
            ImGuiComponents.HelpMarker("These characters are collected from the character select screen.\nIf any are missing go through all the worlds with characters created.");
            ImGui.SameLine();
            ImGui.BeginDisabled(currentContentId == 0);
            if (ImGuiComponents.IconButton($"##{Title}##New", FontAwesomeIcon.Plus))
            {

                Services.ConfigurationService.DisplayTypeOverrides.Add(new(currentContentId, new() { Type = DisplayType.LastLocation }));
                Services.ConfigurationService.Save();

                currentContentId = 0;
            }
            ImGui.EndDisabled();
        }

        private void DrawSelectPresetCombo(string label, DisplayTypeOption value, Action<DisplayTypeOption> selectAction, bool showLastLocationOption = true)
        {
            string display;
            bool invalid = false;
            if (value.Type == DisplayType.LastLocation)
            {
                display = "Last location";
            }
            else if (value.Type == DisplayType.AetherialSea)
            {
                display = "Aetherial sea";
            }
            else
            {
                if (value.PresetPath != null && Services.PresetService.Presets.TryGetValue(value.PresetPath, out var preset))
                {
                    display = preset.Name;
                }
                else
                {
                    display = "Invalid preset";
                    invalid = true;
                }
            }
            if (invalid)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.8f, 0, 1));
            }
            bool stylePopped = false;
            GuiUtils.Combo(label, display, () =>
            {
                if (invalid)
                {
                    ImGui.PopStyleColor();
                    stylePopped = true;
                }

                if (showLastLocationOption)
                {
                    if (ImGui.Selectable($"Last location##{Title}##{label}##last location", value.Type == DisplayType.LastLocation))
                    {
                        selectAction.Invoke(new()
                        {
                            Type = DisplayType.LastLocation
                        });
                        Services.ConfigurationService.Save();
                    }
                    DrawDisplayTypeTooltip(DisplayType.LastLocation);

                }
                if (ImGui.Selectable($"Aetherial sea##{Title}##{label}##vanilla aetherial sea", value.Type == DisplayType.AetherialSea))
                {
                    selectAction.Invoke(new()
                    {
                        Type = DisplayType.AetherialSea
                    });
                    Services.ConfigurationService.Save();
                }
                DrawDisplayTypeTooltip(DisplayType.AetherialSea);
                ImGui.Separator();


                foreach (var entry in Services.PresetService.Presets)
                {
                    if (ImGui.Selectable($"{entry.Value.Name}##{Title}##{label}##{entry.Key}", value.Type == DisplayType.Preset && entry.Key == value.PresetPath))
                    {
                        selectAction.Invoke(new()
                        {
                            Type = DisplayType.Preset,
                            PresetPath = entry.Key
                        });
                        Services.ConfigurationService.Save();
                    }
                    DrawDisplayTypeTooltip(DisplayType.Preset, entry.Key);
                }
            });
            if (invalid && !stylePopped)
            {
                ImGui.PopStyleColor();
            }
            DrawDisplayTypeTooltip(value);
        }

        private void DrawDisplayTypeTooltip(DisplayTypeOption value) => DrawDisplayTypeTooltip(value.Type, value.PresetPath);

        private void DrawDisplayTypeTooltip(DisplayType type, string? path = null)
        {
            GuiUtils.HoverTooltip(() =>
            {
                ImGui.PushTextWrapPos(ImGui.GetFont().FontSize * 16);
                switch (type)
                {
                    case DisplayType.AetherialSea:
                        ImGui.TextWrapped("Regular unmodded character select scene");
                        break;
                    case DisplayType.LastLocation:
                        ImGui.TextWrapped("Last recorded character location, if nothing was recorded will default to the Nothing selected option");
                        break;
                    case DisplayType.Preset:
                        if (path != null && Services.PresetService.Presets.TryGetValue(path, out var preset))
                        {
                            var territory = Services.DataManager.GetExcelSheet<TerritoryType>()!.GetRow(preset.LocationModel.TerritoryTypeId);
                            if (territory != null)
                            {
                                ImGui.TextWrapped($"Zone: {territory.RowId} - {territory.PlaceNameRegion.Value?.Name} > {territory.PlaceName.Value?.Name}");
                            }
                            else
                            {
                                ImGui.TextWrapped($"Zone: Unknown");
                            }
                            ImGui.TextWrapped($"Author: {preset.Author.Replace("%", "%%")}");
                        }
                        else
                        {
                            ImGui.TextWrapped("Selected preset does not exist or failed to load");
                        }
                        break;
                }
                ImGui.PopTextWrapPos();
            });
        }
    }
}
