using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using TitleEdit.Utility;

namespace TitleEdit.Windows.Tabs
{
    internal class ExtrasTab : AbstractTab
    {
        public override string Title => "Extras";

        public override void Draw()
        {
            base.Draw();
            using var textWrapPos = ImRaii.TextWrapPos(ImGui.GetFontSize() * 28);
            ImGui.TextWrapped("Use these buttons to rerun configuration migrations if something went wrong or you changed something in Title Edit v2.");
            using (ImRaii.PushColor(ImGuiCol.Text, GuiUtils.WarningColor))
                ImGui.TextWrapped("PRESSING THESE BUTTONS WILL OVERWRITE YOUR CURRENT SETTINGS OR ANY OF THE PRESETS THAT HAVE BEEN MIGRATED BEFORE AND MODIFIED AFTER.");
            if (ImGui.Button("Remigrate settings"))
            {
                Services.MigrationService.MigrateTitleScreenV2Configuration();
            }
            ImGui.SameLine();
            if (ImGui.Button("Remigrate title screens"))
            {
                Services.MigrationService.MigrateTitleScreenV2Presets();
            }
        }
    }
}
