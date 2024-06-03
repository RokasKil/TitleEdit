using CharacterSelectBackgroundPlugin.Utility;
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
            ImGui.TextUnformatted($"Current character {Services.ClientState.LocalContentId:X16}");
            ImGui.TextUnformatted($"Current weather {EnvManager.Instance()->ActiveWeather}");
            ImGui.TextUnformatted($"Current lobbymap {Services.LobbyService.CurrentLobbyMap}");
            var location = Services.LocationService.GetLocationModel(Services.ClientState.LocalContentId);
            ImGui.TextUnformatted($"Current layout {location.Active.Count} {location.Inactive.Count} {location.VfxTriggerIndexes.Count}");
            ImGui.TextUnformatted($"Current Song {Services.BgmService.CurrentSongId}");
            ImGui.TextUnformatted($"Current LobbyMusicIndex {Services.LobbyService.CurrentLobbyMusicIndex}");
            ImGui.TextUnformatted($"Current MountId {location.Mount.MountId}");
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
