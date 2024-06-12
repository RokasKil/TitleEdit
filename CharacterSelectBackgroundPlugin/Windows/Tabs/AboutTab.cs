using ImGuiNET;

namespace CharacterSelectBackgroundPlugin.Windows.Tabs
{
    internal class AboutTab : ITab
    {
        public string Title => "About";

        public unsafe void Draw()
        {

            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 28);
            ImGui.TextWrapped("This is a testing release, while it hasn't caused any crashes in my personal testing, expect to see some issues.");
            ImGui.TextWrapped("If you encounter any issues that are not listed in the Known Issues or have information about them that you feel " +
                              "would be valuable you can report it in the Dalamud's discord #plugin-testing channel, create an issue on github " +
                              "or write me a DM on discord @speedas");


            if (ImGui.CollapsingHeader($"Known Issues##{Title}"))
            {
                ImGui.TextWrapped("Unless specifically otherwise said, these issues only affect the character select screen");
                WrappedBulletText("World and character takes longer to load in when in housing zones");
                WrappedBulletText("Vfx assets that have played out might show up in character select on scene load e.g. retainer bell flash");
                WrappedBulletText("Certain BGM tracks will not loop");
                WrappedBulletText("In rare cases your camera will swiftly fly in on the vertical axis when switching characters instead of starting out focused");
                WrappedBulletText("Certain zones with multiple layouts will not load the correct one e.g. Endwalker gatherer tribe subzone");
            }
            if (ImGui.CollapsingHeader($"Planned Features##{Title}"))
            {
                ImGui.TextWrapped("These are not confirmed nor researched, they're something I personally think would be nice to have");
                WrappedBulletText("Companion support");
                WrappedBulletText("Ornament support");
                WrappedBulletText("House and housing zone furnishing placement saving");
            }
            if (ImGui.CollapsingHeader($"Special Thanks##{Title}"))
            {
                WrappedBulletText("attick and perchbird for TitleEdit which served as a nice resource and starting point ");
                WrappedBulletText("perchbird and Mali for Orchestrion Plugin whose code helped with the BGM portion of this plugin");
                WrappedBulletText("Everyone behind the Dalamud and FFXIVClientStructs projects");
                WrappedBulletText("My cat for being cute");

            }
            ImGui.PopTextWrapPos();

        }

        private void WrappedBulletText(string text)
        {

            ImGui.Bullet();
            ImGui.TextWrapped(text);
        }
    }
}
