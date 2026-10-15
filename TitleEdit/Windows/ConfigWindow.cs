using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using Dalamud.Utility.Numerics;
using TitleEdit.Windows.Tabs;

namespace TitleEdit.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly ITab[] tabs =
    {
        new DisplayTypeTab(),
        new SettingsTab(),
        new PresetTab(),
        new GroupTab(),
        new AboutTab()
    };

    public ConfigWindow() : base(
        "Title Edit Configuration", ImGuiWindowFlags.AlwaysAutoResize)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, 1000)
        };
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        using var id = ImRaii.PushId(WindowName);
        using (ImRaii.Group())
        using (var tabBar = ImRaii.TabBar("##ConfigTabs"))
            if (tabBar)
            {
                foreach (var tab in tabs)
                {
                    using var tabItem = ImRaii.TabItem(tab.Title);
                    if (tabItem)
                    {
                        tab.Draw();
                    }
                }
            }

        KofiButton(ImGui.GetItemRectSize().X + ImGui.GetStyle().WindowPadding.X);
    }

    private void KofiButton(float contentWidth)
    {
        var size = ImGui.CalcTextSize("Buy my cat a treat on Ko-fi") + ImGui.GetStyle().FramePadding * 2;
        if (SizeConstraints.HasValue)
        {
            var minimumSize = SizeConstraints.Value.MinimumSize * ImGui.GetIO().FontGlobalScale;
            contentWidth = Math.Max(contentWidth, minimumSize.X);
            if (ImGui.GetCursorPosY() + size.Y + ImGui.GetStyle().WindowPadding.Y <= minimumSize.Y)
            {
                ImGui.SetCursorPosY(minimumSize.Y - size.Y - ImGui.GetStyle().WindowPadding.Y);
            }
        }

        ImGui.Separator();
        ImGui.SetCursorPosX(contentWidth - size.X);
        using (ImRaii.PushColor(ImGuiCol.Text, 0xFFFFFFFF))
        using (ImRaii.PushColor(ImGuiCol.Button, 0xFFFF478E))
        using (ImRaii.PushColor(ImGuiCol.ButtonHovered, 0xFFFF2478))
        using (ImRaii.PushColor(ImGuiCol.ButtonActive, 0xFFBD0048))
            if (ImGui.Button("Buy my cat a treat on Ko-fi"))
            {
                Util.OpenLink("https://ko-fi.com/speedas");
            }
    }
}