using CharacterSelectBackgroundPlugin.Data.Persistence;
using CharacterSelectBackgroundPlugin.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace CharacterSelectBackgroundPlugin.PluginServices
{
    public class PresetService : AbstractService
    {
        private readonly static Regex FileNameRegex = new(@"^.*\((\d+)\)\.json$", RegexOptions.IgnoreCase);
        private readonly static Regex FileInvalidSymbolsRegex = new(@"[/\\:*?|""<>]");

        public Dictionary<string, PresetModel> Presets { get; private set; } = new(StringComparer.InvariantCultureIgnoreCase);

        private DirectoryInfo saveDirectory;
        public PresetService()
        {
            LoadSavedPresets();
        }
        private void LoadSavedPresets()
        {
            saveDirectory = Services.PluginInterface.ConfigDirectory.CreateSubdirectory("presets");
            foreach (var file in saveDirectory.EnumerateFiles())
            {
                if (file.Name.EndsWith(".json", true, null))
                {
                    Services.Log.Debug($"Loading {file.Name}");
                    try
                    {
                        var preset = JsonConvert.DeserializeObject<PresetModel>(File.ReadAllText(file.FullName));
                        //TODO: validate
                        preset.FileName = file.Name;
                        Presets[preset.FileName] = preset;
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


        public string Save(PresetModel preset)
        {
            if (string.IsNullOrEmpty(preset.FileName))
            {
                var namePart = FileInvalidSymbolsRegex.Replace(preset.Name, "").Truncate(50);
                if (Presets.ContainsKey($"{namePart}.json"))
                {
                    int i = 1;
                    while (Presets.ContainsKey($"{namePart} ({i}).json")) i++;
                    namePart = $"{namePart} ({i})";
                }
                preset.FileName = $"{namePart}.json";
            }

            try
            {
                Services.Log.Debug($"Saving {preset.FileName}");
                File.WriteAllText(
                    Path.Join(saveDirectory.FullName, preset.FileName),
                    JsonConvert.SerializeObject(preset)
                );
            }
            catch (Exception e)
            {
                Services.Log.Error(e, e.Message);
                throw;
            }
            Presets[preset.FileName] = preset;
            return preset.FileName;
        }
        public void Delete(string presetFileName)
        {
            if (Presets.ContainsKey(presetFileName))
            {
                File.Delete(Path.Join(saveDirectory.FullName, presetFileName));
                Presets.Remove(presetFileName);
            }
            return;
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
