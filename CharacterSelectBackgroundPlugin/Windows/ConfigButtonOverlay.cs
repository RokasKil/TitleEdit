using CharacterSelectBackgroundPlugin.Utility;
using Dalamud.Interface.Utility;
using Dalamud.Utility.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System.Numerics;

namespace CharacterSelectBackgroundPlugin.Windows;

//Will be replaed with a native button once I move over to ApiX
public class ConfigButtonOverlay
{
    public string Title => "Config button overlay";

    public void Draw()
    {
        if (!Services.ConfigurationService.DrawCharacterSelectButton || Services.LobbyService.CurrentLobbyMap != Data.Lobby.GameLobbyType.CharaSelect) return;
        Vector2 rightAnchor;
        unsafe
        {
            var addon = (AtkUnitBase*)Services.GameGui.GetAddonByName("_CharaSelectListMenu");
            if (addon == null) return;
            var node = addon->GetNodeById(4);
            if (node == null) return;
            rightAnchor = new(node->ScreenX, node->ScreenY + (node->Height / 2) * addon->Scale);
        }
        var buttonSize = ImGui.CalcTextSize("Immersive Character Select") + ImGui.GetStyle().FramePadding * 2;
        var pos = rightAnchor - buttonSize.WithY(buttonSize.Y / 2);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
        ImGuiHelpers.ForceNextWindowMainViewport();
        ImGuiHelpers.SetNextWindowPosRelativeMainViewport(pos);
        ImGui.Begin($"Canvas##{Title}", ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.AlwaysAutoResize);
        if (ImGui.Button("Immersive Character Select"))
        {
            Services.Plugin.ToggleConfigUI();
        }
        ImGui.SetCursorPos(new(0, 0));
        ImGui.End();
        ImGui.PopStyleVar();

    }

}
