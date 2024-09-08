using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace TitleEdit.Windows.Tabs
{
    internal class AboutTab : AbstractTab
    {
        public override string Title => "About";

        public override void Draw()
        {
            base.Draw();
            using var textWrapPos = ImRaii.TextWrapPos(ImGui.GetFontSize() * 28);
            ImGui.TextWrapped("Welcome to Title Edit V3, this is a testing release, while it hasn't caused any crashes in my personal testing, expect to see some issues.");
            ImGui.TextWrapped("If you encounter any issues that are not listed in the Known Issues or have information about them that you feel " +
                              "would be valuable you can report it in the Dalamud's discord #plugin-testing channel or the Title Edit post in #plugin-help-forum. " +
                              "Don't forget to tag Speedas so I don't miss it!");

            if (ImGui.CollapsingHeader($"Tips and tricks##{Title}"))
            {
                WrappedBulletText("You can ctrl+click most sliders/drag inputs to enter values manually");
                WrappedBulletText("You can alt+drag most drag inputs to change values slowly");
                WrappedBulletText("You can shift+drag most drag inputs to change values rapidly");
                WrappedBulletText("You can press ctrl+T to open this window while not logged in");
            }
            if (ImGui.CollapsingHeader($"Known Issues##{Title}"))
            {
                ImGui.TextWrapped("Unless specifically otherwise said, these issues only affect the character or title screen");
                WrappedBulletText("World and character takes longer to load in when in housing zones");
                WrappedBulletText("Certain BGM tracks will not loop");
                WrappedBulletText("Certain zones with multiple layouts will not load the correct one e.g. Endwalker gatherer tribe subzone");
                WrappedBulletText("With experimental layout enabled some VFX objects will play every time you load the scene often happening in instances");
                WrappedBulletText("Experimental layout saving being janky in general");
            }
            if (ImGui.CollapsingHeader($"Planned Features##{Title}"))
            {
                ImGui.TextWrapped("These are not confirmed or researched, or have any date planned. They're something I personally think would be nice to have");
                WrappedBulletText("Character select companion support");
                WrappedBulletText("Character select ornament support");
                WrappedBulletText("House and housing zone furnishing placement saving support for presets with layouts");
                WrappedBulletText("Layout editor allowing you to turn off/on certain parts of the map");
                WrappedBulletText("Festival selector for presets");
            }
            if (ImGui.CollapsingHeader($"Credits##{Title}"))
            {
                WrappedBulletText("Speedas - initial Dawntrail update, full plugin rewrite to 3.0");
                WrappedBulletText("attick - Title Edit 1.0 and many functions of 2.0");
                WrappedBulletText("perchbird - Custom title screens and supporting features, maintaining the plugin before Dawntrail");
                WrappedBulletText("ff-meli - BGM now playing code");
                WrappedBulletText("goat - being a caprine individual");

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
