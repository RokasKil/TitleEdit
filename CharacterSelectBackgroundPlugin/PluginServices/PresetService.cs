using CharacterSelectBackgroundPlugin.Data.Persistence;
using CharacterSelectBackgroundPlugin.Utility;
using Dalamud.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace CharacterSelectBackgroundPlugin.PluginServices
{
    public class PresetService : AbstractService
    {
        private readonly static Regex FileInvalidSymbolsRegex = new(@"[/\\:*?|""<>]");

        public IReadOnlyDictionary<string, PresetModel> Presets => presets;
        private readonly Dictionary<string, PresetModel> presets = new(StringComparer.InvariantCultureIgnoreCase);

        private readonly DirectoryInfo saveDirectory;
        public PresetService()
        {
            saveDirectory = Services.PluginInterface.ConfigDirectory.CreateSubdirectory("presets");
            LoadSavedPresets();
        }
        private void LoadSavedPresets()
        {
            foreach (var file in saveDirectory.EnumerateFiles())
            {
                if (file.Name.EndsWith(".json", true, null))
                {
                    Services.Log.Debug($"Loading {file.Name}");
                    try
                    {
                        var preset = Load(file.FullName);
                        presets[preset.FileName] = preset;
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
                if (presets.ContainsKey($"{namePart}.json"))
                {
                    int i = 1;
                    while (presets.ContainsKey($"{namePart} ({i}).json")) i++;
                    namePart = $"{namePart} ({i})";
                }
                preset.FileName = $"{namePart}.json";
            }

            try
            {
                Services.Log.Debug($"Saving {preset.FileName}");
                Util.WriteAllTextSafe(
                    Path.Join(saveDirectory.FullName, preset.FileName),
                    JsonConvert.SerializeObject(preset)
                );
            }
            catch (Exception e)
            {
                Services.Log.Error(e, e.Message);
                throw;
            }
            presets[preset.FileName] = preset;
            return preset.FileName;
        }

        public void Export(string presetFileName, string filePath)
        {
            if (presets.TryGetValue(presetFileName, out var preset))
            {
                try
                {
                    Services.Log.Debug($"Exporting {preset.FileName} to {filePath}");
                    Util.WriteAllTextSafe(
                        filePath,
                        JsonConvert.SerializeObject(preset)
                    );
                }
                catch (Exception e)
                {
                    Services.Log.Error(e, e.Message);
                    throw;
                }
            }
            throw new("Preset not found");
        }

        public string ExportText(string presetFileName)
        {
            if (presets.TryGetValue(presetFileName, out var preset))
            {
                try
                {
                    Services.Log.Debug($"Exporting {preset.FileName}");
                    return JsonConvert.SerializeObject(preset);
                }
                catch (Exception e)
                {
                    Services.Log.Error(e, e.Message);
                    throw;
                }
            }
            throw new("Preset not found");
        }

        public string Import(string filePath)
        {
            PresetModel preset;
            try
            {
                preset = Load(filePath, false);
            }
            catch (Exception e)
            {
                Services.Log.Error(e, e.Message);
                throw;
            }
            return Save(preset);
        }

        public string ImportText(string textData)
        {
            PresetModel preset;
            try
            {
                preset = LoadText(textData);
            }
            catch (Exception e)
            {
                Services.Log.Error(e, e.Message);
                throw;
            }
            return Save(preset);
        }

        private PresetModel Load(string path, bool setFileName = true)
        {
            var file = new FileInfo(path);
            var preset = LoadText(File.ReadAllText(file.FullName));
            if (setFileName)
            {
                preset.FileName = file.Name;
            }
            return preset;
        }

        private PresetModel LoadText(string textData)
        {
            var preset = JsonConvert.DeserializeObject<PresetModel>(textData);
            if (preset.Version != 1 || string.IsNullOrEmpty(preset.Name))
            {
                throw new("Invalid preset");
            }
            Services.LocationService.Validate(preset.LocationModel);
            return preset;
        }

        public void Delete(string presetFileName)
        {
            if (presets.ContainsKey(presetFileName))
            {
                File.Delete(Path.Join(saveDirectory.FullName, presetFileName));
                presets.Remove(presetFileName);
            }
            return;
        }

        public override void Dispose()
        {
            base.Dispose();
        }

    }
}
