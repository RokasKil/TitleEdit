using CharacterSelectBackgroundPlugin.Utils;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using ImGuiNET;
using System;
using System.Numerics;

namespace CharacterSelectBackgroundPlugin.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;

    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin)
        : base("My Amazing Window##asdf", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        unsafe
        {
            ImGui.Text($"Current character {Services.ClientState.LocalContentId:X16}");
            ImGui.Text($"Current weather {EnvManager.Instance()->ActiveWeather}");
            ImGui.Text($"Current lobbymap {Services.LobbyService.CurrentLobbyMap}");
            ImGui.Text($"Current layout {Services.LocationService.GetLocationModel(Services.ClientState.LocalContentId).Active.Count} {Services.LocationService.GetLocationModel(Services.ClientState.LocalContentId).Inactive.Count} ");
        }
        if (ImGui.Button("weather"))
        {
        }
        if (ImGui.Button("Show Settings"))
        {
            Plugin.ToggleConfigUI();
        }
    }
}
