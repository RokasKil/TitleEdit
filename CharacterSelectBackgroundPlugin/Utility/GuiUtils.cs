using ImGuiNET;
using System;
using System.Collections.Generic;

namespace CharacterSelectBackgroundPlugin.Utility
{
    public static class GuiUtils
    {
        private static readonly Dictionary<string, bool> OpenCombos = [];

        public static void Combo(string title, string value, Action elementsAction, ImGuiComboFlags flags = ImGuiComboFlags.None) =>
            Combo(title, value, (_) => elementsAction.Invoke(), flags);

        public static void Combo(string title, string value, Action<bool> elementsAction, ImGuiComboFlags flags = ImGuiComboFlags.None)
        {
            if (ImGui.BeginCombo(title, value, flags))
            {
                elementsAction.Invoke(!OpenCombos.GetValueOrDefault(title));
                OpenCombos[title] = true;
                ImGui.EndCombo();
            }
            else
            {
                OpenCombos[title] = false;
            }
        }

        public static void HoverTooltip(string text, ImGuiHoveredFlags flags = ImGuiHoveredFlags.None) => HoverTooltip(() => ImGui.TextUnformatted(text), flags);

        public static void HoverTooltip(Action element, ImGuiHoveredFlags flags = ImGuiHoveredFlags.None)
        {
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
