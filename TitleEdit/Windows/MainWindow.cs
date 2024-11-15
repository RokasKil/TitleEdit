using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using ImGuiNET;
using System;
using System.Numerics;
using TitleEdit.Utility;

namespace TitleEdit.Windows;

public class MainWindow : Window, IDisposable
{
    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow()
        : base("My Amazing Debug Window##TitleScreen", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
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
            ImGui.TextUnformatted($"Selected character {(IntPtr)Services.LobbyService.CurrentCharacter:X16} - {Services.LobbyService.CurrentContentId:X16}");
            ImGui.TextUnformatted($"Current weather {Services.WeatherService.WeatherId}");
            ImGui.TextUnformatted($"Current lobbymap {Services.LobbyService.CurrentLobbyMap}");
            var location = Services.LocationService.GetLocationModel(Services.ClientState.LocalContentId);
            ImGui.TextUnformatted($"Current layout {location.Active.Count} {location.Inactive.Count} {location.VfxTriggerIndexes.Count}");
            //ImGui.TextUnformatted($"Current layout2 {Services.LocationService.Active.Count} {Services.LocationService.Inactive.Count} {Services.LocationService.VfxTriggerIndexes.Count}");
            //ImGui.TextUnformatted($"Current layout3 {location.Active.All(Services.LocationService.Active.Contains)} {location.Inactive.All(Services.LocationService.Inactive.Contains)} {location.VfxTriggerIndexes.Keys.Count == Services.LocationService.VfxTriggerIndexes.Keys.Count && location.VfxTriggerIndexes.Keys.All(k => Services.LocationService.VfxTriggerIndexes.ContainsKey(k) && object.Equals(location.VfxTriggerIndexes[k], Services.LocationService.VfxTriggerIndexes[k]))}");
            ImGui.TextUnformatted($"Current Song {Services.BgmService.CurrentSongId}");
            ImGui.TextUnformatted($"Current LobbyMusicIndex {Services.LobbyService.LobbyInfo->CurrentLobbyMusicIndex}");
            ImGui.TextUnformatted($"Current MountId {location.Mount.MountId}");
            ImGui.TextUnformatted($"Update time {Services.LayoutService.UpdateTime}");
            ImGui.SliderAngle($"test", ref test1, -360, 360);
            ImGui.TextUnformatted($"Normalized {Utils.NormalizeAngle(test1) * Utils.RadToDegreeRatio}");
            ImGui.SliderFloat($"Testing normalizer", ref test2, -720, 720);
            ImGui.TextUnformatted($"Normalized {Utils.NormalizeAngle((float)(test2 * Utils.DegreeToRadRatio)) * Utils.RadToDegreeRatio}");
            ImGui.TextUnformatted($"Time: {Services.LocationService.TimeOffset}");
            if (ImGui.Button("Remigrate title screens"))
            {
                Services.MigrationService.MigrateTitleScreenV2Presets();
            }

            ImGui.TextUnformatted($"LobbyUiStage {Services.LobbyService.LobbyUiStage}");
            ImGui.TextUnformatted($"TitleCutsceneIsLoaded {Services.LobbyService.TitleCutsceneIsLoaded}");
            if (ImGui.Button("Reload title screen UI"))
            {
                Services.LobbyService.ReloadTitleScreenUi();
            }

            if (ImGui.Button("Reload title screen colors"))
            {
                Services.LobbyService.RecolorTitleScreenUi();
            }

            if (ImGui.Button("Reload title screen"))
            {
                Services.LobbyService.ReloadTitleScreen();
            }

            ImGui.TextUnformatted($"Available expansion {UIState.Instance()->PlayerState.MaxExpansion}");
            if (ImGui.CollapsingHeader("Versions"))
            {
                var framework = Framework.Instance();
                ImGui.TextUnformatted($"Game version: {framework->GameVersionString} {framework->GameVersionString.Length}");
                ImGui.TextUnformatted($"Heavensward: {framework->Ex1VersionString} {framework->Ex1VersionString.Length}");
                ImGui.TextUnformatted($"Stormblood: {framework->Ex2VersionString} {framework->Ex2VersionString.Length}");
                ImGui.TextUnformatted($"Shadowbringers: {framework->Ex3VersionString} {framework->Ex3VersionString.Length}");
                ImGui.TextUnformatted($"Endwalker: {framework->Ex4VersionString} {framework->Ex4VersionString.Length}");
                ImGui.TextUnformatted($"Dawntrail: {framework->Ex5VersionString} {framework->Ex5VersionString.Length}");
            }
        }
    }
}
