using ImGuiNET;

namespace CharacterSelectBackgroundPlugin.Windows.Tabs
{
    internal class PresetTab : ITab
    {
        public string Title => "Presets";

        public unsafe void Draw()
        {
            if (ImGui.BeginCombo($"Preset##{Title}", "idk"))
            {
                ImGui.EndCombo();
            }
            if (ImGui.Button($"Export to clipboard##{Title}"))
            {

            }
            ImGui.SameLine();
            if (ImGui.Button($"Export to file##{Title}"))
            {

            }
            if (ImGui.Button($"Import from clipboard##{Title}"))
            {

            }
            ImGui.SameLine();
            if (ImGui.Button($"Import from file##{Title}"))
            {

            }
            ImGui.Separator();

        }
    }
}
