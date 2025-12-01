using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;
using TitleEdit.Data.Persistence;
using TitleEdit.Utility;

namespace TitleEdit.Windows;

public class ShareImportConfirmationPopup
{
    private PresetModel? preset;
    private bool isModalOpen;

    public void Draw()
    {
        if (preset == null && Services.ShareService.ConfirmationQueue.TryDequeue(out var presetModel))
        {
            preset = presetModel;
            isModalOpen = true;
        }

        // If settings changed just confirm the whole queue
        if (preset != null && !Services.ConfigurationService.PromptForUrlImport)
        {
            Services.ShareService.ConfirmImport(preset.Value);
            preset = null;
            return;
        }

        if (preset != null)
        {
            ImGui.OpenPopup("Import confirmation");
            using var popupModal = ImRaii.PopupModal("Import confirmation", ref isModalOpen, ImGuiWindowFlags.NoNav | ImGuiWindowFlags.AlwaysAutoResize);
            if (popupModal.Success)
            {
                ImGui.Text($"Are you sure you want to import this {preset.Value.LocationModel.LocationType.ToText()} preset:");
                ImGui.TextUnformatted($"Name: {preset.Value.Name}");
                if (!string.IsNullOrEmpty(preset.Value.Author))
                {
                    ImGui.TextUnformatted($"Author: {preset.Value.Author}");
                }

                ImGui.TextUnformatted($"Zone: {Utils.GetTerritoryString(preset.Value.LocationModel.TerritoryTypeId) ?? "Unknown"}");


                if (ImGui.Button("Yes"))
                {
                    Services.ShareService.ConfirmImport(preset.Value);
                    preset = null;
                }

                ImGui.SameLine();
                if (ImGui.Button("Yes and don't ask again"))
                {
                    Services.ShareService.ConfirmImport(preset!.Value);
                    Services.ConfigurationService.PromptForUrlImport = false;
                    Services.ConfigurationService.Save();
                    preset = null;
                }

                ImGui.SameLine();
                if (ImGui.Button("No"))
                {
                    preset = null;
                }
            }

            if (preset != null && !isModalOpen)
            {
                preset = null;
                Services.Log.Info("Close import confirmation");
            }
        }
    }
}
