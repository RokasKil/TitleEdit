using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Numerics;
using TitleEdit.Data.Persistence;
using Action = System.Action;

namespace TitleEdit.Utility
{
    public static class GuiUtils
    {
        private static readonly Dictionary<string, bool> OpenCombos = [];
        private static readonly Dictionary<string, string> ComboFilters = [];

        private static readonly Dictionary<string, UiColorExpansion> UiColorPickerSelections = [];
        private static string? FilterComboTitle = null;
        private static bool FilterDrawSeperator = true;
        private static bool FilterDrawTooltip = false;

        public static void Combo(string title, string value, Action elementsAction, ImGuiComboFlags flags = ImGuiComboFlags.None, bool popStyleColor = false) =>
            Combo(title, value, (_) => elementsAction.Invoke(), flags);

        public static void Combo(string title, string value, Action<bool> elementsAction, ImGuiComboFlags flags = ImGuiComboFlags.None, bool popStyleColor = false)
        {
            if (ImGui.BeginCombo(title, value, flags))
            {
                if (popStyleColor)
                {
                    ImGui.PopStyleColor();
                }
                elementsAction.Invoke(!OpenCombos.GetValueOrDefault(title));
                OpenCombos[title] = true;
                ImGui.EndCombo();
            }
            else
            {
                if (popStyleColor)
                {
                    ImGui.PopStyleColor();
                }
                OpenCombos[title] = false;
            }
        }

        public static void FilterCombo(string title, string value, Func<bool> elementsAction, ImGuiComboFlags flags = ImGuiComboFlags.None, bool popStyleColor = false) =>
            FilterCombo(title, value, (_, _) => elementsAction.Invoke(), flags, popStyleColor);

        public static void FilterCombo(string title, string value, Func<bool, string, bool> elementsAction, ImGuiComboFlags flags = ImGuiComboFlags.None, bool popStyleColor = false)
        {
            Combo(title, value, (justOpened) =>
            {
                if (justOpened)
                {
                    ImGui.SetKeyboardFocusHere();
                    ComboFilters[title] = "";
                }
                string filter = ComboFilters[title];
                ImGui.InputText($"Search##{title}", ref filter, 256);
                ComboFilters[title] = filter;
                ImGui.Separator();
                if (ImGui.BeginChild($"{title}##Child", new Vector2(-1, ImGui.GetTextLineHeightWithSpacing() * 8)))
                {
                    FilterComboTitle = title;
                    FilterDrawSeperator = false;
                    if (elementsAction.Invoke(justOpened, filter))
                    {
                        ImGui.CloseCurrentPopup();
                    }
                    FilterComboTitle = null;
                    ImGui.EndChild();
                }
            }, flags | ImGuiComboFlags.HeightLargest, popStyleColor);
        }

