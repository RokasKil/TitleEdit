using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System;
using TitleEdit.Utility;

namespace TitleEdit.Windows.Tabs
{
    internal class SettingsTab : AbstractTab
    {
        public override string Title => "Settings";

        public override void Draw()
        {
            base.Draw();
            if (SettingCheckbox($"Track player location##{Title}", ref Services.ConfigurationService.TrackPlayerLocation))
            {
                Services.LayoutService.SettingsUpdated();
            }

            ImGuiComponents.HelpMarker("Continously track player location to show your character at the location you logged off");
            using (ImRaii.Disabled(!Services.ConfigurationService.TrackPlayerLocation))
            {
                SettingCheckbox($"Save periodically##{Title}", ref Services.ConfigurationService.PeriodicSaving);
                using (ImRaii.Enabled()) ImGuiComponents.HelpMarker("Periodically save incase of a crash or process termination");

                ImGui.SetNextItemWidth(6f * ImGui.GetFontSize());
                var savePeriod = Services.ConfigurationService.SavePeriod;
                if (ImGui.InputInt($"Save period in seconds##{Title}", ref savePeriod))
                {
                    Services.ConfigurationService.SavePeriod = Math.Max(1, savePeriod);
                    Services.ConfigurationService.Save();
                }

                using var color = ImRaii.PushColor(ImGuiCol.Text, GuiUtils.WarningColor);
                if (SettingCheckbox($"Experimental: Save layout##{Title}", ref Services.ConfigurationService.SaveLayout))
                {
                    Services.LayoutService.SettingsUpdated();
                    Services.LocationService.LayoutSettingsUpdated();
                }

                color.Pop();
                using (ImRaii.Enabled()) ImGuiComponents.HelpMarker("Experimental feature to save world layout (e.g. changes that happen when you progress MSQ).\nWorks fairly well out in the world but often has issues in instances.");
                color.Push(ImGuiCol.Text, GuiUtils.WarningColor);
                if (SettingCheckbox($"Experimental: Save layout in instance##{Title}", ref Services.ConfigurationService.SaveLayoutInInstance))
                {
                    Services.LayoutService.SettingsUpdated();
                    Services.LocationService.LayoutSettingsUpdated();
                }

                color.Pop();
                using (ImRaii.Enabled()) ImGuiComponents.HelpMarker("Rescanning the layout after something changes may potentially cause stuttering on low-end systems.\nYou can switch that functionality off while specifically in instance");
                SettingCheckbox($"Save mount##{Title}", ref Services.ConfigurationService.SaveMount);
                SettingCheckbox($"Save song##{Title}", ref Services.ConfigurationService.SaveBgm);
                SettingCheckbox($"Save Eorzea time##{Title}", ref Services.ConfigurationService.SaveTime);
                ImGuiComponents.HelpMarker("Check to remember what in-game time your location was recorded, will use in-game time if unchecked");
            }

            ImGui.Separator();
            SettingCheckbox($"Don't restart the BGM when switching between scenes where the same BGM is used##{Title}", ref Services.ConfigurationService.DontInterruptMusicOnSceneSwitch);
            ImGui.Separator();
            SettingCheckbox($"Add a button to character select screen that opens this window##{Title}", ref Services.ConfigurationService.DrawCharacterSelectButton);
            SettingCheckbox($"Display the name of the current screen when loaded##{Title}", ref Services.ConfigurationService.DisplayTitleToast);
            if (SettingCheckbox($"Hide character names in character select##{Title}", ref Services.ConfigurationService.HideCharacterSelectNames))
            {
                Services.LobbyService.HideCharacterSelectNamesSettingUpdated();
            }

            ImGuiComponents.HelpMarker("To hide your name when sharing character select presets");
            ImGui.Separator();
            SettingCheckbox($"Use the logged in character's name as default preset author name##{Title}", ref Services.ConfigurationService.UseCharacterNameAsAuthor);
            using (ImRaii.Disabled(Services.ConfigurationService.UseCharacterNameAsAuthor))
                if (ImGui.InputText($"Default author name##{Title}", ref Services.ConfigurationService.DefaultAuthorName, 256))
                    Services.ConfigurationService.Save();
        }

        private bool SettingCheckbox(string label, ref bool value, bool save = true)
        {
            if (ImGui.Checkbox(label, ref value))
            {
                if (save) Services.ConfigurationService.Save();
                return true;
            }

            return false;
        }
    }
}
