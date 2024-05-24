using CharacterSelectBackgroundPlugin.PluginServices;
using CharacterSelectBackgroundPlugin.Utils;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;

namespace CharacterSelectBackgroundPlugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private ConfigurationService Configuration => Services.ConfigurationService;

    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base("A Wonderful Configuration Window###With a constant ID")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(232, 75);
        SizeCondition = ImGuiCond.Always;

    }

    public void Dispose() { }


    public override void Draw()
    {
        CheckboxConfig("Get Layout", Configuration.SaveLayout, value => Configuration.SaveLayout = value);
    }

    private void CheckboxConfig(string title, bool value, Action<bool> action, bool save = true)
    {
        if (ImGui.Checkbox(title, ref value))
        {
            action(value);
            if (save)
            {
                this.Configuration.Save();
            }
        }
    }
}
