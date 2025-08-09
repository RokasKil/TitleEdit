using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;
using Dalamud.Interface.Utility.Raii;
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

    public void Dispose() { }

    public override void Draw()
    {
        using var id = ImRaii.PushId(WindowName);
        using var tabBar = ImRaii.TabBar("##ConfigTabs");
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
    }
}
