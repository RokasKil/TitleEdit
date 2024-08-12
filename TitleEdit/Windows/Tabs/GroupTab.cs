using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TitleEdit.Data.Persistence;
using TitleEdit.Utility;

namespace TitleEdit.Windows.Tabs
{
    internal class GroupTab : AbstractTab
    {
        public override string Title => "Groups";

        bool makingNewGroup = false;
        string currentGroup = "";
        GroupModel group = new();

        public override void Draw()
        {
            base.Draw();
            DrawGroupListControls();
            ImGui.Separator();
            DrawGroupControls();
            DrawGroupActions();
        }

        private void DrawGroupListControls()
        {
            string currentGroupName;
            if (makingNewGroup)
            {
                currentGroupName = group.LocationType switch
                {
                    LocationType.TitleScreen => "Unsaved Title Screen group",
                    LocationType.CharacterSelect => "Unsaved Character Select group",
                    _ => throw new Exception()
                };
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.8f, 0, 1));
            }
            else if (currentGroup == "")
            {
                currentGroupName = "";
            }
            else
            {
                currentGroupName = GetGroupEntryName(Services.GroupService.Groups.GetValueOrDefault(currentGroup));
            }
            bool stylePopped = false;

            GuiUtils.FilterCombo($"##{Title}##groupCombo", currentGroupName, () =>
            {
                if (makingNewGroup)
                {
                    ImGui.PopStyleColor();
                    stylePopped = true;
                }
                foreach (var entry in Services.GroupService.Groups)
                {
                    if (GuiUtils.FilterSelectable($"{GetGroupEntryName(entry.Value)}##{Title}##{entry.Key}", entry.Key == currentGroup))
                    {
                        SelectGroup(entry.Key);
                        return true;
                    }
                    GuiUtils.DrawGroupTooltip(entry.Key);
                }
                return false;
            });
            if (makingNewGroup && !stylePopped)
            {
                ImGui.PopStyleColor();
            }
            ImGui.SameLine();
            ImGui.BeginDisabled(makingNewGroup || currentGroup == "");
            if (ImGuiComponents.IconButton($"##{Title}##Duplicate", FontAwesomeIcon.Copy))
            {
                DuplicateGroup(currentGroup);
            }
            ImGui.EndDisabled();
            GuiUtils.HoverTooltip("Duplicate group");
            ImGui.BeginDisabled(makingNewGroup);
            if (ImGui.Button($"New Title Screen group##{Title}##NewGroup"))
            {
                NewGroup(LocationType.TitleScreen);
            }
            ImGui.SameLine();
            if (ImGui.Button($"New Character Select group##{Title}##NewGroup"))
            {
                NewGroup(LocationType.CharacterSelect);
            }
            ImGui.EndDisabled();
        }

        private void DrawGroupControls()
        {
            ImGui.BeginDisabled(!makingNewGroup && currentGroup == "");

            ImGui.InputText($"Name##{Title}", ref group.Name, 256);
            var tableHeight = MathF.Max(1f, MathF.Min(8f, group.PresetFileNames.Count) * (ImGui.CalcTextSize(" ").Y + ImGui.GetStyle().CellPadding.Y * 2 + ImGui.GetStyle().FramePadding.Y * 2));
            using (ImRaii.Table($"Group entries##{Title}", 2, ImGuiTableFlags.ScrollY, new(0f, tableHeight)))
            {
                var usedPresets = new HashSet<string?>(group.PresetFileNames); // performance?
                for (int i = 0; i < group.PresetFileNames.Count; i++)
                {
                    var presetPath = group.PresetFileNames[i];
                    ImGui.TableNextColumn();

                    ImGui.SetNextItemWidth(16f * ImGui.GetFontSize());
                    //
                    string display;
                    bool invalid = false;
                    if (presetPath == null)
                    {
                        display = "";
                    }
                    else if (Services.PresetService.TryGetPreset(presetPath, out var preset, group.LocationType))
                    {
                        display = preset.Name;
                    }
                    else
                    {
                        display = "Invalid preset";
                        invalid = true;
                    }

                    if (invalid)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.8f, 0, 1));
                    }
                    GuiUtils.FilterCombo($"##{Title}##grouprow{i}", display, () =>
                    {
                        foreach (var entry in Services.PresetService.Presets.Where(preset => preset.Value.LocationModel.LocationType == group.LocationType && !group.PresetFileNames.Contains(preset.Key)))
                        {
                            if (GuiUtils.FilterSelectable($"{entry.Value.Name}##{Title}##grouprow{i}##{entry.Key}", entry.Key == presetPath))
                            {
                                group.PresetFileNames[i] = entry.Key;
                                return true;
                            }
                            GuiUtils.DrawPresetTooltip(entry.Key, group.LocationType);

                        }
                        return false;
                    }, popStyleColor: invalid);
                    if (presetPath != null)
                    {
                        GuiUtils.DrawPresetTooltip(presetPath, group.LocationType);
                    }
                    //
                    ImGui.TableNextColumn();
                    if (ImGuiComponents.IconButton($"##{Title}##{i}##Delete", FontAwesomeIcon.Trash))
                    {
                        group.PresetFileNames.RemoveRange(i, 1);
                    }
                }
            }

            //
            if (ImGui.Button($"Add preset##{Title}##addEntry"))
            {
                group.PresetFileNames.Add(null);
            }
            ImGui.EndDisabled();
        }

        private void DrawGroupActions()
        {
            ImGui.BeginDisabled(!makingNewGroup && currentGroup == "");
            if (ImGui.Button($"Save##{Title}"))
            {
                try
                {
                    currentGroup = Services.GroupService.Save(group);
                    makingNewGroup = false;
                    group = Services.GroupService.Groups[currentGroup];
                }
                catch (Exception ex)
                {
                    SetupError(ex);
                }
            }

            ImGui.SameLine();
            if (makingNewGroup)
            {

                if (ImGui.Button($"Cancel##{Title}"))
                {
                    ClearGroup();
                }
            }
            else
            {
                if (ImGui.Button($"Delete##{Title}"))
                {
                    SetupDeleteConfirmation();
                }
            }
            ImGui.EndDisabled();
        }

        private string GetGroupEntryName(GroupModel group)
        {
            var prefix = group.LocationType switch
            {
                LocationType.CharacterSelect => "C",
                LocationType.TitleScreen => "T",
                _ => "?"
            };
            return $"[{prefix}] {group.Name}";
        }

        public void SelectGroup(string groupFileName)
        {
            if (Services.GroupService.Groups.TryGetValue(groupFileName, out var groupModel))
            {
                group = groupModel;
                currentGroup = groupFileName;
                makingNewGroup = false;
            }
        }

        public void ClearGroup()
        {
            group = new();
            makingNewGroup = false;
            currentGroup = "";
        }

        public void DuplicateGroup(string groupFileName)
        {
            if (Services.GroupService.Groups.TryGetValue(groupFileName, out var groupModel))
            {
                group = groupModel;
                makingNewGroup = true;
                currentGroup = "";
                group.FileName = "";
            }
        }

        public void NewGroup(LocationType type)
        {
            makingNewGroup = true;
            currentGroup = "";
            group = new();
            group.PresetFileNames.Add(null);
            group.LocationType = type;
        }

        private void SetupDeleteConfirmation() => SetupModal("Delete a group", DrawDeleteConfirmation);

        private void DrawDeleteConfirmation()
        {
            ImGui.TextUnformatted("Are you sure you want to delete the preset, this is unrecoverable.");
            if (ImGui.Button("Yes"))
            {
                modal = false;
                try
                {
                    Services.GroupService.Delete(currentGroup);
                    ClearGroup();
                }
                catch (Exception ex)
                {
                    SetupError(ex);
                }
            }
            ImGui.SameLine();
            if (ImGui.Button("No")) modal = false;
        }
    }
}
