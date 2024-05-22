using CharacterSelectBackgroundPlugin.Data;
using CharacterSelectBackgroundPlugin.Utils;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CharacterSelectBackgroundPlugin.PluginServices
{
    public class LocationService : IDisposable
    {
        private readonly static Regex FileNameRegex = new("^([A-F0-9]{16})\\.json$", RegexOptions.IgnoreCase);

        public static readonly LocationModel DefaultLocation = new LocationModel
        {
            TerritoryPath = "ffxiv/zon_z1/chr/z1c1/level/z1c1",
            Position = Vector3.Zero,
            Rotation = 0,
            WeatherId = 2,
            TimeOffset = 0,
            BgmPath = "music/ffxiv/BGM_System_Title.scd"
        };

        private readonly CancellationTokenSource cancellationToken = new();
        private DateTime lastSave = DateTime.Now;
        private string? territoryPath = null;
        private ConcurrentDictionary<ulong, LocationModel> locations = [];
        private ulong lastContentId;
        public LocationService()
        {
            LoadSavedLocations();

            //foreach (var pair in Services.ConfigurationService.Locations)
            //{
            //    locations[pair.Key] = pair.Value;
            //    Save(pair.Key);
            //}
            Services.Framework.Update += Tick;
            Services.ClientState.Logout += OnLogout;
            Services.ClientState.TerritoryChanged += TerritoryChanged;
            TerritoryChanged(Services.ClientState.TerritoryType);
            Task.Run(SaveTask, cancellationToken.Token);

        }

        private unsafe void Tick(IFramework framework)
        {
            if (Services.ClientState.LocalPlayer != null)
            {
                lastContentId = Services.ClientState.LocalContentId;

                if (territoryPath != null)
                {
                    long etS = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->ClientTime.EorzeaTime;
                    var et = DateTimeOffset.FromUnixTimeSeconds(etS);
                    locations[lastContentId] = new LocationModel
                    {
                        TerritoryPath = territoryPath,
                        Position = Services.ClientState.LocalPlayer.Position,
                        Rotation = Services.ClientState.LocalPlayer.Rotation,
                        WeatherId = EnvManager.Instance()->ActiveWeather,
                        TimeOffset = (ushort)(et.Hour * 100 + (et.Minute / 60f * 100) % 100),
                        BgmPath = ""
                    };
                }
                else
                {
                    Services.Log.Debug("No territory path?");
                }
            }

        }


        public void Save(ulong localContentId)
        {
            Services.Log.Debug($"Save {localContentId:X16}");
            try
            {
                if (locations.TryGetValue(localContentId, out LocationModel locationModel))
                {
                    File.WriteAllText(
                        Path.Join(Services.PluginInterface.ConfigDirectory.FullName, $"{localContentId:X16}.json"),
                        JsonConvert.SerializeObject(locationModel, Formatting.Indented)
                    );
                }
                else
                {
                    Services.Log.Warning("Failed to save contentId is null");
                }
            }
            catch (Exception e)
            {
                Services.Log.Error(e.Message);
                Services.Log.Error(e.StackTrace ?? "Null Stacktrace");
            }
        }

        private async Task SaveTask()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken.Token);

                if (Services.ClientState.IsLoggedIn)
                {
                    if (Services.ConfigurationService.PeriodicSaving && (DateTime.Now - lastSave).TotalSeconds > Services.ConfigurationService.SavePeriod)
                    {
                        lastSave = DateTime.Now;
                        Save(Services.ClientState.LocalContentId);
                    }
                    else
                    {
                        continue;
                    }

                }
            }
        }

        private void LoadSavedLocations()
        {
            foreach (var file in Services.PluginInterface.ConfigDirectory.EnumerateFiles())
            {
                var match = FileNameRegex.Match(file.Name);
                if (match.Success)
                {
                    ulong contentId = Convert.ToUInt64(match.Groups[1].Value, 16);
                    Services.Log.Debug($"Loading {contentId:X16}");
                    try
                    {
                        locations[contentId] = JsonConvert.DeserializeObject<LocationModel>(File.ReadAllText(file.FullName));
                    }
                    catch (Exception e)
                    {
                        Services.Log.Error(e.Message);
                        Services.Log.Error(e.StackTrace ?? "Null Stacktrace");
                    }
                }
                else
                {
                    Services.Log.Debug($"Unknown file in config directory {file.Name}");

                }
            }
        }

        private void TerritoryChanged(ushort territoryId)
        {
            territoryPath = Services.DataManager.GetExcelSheet<TerritoryType>()!.GetRow(Services.ClientState.TerritoryType)?.Bg.ToString();
            Services.Log.Debug($"TerritoryChanged: {territoryPath}");
        }

        public void OnLogout()
        {
            Save(lastContentId);
        }


        public LocationModel GetLocationModel(ulong contentId)
        {
            if (locations.ContainsKey(contentId))
            {
                return locations[contentId];
            }
            return DefaultLocation;
        }

        public void Dispose()
        {
            Services.Framework.Update -= Tick;
            Services.ClientState.Logout -= OnLogout;
            Services.ClientState.TerritoryChanged -= TerritoryChanged;
            cancellationToken.Cancel();
            cancellationToken.Dispose();
        }
    }
}
