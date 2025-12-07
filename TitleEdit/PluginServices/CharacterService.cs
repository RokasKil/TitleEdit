using Dalamud.Game;
using Dalamud.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Lumina.Excel.Sheets;
using TitleEdit.Utility;

namespace TitleEdit.PluginServices
{
    public class CharactersService : AbstractService
    {
        public IReadOnlyDictionary<ulong, string> Characters => characters;
        private Dictionary<ulong, string> characters = [];
        private bool changed;
        private readonly string filePath;

        public CharactersService()
        {
            filePath = Path.Join(ConfigurationService.GetBaseConfigDirectory().CreateSubdirectory(PersistanceConsts.DataFolder).FullName, PersistanceConsts.CharactersName);
        }

        public override void LoadData()
        {
            LoadCharacters();
        }

        public override void Init()
        {
            if (Services.ObjectTable.LocalPlayer != null)
            {
                var world = Services.DataManager.GetExcelSheet<World>(ClientLanguage.English).GetRow(Services.ClientState.LocalPlayer.HomeWorld.RowId);
                PutCharacter(Services.ClientState.LocalContentId, $"{Services.ClientState.LocalPlayer.Name}@{world.Name}");
            }

            foreach (var entry in Services.LobbyService.GetCurrentCharacterNames())
            {
                PutCharacter(entry.Key, entry.Value);
            }

            SaveCharacters();
        }

        private void LoadCharacters()
        {
            try
            {
                if (File.Exists(filePath))
                {
                    characters = JsonConvert.DeserializeObject<Dictionary<ulong, string>>(File.ReadAllText(filePath)) ?? [];
                    Services.Log.Debug($"Loaded characters {characters.Count}");
                }
            }
            catch (Exception e)
            {
                Services.Log.Error(e, e.Message);
            }
        }


        public void SaveCharacters(bool force = false)
        {
            if (changed || force)
            {
                try
                {
                    Services.Log.Debug($"Saving characters");
                    FilesystemUtil.WriteAllTextSafe(filePath, JsonConvert.SerializeObject(characters));
                    changed = false;
                }
                catch (Exception e)
                {
                    Services.Log.Error(e, e.Message);
                }
            }
        }

        public void PutCharacter(ulong contentId, string name)
        {
            if (!characters.TryGetValue(contentId, out var currentName) || currentName != name)
            {
                characters[contentId] = name;
                changed = true;
            }
        }
    }
}
