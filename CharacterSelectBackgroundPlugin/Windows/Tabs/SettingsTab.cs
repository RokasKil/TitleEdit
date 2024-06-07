using CharacterSelectBackgroundPlugin.Utility;
using Dalamud.Interface.Components;
using ImGuiNET;
using System;

namespace CharacterSelectBackgroundPlugin.Windows.Tabs
{
    internal class SettingsTab : ITab
    {
        public string Title => "Tracking settings";

        public void Draw()
        {
            SettingCheckbox($"Track player location##{Title}", ref Services.ConfigurationService.TrackPlayerLocation);
            ImGuiComponents.HelpMarker("Continously track player location to show your character at the location you logged off");
            SettingCheckbox($"Save periodically##{Title}", ref Services.ConfigurationService.PeriodicSaving);
            ImGuiComponents.HelpMarker("Periodically save incase of a crash or process termination");

            ImGui.SetNextItemWidth(6f * ImGui.GetFontSize());
            var savePeriod = Services.ConfigurationService.SavePeriod;
            if (ImGui.InputInt($"Save period in seconds##{Title}", ref savePeriod))
            {
                Services.ConfigurationService.SavePeriod = Math.Max(1, savePeriod);
                Services.ConfigurationService.Save();
            }
            SettingCheckbox($"Save layout##{Title}", ref Services.ConfigurationService.SaveLayout);
            ImGuiComponents.HelpMarker("Information about conditional world assets e.g. objects that change as you progress through MSQ");
            SettingCheckbox($"Save layout in instance##{Title}", ref Services.ConfigurationService.SaveLayoutInInstance);
            ImGuiComponents.HelpMarker("Rescanning the layout after something changes may potentially cause stuttering on low-end systems\nYou can switch that functionality off while specifically in instance");
            SettingCheckbox($"Save mount##{Title}", ref Services.ConfigurationService.SaveMount);
            SettingCheckbox($"Save song##{Title}", ref Services.ConfigurationService.SaveBgm);
            SettingCheckbox($"Save Eorzea time##{Title}", ref Services.ConfigurationService.SaveTime);
        }

        private void SettingCheckbox(string label, ref bool value, bool save = true)
        {
            if (ImGui.Checkbox(label, ref value))
            {
                if (save) Services.ConfigurationService.Save();
            }
        }
    }
}