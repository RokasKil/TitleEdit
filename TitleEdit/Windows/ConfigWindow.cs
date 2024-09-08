using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;
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
        new AboutTab(),
        new ExtrasTab()
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
        if (ImGui.BeginTabBar($"##{WindowName}##Config Tabs"))
        {
            foreach (var tab in tabs)
            {
                if (ImGui.BeginTabItem(tab.Title))
                {
                    tab.Draw();
                    ImGui.EndTabItem();
                }
            }
            ImGui.EndTabBar();
        }
    }
}
