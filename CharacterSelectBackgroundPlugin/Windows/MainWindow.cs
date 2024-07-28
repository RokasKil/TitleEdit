using CharacterSelectBackgroundPlugin.Utility;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using ImGuiNET;
using System;
using System.Linq;
using System.Numerics;

namespace CharacterSelectBackgroundPlugin.Windows;

public class MainWindow : Window, IDisposable
{

    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow()
        : base("My Amazing Window##asdf", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

    }

    private float test1 = 0;
    private float test2 = 0;

    public void Dispose() { }

    public override void Draw()
    {
        unsafe
        {
            ImGui.TextUnformatted($"Current character {Services.ClientState.LocalContentId:X16}");
            ImGui.TextUnformatted($"Selected character {(IntPtr)Services.LobbyService.CurrentCharacter:X16}");
            ImGui.TextUnformatted($"Current weather {EnvManager.Instance()->ActiveWeather}");
            ImGui.TextUnformatted($"Current lobbymap {Services.LobbyService.CurrentLobbyMap}");
            var location = Services.LocationService.GetLocationModel(Services.ClientState.LocalContentId);
            ImGui.TextUnformatted($"Current layout {location.Active.Count} {location.Inactive.Count} {location.VfxTriggerIndexes.Count}");
            ImGui.TextUnformatted($"Current layout2 {Services.LocationService.Active.Count} {Services.LocationService.Inactive.Count} {Services.LocationService.VfxTriggerIndexes.Count}");
            ImGui.TextUnformatted($"Current layout3 {location.Active.All(Services.LocationService.Active.Contains)} {location.Inactive.All(Services.LocationService.Inactive.Contains)} {location.VfxTriggerIndexes.Keys.Count == Services.LocationService.VfxTriggerIndexes.Keys.Count && location.VfxTriggerIndexes.Keys.All(k => Services.LocationService.VfxTriggerIndexes.ContainsKey(k) && object.Equals(location.VfxTriggerIndexes[k], Services.LocationService.VfxTriggerIndexes[k]))}");
            ImGui.TextUnformatted($"Current Song {Services.BgmService.CurrentSongId}");
            ImGui.TextUnformatted($"Current LobbyMusicIndex {Services.LobbyService.CurrentLobbyMusicIndex}");
            ImGui.TextUnformatted($"Current MountId {location.Mount.MountId}");
            ImGui.TextUnformatted($"Update time {Services.LayoutService.UpdateTime}");
            ImGui.SliderAngle($"test", ref test1, -360, 360);
            ImGui.TextUnformatted($"Normalized {Utils.NormalizeAngle(test1) / Math.PI * 180}");
            ImGui.SliderFloat($"Testing normalizer", ref test2, -720, 720);
            ImGui.TextUnformatted($"Normalized {Utils.NormalizeAngle((float)(test2 / 180 * Math.PI)) / Math.PI * 180}");
        }
    }
}
