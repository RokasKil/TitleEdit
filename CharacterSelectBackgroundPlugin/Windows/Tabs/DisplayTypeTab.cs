using CharacterSelectBackgroundPlugin.Data.Persistence;
using CharacterSelectBackgroundPlugin.Utility;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using ImGuiNET;
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
            DrawSelectPresetCombo("Global setting", Services.ConfigurationService.GlobalDisplayType, (result) => Services.ConfigurationService.GlobalDisplayType = result);
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

        private void DrawSelectPresetCombo(string label, DisplayTypeOption value, Action<DisplayTypeOption> selectAction)
        {
            string display;
            bool invalid = false;
            if (value.Type == DisplayType.LastLocation)
            {
                display = "Last location";
            }
            else if (value.Type == DisplayType.AetherialSea)
            {
                display = "Vanilla aetherial sea";
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
                if (ImGui.Selectable($"Last location##{Title}##{label}##last location", value.Type == DisplayType.LastLocation))
                {
                    selectAction.Invoke(new()
                    {
                        Type = DisplayType.LastLocation
                    });
                    Services.ConfigurationService.Save();
                }
                if (ImGui.Selectable($"Vanilla aetherial sea##{Title}##{label}##vanilla aetherial sea", value.Type == DisplayType.AetherialSea))
                {
                    selectAction.Invoke(new()
                    {
                        Type = DisplayType.AetherialSea
                    });
                    Services.ConfigurationService.Save();
                }
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
                }
            });
            if (invalid && !stylePopped)
            {
                ImGui.PopStyleColor();
            }
            if (invalid)
            {
                GuiUtils.HoverTooltip("Saved preset does not exist");
            }
        }
    }
}
