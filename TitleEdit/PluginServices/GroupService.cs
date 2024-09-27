using Dalamud.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TitleEdit.Data.Persistence;
using TitleEdit.Utility;

namespace TitleEdit.PluginServices
{
    public class GroupService : AbstractService
    {
        private readonly static Regex FileInvalidSymbolsRegex = new(@"[/\\:*?|""<>]");

        public IReadOnlyDictionary<string, GroupModel> Groups => groups;

        public IEnumerable<KeyValuePair<string, GroupModel>> CharacterSelectGroupEnumerator => Groups.Where(group => group.Value.LocationType == LocationType.CharacterSelect);

        public IEnumerable<KeyValuePair<string, GroupModel>> TitleScreenGroupEnumerator => Groups.Where(group => group.Value.LocationType == LocationType.TitleScreen);
        public IEnumerable<KeyValuePair<string, GroupModel>> EditableGroupEnumerator => Groups.Where(group => !group.Key.StartsWith("?"));


        private readonly Dictionary<string, GroupModel> groups = new(StringComparer.InvariantCultureIgnoreCase);

        private readonly DirectoryInfo saveDirectory;

        public GroupService()
        {
            saveDirectory = ConfigurationService.GetBaseConfigDirectory().CreateSubdirectory(PersistanceConsts.GroupsFolder);
        }

        public override void LoadData()
        {
            LoadBaseGroups();
            LoadSavedGroups();
        }

        private void LoadSavedGroups()
        {
            foreach (var file in saveDirectory.EnumerateFiles())
            {
                if (file.Name.EndsWith(".json", true, null))
                {
                    Services.Log.Debug($"Loading {file.Name}");
                    try
                    {
                        var group = Load(file.FullName);
                        groups[group.FileName] = group;
                    }
                    catch (Exception e)
                    {
                        Services.Log.Error(e, e.Message);
                    }
                }
                else
                {
                    Services.Log.Debug($"Unknown file in preset directory {file.Name}");

                }
            }
        }


        public string Save(GroupModel group)
        {
            Validate(group);
            if (string.IsNullOrEmpty(group.FileName))
            {
                var namePart = FileInvalidSymbolsRegex.Replace(group.Name, "").Truncate(50);
                if (groups.ContainsKey($"{namePart}.json"))
                {
                    int i = 1;
                    while (groups.ContainsKey($"{namePart} ({i}).json")) i++;
                    namePart = $"{namePart} ({i})";
                }
                group.FileName = $"{namePart}.json";
            }

            try
            {
                Services.Log.Debug($"Saving {group.FileName}");
                Util.WriteAllTextSafe(
                    Path.Join(saveDirectory.FullName, group.FileName),
                    JsonConvert.SerializeObject(group)
                );
            }
            catch (Exception e)
            {
                Services.Log.Error(e, e.Message);
                throw;
            }
            groups[group.FileName] = group;
            return group.FileName;
        }

        private GroupModel Load(string path, bool setFileName = true)
        {
            var file = new FileInfo(path);
            var group = LoadText(File.ReadAllText(file.FullName));
            if (setFileName)
            {
                group.FileName = file.Name;
            }
            return group;
        }

        private GroupModel LoadText(string textData)
        {
            var group = Services.MigrationService.MigrateGroup(textData);
            Validate(group);
            return group;
        }

        public void Validate(GroupModel? groupOpt)
        {
            if (!groupOpt.HasValue)
            {
                throw new("Invalid group");
            }
            var group = groupOpt.Value;
            if (group.Version != GroupModel.CurrentVersion)
            {
                throw new($"Group Version is not valid ({group.Version})");
            }
            if (group.PresetFileNames.Any(string.IsNullOrEmpty))
            {
                throw new($"Group has empty items");
            }
            if (group.PresetFileNames.Count == 0)
            {
                throw new($"Group must have at least one item");
            }
            if (string.IsNullOrEmpty(group.Name))
            {
                throw new("Group doesn't have a name");
            }
        }

        public void Delete(string groupFileName)
        {
            if (groups.ContainsKey(groupFileName))
            {
                File.Delete(Path.Join(saveDirectory.FullName, groupFileName));
                groups.Remove(groupFileName);
            }
            return;
        }

        public bool TryGetGroup(string groupFileName, out GroupModel group, LocationType? type = null)
        {
            if (groups.TryGetValue(groupFileName, out group) && (type == null || group.LocationType == type))
            {
                return true;
            }
            return false;
        }

        private void LoadBaseGroups()
        {
            AddGroup(new()
            {
                Name = "Random",
                Tooltip = "Contains all of your title screen presets",
                FileName = "?/RandomTitleScreen.json",
                LocationType = LocationType.TitleScreen
            });
            AddGroup(new()
            {
                Name = "Random",
                Tooltip = "Contains all of your character select presets",
                FileName = "?/RandomCharacterSelect.json",
                LocationType = LocationType.CharacterSelect
            });
        }

        private void AddGroup(GroupModel group)
        {
            groups.Add(group.FileName, group);
        }
    }
}
