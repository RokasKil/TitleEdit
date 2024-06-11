using CharacterSelectBackgroundPlugin.Windows.Tabs;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;

namespace CharacterSelectBackgroundPlugin.Windows;

public class ConfigWindow : Window, IDisposable
{

    private readonly ITab[] tabs =
{
        new DisplayTypeTab(),
        new SettingsTab(),
        new PresetTab(),
        new AboutTab()
    };

    public ConfigWindow() : base(
        "Immersive Character Background Configuration", ImGuiWindowFlags.AlwaysAutoResize)
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
        if (ImGui.BeginTabBar("###Config Tabs"))
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
