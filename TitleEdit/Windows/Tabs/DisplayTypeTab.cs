using Dalamud.Interface;
using Dalamud.Interface.Components;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Numerics;
using TitleEdit.Data.Persistence;
using TitleEdit.Utility;

namespace TitleEdit.Windows.Tabs
{
    internal class DisplayTypeTab : ITab
    {
        public string Title => "Display";

        private ulong currentContentId = 0;

        public void Draw()
        {
            ImGui.TextUnformatted("Title screen");
            DrawSelectPresetCombo("Title screen setting", Services.ConfigurationService.TitleDisplayTypeOption, (result) => Services.ConfigurationService.TitleDisplayTypeOption = result);
            GuiUtils.Combo("Default title screen logo", ref Services.ConfigurationService.TitleScreenLogo, filter: (entry) => entry != TitleScreenLogo.Unspecified);
            ImGui.Checkbox("Override preset title screen logo setting", ref Services.ConfigurationService.OverridePresetTitleScreenLogo);

            ImGui.Separator();
            ImGui.TextUnformatted("Character select screen");
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
            GuiUtils.FilterCombo("##Character override", characterLabel, () =>
            {
                bool empty = true;
                foreach (var entry in Services.CharactersService.Characters)
                {
                    if (usedIds.Contains(entry.Key)) continue;
                    empty = false;
                    if (GuiUtils.FilterSelectable($"{entry.Value}##{Title}##Character override##{entry.Key}", entry.Key == currentContentId))
                    {
                        currentContentId = entry.Key;
                        return true;
                    }
                }
                if (empty)
                {
                    ImGui.TextUnformatted("All characters configured");
                }
                return false;
            });
            ImGuiComponents.HelpMarker("These characters are collected from the character select screen.\nIf any are missing go through all the worlds with characters created.");
            ImGui.SameLine();
            ImGui.BeginDisabled(currentContentId == 0);
            if (ImGuiComponents.IconButton($"##{Title}##New", FontAwesomeIcon.Plus))
            {

                Services.ConfigurationService.DisplayTypeOverrides.Add(new(currentContentId, new() { Type = CharacterDisplayType.LastLocation }));
                Services.ConfigurationService.Save();

                currentContentId = 0;
            }
            ImGui.EndDisabled();
        }

        private void DrawSelectPresetCombo(string label, CharacterDisplayTypeOption value, Action<CharacterDisplayTypeOption> selectAction, bool showLastLocationOption = true)
        {
            string display = "???";
            bool invalid = false;
            if (value.Type == CharacterDisplayType.LastLocation)
            {
                display = "Last location";
            }
            else if (value.Type == CharacterDisplayType.Preset)
            {
                if (value.PresetPath != null && Services.PresetService.TryGetPreset(value.PresetPath, out var preset, LocationType.CharacterSelect))
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
            GuiUtils.FilterCombo(label, display, () =>
            {
                if (showLastLocationOption)
                {
                    if (GuiUtils.FilterSelectable($"Last location##{Title}##{label}##last location", value.Type == CharacterDisplayType.LastLocation))
                    {
                        selectAction.Invoke(new()
                        {
                            Type = CharacterDisplayType.LastLocation
                        });
                        Services.ConfigurationService.Save();
                        return true;
                    }
                    DrawDisplayTypeTooltip(CharacterDisplayType.LastLocation);

                }
                GuiUtils.FilterSeperator();


                foreach (var entry in Services.PresetService.CharacterSelectPresetEnumerator)
                {
                    if (GuiUtils.FilterSelectable($"{entry.Value.Name}##{Title}##{label}##{entry.Key}", value.Type == CharacterDisplayType.Preset && entry.Key == value.PresetPath))
                    {
                        selectAction.Invoke(new()
                        {
                            Type = CharacterDisplayType.Preset,
                            PresetPath = entry.Key
                        });
                        Services.ConfigurationService.Save();
                        return true;
                    }
                    DrawDisplayTypeTooltip(CharacterDisplayType.Preset, entry.Key);
                }
                return false;
            }, popStyleColor: invalid);
            DrawDisplayTypeTooltip(value);
        }

        private void DrawSelectPresetCombo(string label, TitleDisplayTypeOption value, Action<TitleDisplayTypeOption> selectAction)
        {
            string display;
            bool invalid = false;
            if (value.PresetPath != null && Services.PresetService.TryGetPreset(value.PresetPath, out var preset, LocationType.TitleScreen))
            {
                display = preset.Name;
            }
            else
            {
                display = "Invalid preset";
                invalid = true;
            }

            if (invalid)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.8f, 0, 1));
            }
            GuiUtils.FilterCombo(label, display, () =>
            {
                foreach (var entry in Services.PresetService.TitleScreenPresetEnumerator)
                {
                    if (GuiUtils.FilterSelectable($"{entry.Value.Name}##{Title}##{label}##{entry.Key}", value.Type == TitleDisplayType.Preset && entry.Key == value.PresetPath))
                    {
                        selectAction.Invoke(new()
                        {
                            Type = TitleDisplayType.Preset,
                            PresetPath = entry.Key
                        });
                        Services.ConfigurationService.Save();
                        return true;
                    }
                    DrawDisplayTypeTooltip(TitleDisplayType.Preset, entry.Key);

                }
                return false;
            }, popStyleColor: invalid);
            DrawDisplayTypeTooltip(value);
        }

        private void DrawDisplayTypeTooltip(CharacterDisplayTypeOption value) => DrawDisplayTypeTooltip(value.Type, value.PresetPath);

        private void DrawDisplayTypeTooltip(CharacterDisplayType type, string? path = null)
        {
            DrawTooltip(() =>
            {
                switch (type)
                {
                    case CharacterDisplayType.LastLocation:
                        ImGui.TextWrapped("Last recorded character location, if nothing was recorded will default to the Nothing selected option");
                        break;
                    case CharacterDisplayType.Preset:
                        DrawDisplayTypeTooltipPresetInfo(path, LocationType.CharacterSelect);
                        break;
                    case CharacterDisplayType.Random:
                        ImGui.TextWrapped("Random");
                        break;
                }
            });
        }

        private void DrawDisplayTypeTooltip(TitleDisplayTypeOption value) => DrawDisplayTypeTooltip(value.Type, value.PresetPath);

        private void DrawDisplayTypeTooltip(TitleDisplayType type, string? path = null)
        {
            DrawTooltip(() =>
            {
                switch (type)
                {
                    case TitleDisplayType.Preset:
                        DrawDisplayTypeTooltipPresetInfo(path, LocationType.TitleScreen);
                        break;
                    case TitleDisplayType.Random:
                        ImGui.TextWrapped("Random");
                        break;
                }
            });
        }

        private void DrawTooltip(System.Action contentAction)
        {
            GuiUtils.HoverTooltip(() =>
            {
                ImGui.PushTextWrapPos(ImGui.GetFont().FontSize * 16);
                contentAction.Invoke();
                ImGui.PopTextWrapPos();
            });
        }


        private void DrawDisplayTypeTooltipPresetInfo(string? path = null, LocationType? type = null)
        {
            if (path != null && Services.PresetService.TryGetPreset(path, out var preset, type))
            {
                if (preset.Tooltip != null)
                {
                    ImGui.TextWrapped(preset.Tooltip);
                }
                else
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
            }
            else
            {
                ImGui.TextWrapped("Selected preset does not exist or failed to load");
            }
        }
    }
}
