using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace TitleEdit.Utility
{
    public static class GuiUtils
    {
        private static readonly Dictionary<string, bool> OpenCombos = [];
        private static readonly Dictionary<string, string> ComboFilters = [];
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

        public static void Combo<T>(string title, ref T value, ImGuiComboFlags flags = ImGuiComboFlags.None, Func<T, bool>? filter = null) where T : System.Enum
        {
            if (ImGui.BeginCombo(title, value.ToText(), flags))
            {
                foreach (T enumValue in Enum.GetValues(typeof(T)))
                {
                    if (filter == null || filter.Invoke(enumValue))
                    {
                        if (ImGui.Selectable($"{enumValue.ToText()}##{title}", value.Equals(enumValue)))
                        {
                            value = enumValue;
                        }
                    }
                }
                OpenCombos[title] = true;
                ImGui.EndCombo();
            }
            else
            {
                OpenCombos[title] = false;
            }
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
    }
}
