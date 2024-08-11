using ImGuiNET;

namespace TitleEdit.Windows.Tabs
{
    internal class GroupTab : ITab
    {
        public string Title => "Groups";

        public unsafe void Draw()
        {
            ;
            ImGui.TextUnformatted("WIP");
        }
    }
}