        public static bool FilterSelectable(string label, bool selected)
        {
            FilterDrawTooltip = false;
            if (FilterComboTitle != null)
            {
                var visibleLabel = label.Contains("##") ? label[..label.IndexOf("##")] : label;
                if (!visibleLabel.Contains(ComboFilters[FilterComboTitle], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            FilterDrawSeperator = true;
            FilterDrawTooltip = true;
            return ImGui.Selectable(label, selected);
        }

        public static void FilterSeperator()
        {
            if (FilterDrawSeperator)
            {
                ImGui.Separator();
                FilterDrawSeperator = false;
            }
        }

        public static bool Combo<T>(string title, ref T value, ImGuiComboFlags flags = ImGuiComboFlags.None, Func<T, bool>? filter = null) where T : System.Enum
        {
            bool selected = false;

            using var combo = ImRaii.Combo(title, value.ToText(), flags);
            if (combo)
            {
                using var id = ImRaii.PushId(title); // I only discovered this exists after pretty much all of the UI is done, I should go and redo everything one day
                foreach (T enumValue in Enum.GetValues(typeof(T)))
                {
                    if (filter == null || filter.Invoke(enumValue))
                    {
                        if (ImGui.Selectable(enumValue.ToText(), value.Equals(enumValue)))
                        {
                            value = enumValue;
                            selected = true;
                        }
                    }
                }
                OpenCombos[title] = true;
            }
            else
            {
                OpenCombos[title] = false;
            }
            return selected;
        }

        public static bool AngleSlider(string title, ref float value)
        {
            var rotation = value / (float)Math.PI * 180.0f;
            if (ImGui.SliderFloat(title, ref rotation, -180, 180, "%.2f", ImGuiSliderFlags.AlwaysClamp))
            {
                value = rotation / 180.0f * (float)Math.PI;
                return true;
            }
            return false;
        }


        public static void HoverTooltip(string text, ImGuiHoveredFlags flags = ImGuiHoveredFlags.None, bool ignoreFilter = false) => HoverTooltip(() => ImGui.TextUnformatted(text), flags, ignoreFilter);

        public static void HoverTooltip(Action element, ImGuiHoveredFlags flags = ImGuiHoveredFlags.None, bool ignoreFilter = false)
        {
            if (!ignoreFilter && FilterComboTitle != null && !FilterDrawTooltip) return;
            if (ImGui.IsItemHovered(flags))
            {
                ImGui.BeginTooltip();
                element.Invoke();
                ImGui.EndTooltip();
            }
        }


        public static float GuiScale(float f)
        {
            return f * ImGui.GetIO().FontGlobalScale;
        }

        public static void DrawDisplayTypeTooltip(CharacterDisplayTypeOption value) => DrawDisplayTypeTooltip(value.Type, value.PresetPath);

        public static void DrawDisplayTypeTooltip(CharacterDisplayType type, string? path = null)
        {
            DrawDisplayTypeTooltip(() =>
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
                        DrawDisplayTypeTooltipGroupInfo(path, LocationType.CharacterSelect);
                        break;
                }
            });
        }

        public static void DrawDisplayTypeTooltip(TitleDisplayTypeOption value) => DrawDisplayTypeTooltip(value.Type, value.PresetPath);

        public static void DrawDisplayTypeTooltip(TitleDisplayType type, string? path = null)
        {
            DrawDisplayTypeTooltip(() =>
            {
                switch (type)
                {
                    case TitleDisplayType.Preset:
                        DrawDisplayTypeTooltipPresetInfo(path, LocationType.TitleScreen);
                        break;
                    case TitleDisplayType.Random:
                        DrawDisplayTypeTooltipGroupInfo(path, LocationType.TitleScreen);
                        break;
                }
            });
        }

        public static void DrawPresetTooltip(string? path, LocationType? type = null) => DrawDisplayTypeTooltip(() => DrawDisplayTypeTooltipPresetInfo(path, type));
        public static void DrawGroupTooltip(string? path, LocationType? type = null) => DrawDisplayTypeTooltip(() => DrawDisplayTypeTooltipGroupInfo(path, type));

        private static void DrawDisplayTypeTooltip(Action contentAction)
        {
            HoverTooltip(() =>
            {
                ImGui.PushTextWrapPos(ImGui.GetFont().FontSize * 16);
                contentAction.Invoke();
                ImGui.PopTextWrapPos();
            });
        }


        private static void DrawDisplayTypeTooltipPresetInfo(string? path = null, LocationType? type = null)
        {
            if (path != null && Services.PresetService.TryGetPreset(path, out var preset, type))
            {
                ImGui.TextWrapped($"{preset.LocationModel.LocationType.ToText()} preset");
                if (preset.Tooltip != null)
                {
                    ImGui.TextWrapped(preset.Tooltip.Replace("%", "%%"));
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
                    if (!string.IsNullOrEmpty(preset.Author))
                    {
                        ImGui.TextWrapped($"Author: {preset.Author.Replace("%", "%%")}");
                    }
                }
            }
            else
            {
                ImGui.TextWrapped("Selected preset does not exist or failed to load");
            }
        }

        private static void DrawDisplayTypeTooltipGroupInfo(string? path = null, LocationType? type = null)
        {
            if (path != null && Services.GroupService.TryGetGroup(path, out var group, type))
            {
                ImGui.TextWrapped($"{group.LocationType.ToText()} group");
                if (group.Tooltip != null)
                {
                    ImGui.TextWrapped(group.Tooltip.Replace("%", "%%"));
                }
                else
                {
                    ImGui.TextWrapped($"Contains {group.PresetFileNames.Count} preset{(group.PresetFileNames.Count > 1 ? "s" : "")}");
                }
            }
            else
            {
                ImGui.TextWrapped("Selected group does not exist or failed to load");
            }
        }

        public static bool DrawUiColorPicker(string title, string id, ref UiColorModel colorModel, bool allowUnspecified = false)
        {
            bool changed = false;
            if (Combo($"{title}##{id}", ref colorModel.Expansion, filter: (entry) => allowUnspecified || entry != UiColorExpansion.Unspecified))
            {
                changed = true;
            }
            if (colorModel.Expansion == UiColorExpansion.Custom)
            {
                if (ImGui.ColorEdit4($"Text color##{id}", ref colorModel.Color, ImGuiColorEditFlags.NoInputs))
                {
                    changed = true;
                }
                if (ImGui.ColorEdit4($"Edge color##{id}", ref colorModel.EdgeColor, ImGuiColorEditFlags.NoInputs))
                {
                    changed = true;
                }
                if (ImGui.ColorEdit4($"Button highlight color##{id}", ref colorModel.HighlightColor, ImGuiColorEditFlags.NoInputs))
                {
                    changed = true;
                }
                ImGuiComponents.HelpMarker("The button highlight color will not be accurate, because the highlight image itself is blue, I tried to work around that by offsetting the color but it's not perfect");
                var selection = UiColorPickerSelections.GetValueOrDefault(id, UiColorExpansion.Dawntrail);
                if (Combo($"##{id}##selectExpansionToApply", ref selection, filter: (entry) => entry != UiColorExpansion.Unspecified && entry != UiColorExpansion.Custom))
                {
                    UiColorPickerSelections[id] = selection;
                }
                ImGui.SameLine();
                if (ImGui.Button($"Apply expansion colors##{id}"))
                {
                    colorModel = UiColors.GetColorModelByExpansion(selection);
                    colorModel.Expansion = UiColorExpansion.Custom;
                    changed = true;
                }

            }
            return changed;
        }
    }
}
