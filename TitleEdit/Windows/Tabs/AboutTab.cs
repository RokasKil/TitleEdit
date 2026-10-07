using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using System.Linq;
using TitleEdit.Utility;

namespace TitleEdit.Windows.Tabs
{
    internal class AboutTab : AbstractTab
    {
        public override string Title => "About";

        public override void Draw()
        {
            base.Draw();
            using var textWrapPos = ImRaii.TextWrapPos(ImGui.GetFontSize() * 28);
            ImGui.TextWrapped("Title Edit Chronicle is a fork of Title Edit V3. It keeps every custom title and character-select feature, and adds live Main Scenario tracking so the vanilla title + movie follow the character you are actually playing.");
            ImGui.TextWrapped("Unlike patch 7.1's built-in setting, Chronicle uses the currently logged-in character (not the furthest on the account), works on Free Trial, and reloads the lobby as soon as you log out — no client relaunch.");
            ImGui.TextWrapped("Please be advised that this is a fork so exercise your cognizance of where which issues should be reported where. " +
                                "If you find the addition of functionality added here useful please vouch for its inclusion in the parent (RokasKil) repository." );

            ImGui.TextWrapped("If you're looking for more presets head over to Dalamud's official discord and check out the #preset-sharing channel.");

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
                WrappedBulletText("With experimental layout enabled some VFX objects will play every time you load the scene often happening in instances");
                WrappedBulletText("Experimental layout saving being janky in general");
            }


            if (ImGui.CollapsingHeader($"Credits##{Title}"))
            {
                WrappedBulletText("RokasKil — current maintainer of TitleEdit");
                WrappedBulletText("Speedas - initial Dawntrail update, full plugin rewrite to 3.0");
                WrappedBulletText("attick - Title Edit 1.0 and many functions of 2.0");
                WrappedBulletText("perchbird - Custom title screens and supporting features, maintaining the plugin before Dawntrail");
                WrappedBulletText("ff-meli - BGM now playing code");
                WrappedBulletText("goat - being a caprine individual");
            }
        }

        private void WrappedBulletText(string text)
        {
            ImGui.Bullet();
            ImGui.TextWrapped(text);
        }
    }
}
