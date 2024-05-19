using CharacterSelectBackgroundPlugin.Data;
using CharacterSelectBackgroundPlugin.Utils;
using Dalamud.Plugin.Services;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Numerics;
using System.Threading;

namespace CharacterSelectBackgroundPlugin.PluginServices
{
    public class LocationService : IDisposable
    {
        public static readonly LocationModel DefaultLocation = new LocationModel
        {
            TerritoryPath = "ffxiv/zon_z1/chr/z1c1/level/z1c1",
            CameraPosition = Vector3.Zero,
            Position = Vector3.Zero,
            WeatherId = 2,
            TimeOffset = 0,
            BgmPath = ""
        };

        private readonly CancellationTokenSource cancellationTokenSource = new();
        private DateTime lastSave = DateTime.Now;
        private string? territoryPath = null;

        public LocationService()
        {
            Services.Framework.Update += Tick;
            Services.ClientState.Logout += Save;
            Services.ClientState.TerritoryChanged += TerritoryChanged;
            TerritoryChanged(Services.ClientState.TerritoryType);
        }

        private unsafe void Tick(IFramework framework)
        {
            if (Services.ClientState.LocalPlayer != null)
            {
                long etS = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->ClientTime.EorzeaTime;

                var et = DateTimeOffset.FromUnixTimeSeconds(etS);

                if (territoryPath != null)
                {
                    Services.ConfigurationService.Locations[Services.ClientState.LocalContentId] = new LocationModel
                    {
                        TerritoryPath = territoryPath,
                        CameraPosition = Services.ClientState.LocalPlayer.Position,
                        Position = Services.ClientState.LocalPlayer.Position,
                        WeatherId = 2,
                        TimeOffset = (ushort)(et.Hour * 100 + (et.Minute / 60f * 100) % 100),
                        BgmPath = ""
                    };
                }
                else
                {
                    Services.Log.Debug("No territory path?");
                }
            }
            if (Services.ConfigurationService.PeriodicSaving && (DateTime.Now - lastSave).TotalSeconds > Services.ConfigurationService.SavePeriod)
            {
                lastSave = DateTime.Now;
                Services.ConfigurationService.Save();
            }
        }

        private void TerritoryChanged(ushort territoryId)
        {
            territoryPath = Services.DataManager.GetExcelSheet<TerritoryType>()!.GetRow(Services.ClientState.TerritoryType)?.Bg.ToString();
            Services.Log.Debug($"TerritoryChanged: {territoryPath}");
        }

        public void Save()
        {
            Services.Log.Debug($"Saving on logout");
            Services.ConfigurationService.Save();
        }

        public LocationModel GetLocationModel(ulong contentId)
        {
            if (Services.ConfigurationService.Locations.ContainsKey(contentId))
            {
                return Services.ConfigurationService.Locations[contentId];
            }
            return DefaultLocation;
        }

        public void Dispose()
        {
            Services.Framework.Update -= Tick;
            Services.ClientState.Logout -= Save;
            Services.ClientState.TerritoryChanged -= TerritoryChanged;
            cancellationTokenSource.Cancel();
        }
    }
}
