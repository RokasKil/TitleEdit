using CharacterSelectBackgroundPlugin.Data.Character;
using CharacterSelectBackgroundPlugin.Data.Layout;
using CharacterSelectBackgroundPlugin.Data.Persistence;
using CharacterSelectBackgroundPlugin.Utility;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CharacterSelectBackgroundPlugin.PluginServices
{
    public class LocationService : AbstractService
    {
        private readonly static Regex FileNameRegex = new("^([A-F0-9]{16})\\.json$", RegexOptions.IgnoreCase);

        public static readonly LocationModel DefaultLocation = new LocationModel
        {
            TerritoryPath = "ffxiv/zon_z1/chr/z1c1/level/z1c1",
            Position = new(0.001f, 0.001f, 0.001f),
            Rotation = 0,
            WeatherId = 2,
            TimeOffset = 0,
            BgmPath = "music/ffxiv/BGM_System_Chara.scd",
            Mount = new()
        };
        //Move this to a different service?
        public unsafe ushort TimeOffset
        {
            get
            {
                long etS = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->ClientTime.EorzeaTime;
                var et = DateTimeOffset.FromUnixTimeSeconds(etS);
                return (ushort)((et.Hour * 100) + Math.Round(et.Minute * 100f / 60f));
            }
        }

        private readonly CancellationTokenSource cancellationToken = new();
        private DateTime lastSave = DateTime.Now;
        public string? TerritoryPath { get; private set; }
        private ConcurrentDictionary<ulong, LocationModel> locations = [];
        private ulong lastContentId;
        private bool refreshLayout = true;
        private uint bgmId;
        private string? bgmPath = null;
        private bool isLoggedIn = false;

        private DirectoryInfo saveDirectory;

        public LocationService()
        {
            saveDirectory = Services.PluginInterface.ConfigDirectory.CreateSubdirectory("characters");
            LoadSavedLocations();
            Services.Framework.Update += Tick;
            Services.ClientState.Logout += Logout;
            Services.ClientState.TerritoryChanged += TerritoryChanged;
            Services.LayoutService.OnLayoutChange += LayoutChanged;
            Services.BgmService.OnBgmChange += BgmChanged;
            unsafe
            {
                Services.LayoutService.OnLayoutInstanceSetActive += LayoutInstanceSetActive;
            }
            TerritoryChanged(Services.ClientState.TerritoryType);
            BgmChanged(Services.BgmService.CurrentSongId);
            Task.Run(SaveTask, cancellationToken.Token);
        }

        private unsafe void Tick(IFramework framework)
        {
            isLoggedIn = Services.ClientState.IsLoggedIn;
            if (!Services.ConfigurationService.TrackPlayerLocation) return;
            if (Services.ClientState.LocalPlayer != null)
            {
                lastContentId = Services.ClientState.LocalContentId;

                if (TerritoryPath != null)
                {
                    var locationModel = locations.ContainsKey(lastContentId) ? locations[lastContentId] : new();
                    locationModel.TerritoryTypeId = Services.ClientState.TerritoryType;
                    locationModel.TerritoryPath = TerritoryPath;
                    locationModel.Position = Services.ClientState.LocalPlayer.Position;
                    locationModel.Rotation = Services.ClientState.LocalPlayer.Rotation;
                    locationModel.WeatherId = Services.WeatherService.WeatherId;
                    var character = ((CharacterExpanded*)Services.ClientState.LocalPlayer.Address);
                    locationModel.MovementMode = character->MovementMode;
                    if (Services.ConfigurationService.SaveMount)
                    {
                        SetMount(ref locationModel, &character->Character);
                    }
                    if (Services.ConfigurationService.SaveTime)
                    {
                        locationModel.TimeOffset = TimeOffset;
                    }
                    else
                    {
                        locationModel.TimeOffset = 0;
                    }
                    if (Services.ConfigurationService.SaveBgm)
                    {
                        locationModel.BgmPath = bgmPath;
                        locationModel.BgmId = bgmId;
                    }
                    else
                    {
                        locationModel.BgmId = 0;
                        locationModel.BgmPath = null;
                    }
                    if (Services.ConfigurationService.SaveLayout && (!Services.Condition.Any(ConditionFlag.BoundByDuty) || Services.ConfigurationService.SaveLayoutInInstance))
                    {

                        if (Services.LayoutService.LayoutInitialized)
                        {
                            if (refreshLayout)
                            {
                                SetLayout(ref locationModel);
                            }

                            refreshLayout = false;
                        }
                    }
                    else
                    {
                        locationModel.Active.Clear();
                        locationModel.Inactive.Clear();
                        locationModel.Festivals = [];
                    }
                    locations[lastContentId] = locationModel;
                }
                else
                {
                    Services.Log.Debug("No territory path?");
                }
            }

        }

        public unsafe void SetMount(ref LocationModel locationModel, Character* character)
        {
            locationModel.Mount.MountId = character->Mount.MountId;
            if (character->Mount.MountId != 0)
            {
                var mountCharacter = character->Mount.MountObject;
                locationModel.Mount.BuddyModelTop = mountCharacter->DrawData.Head.Value;
                locationModel.Mount.BuddyModelBody = mountCharacter->DrawData.Top.Value;
                locationModel.Mount.BuddyModelLegs = mountCharacter->DrawData.Feet.Value;
                locationModel.Mount.BuddyStain = mountCharacter->DrawData.Legs.Stain;
            }
        }


        private void TerritoryChanged(ushort territoryId)
        {
            TerritoryPath = Services.DataManager.GetExcelSheet<TerritoryType>()!.GetRow(Services.ClientState.TerritoryType)?.Bg.ToString();
            Services.Log.Debug($"TerritoryChanged: {TerritoryPath}");
        }

        public void Logout()
        {
            Save(lastContentId);
        }

        private void LayoutChanged()
        {
            refreshLayout = true;
        }

        private void BgmChanged(uint songId)
        {
            bgmId = songId;
            bgmPath = Services.BgmService.BgmPaths[bgmId];
            Services.Log.Debug($"BgmChanged {songId} {bgmPath}");
        }

        private unsafe void LayoutInstanceSetActive(ILayoutInstance* layout, bool active)
        {
            if (!refreshLayout && Services.ClientState.LocalPlayer != null && locations.TryGetValue(Services.ClientState.LocalContentId, out var locationModel))
            {
                var inActive = locationModel.Active.Contains(layout->UUID);
                var inInactive = locationModel.Inactive.Contains(layout->UUID);
                if ((inActive || inInactive) && ((active && !inActive) || (!active && !inInactive)))
                {
                    refreshLayout = true;

                    Services.Log.Debug($"[LayoutSetActiveDetour] refreshLayoutrefreshLayoutrefreshLayoutrefreshLayoutrefreshLayoutrefreshLayoutrefreshLayoutrefreshLayoutrefreshLayoutrefreshLayout");
                }
            }
        }


        public unsafe void SetLayout(ref LocationModel locationModel)
        {

            HashSet<ulong> active = [];
            HashSet<ulong> inactive = [];
            Dictionary<ulong, short> vfxTriggerIndexes = [];
            Services.LayoutService.ForEachInstance(instance =>
            {
                if (instance.Value->isActive)
                {
                    active.Add(instance.Value->UUID);
                }
                else
                {
                    inactive.Add(instance.Value->UUID);
                }
                if (instance.Value->Id.Type == InstanceType.Vfx)
                {
                    var vfxInstance = (VfxLayoutInstance*)instance.Value;
                    Services.Log.Debug($"{instance.Value->UUID} is vfx with triggerIndex {vfxInstance->VfxTriggerIndex}");
                    if (vfxInstance->VfxTriggerIndex != -1)
                    {
                        vfxTriggerIndexes[instance.Value->UUID] = vfxInstance->VfxTriggerIndex;
                    }
                }
            });
            locationModel.Active = active;
            locationModel.Inactive = inactive;
            locationModel.VfxTriggerIndexes = vfxTriggerIndexes;
        }


        public void Save(ulong localContentId)
        {
            //Services.Log.Debug($"Save {localContentId:X16}");
            //var sw = Stopwatch.StartNew();
            try
            {
                if (locations.TryGetValue(localContentId, out LocationModel locationModel))
                {

                    Util.WriteAllTextSafe(
                        Path.Join(saveDirectory.FullName, $"{localContentId:X16}.json"),
                        JsonConvert.SerializeObject(locationModel)
                    );
                }
                else
                {
                    Services.Log.Warning("Failed to save contentId is null");
                }
            }
            catch (Exception e)
            {
                Services.Log.Error(e, e.Message);
            }
            //sw.Stop();

            //Services.Log.Debug($"Save took {sw.Elapsed.TotalMilliseconds} ms");
        }

        private async Task SaveTask()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken.Token);

                if ((DateTime.Now - lastSave).TotalSeconds > Services.ConfigurationService.SavePeriod)
                {
                    if (Services.ConfigurationService.SaveLayout && Services.ConfigurationService.PeriodicSaving && isLoggedIn)
                    {
                        Save(lastContentId);
                    }
                    lastSave = DateTime.Now;
                }
                else
                {
                    continue;
                }

            }

        }

        private void LoadSavedLocations()
        {
            foreach (var file in saveDirectory.EnumerateFiles())
            {
                var match = FileNameRegex.Match(file.Name);
                if (match.Success)
                {
                    ulong contentId = Convert.ToUInt64(match.Groups[1].Value, 16);
                    Services.Log.Debug($"Loading {contentId:X16}");
                    try
                    {
                        var location = JsonConvert.DeserializeObject<LocationModel>(File.ReadAllText(file.FullName));
                        location.Active ??= [];
                        location.Inactive ??= [];
                        location.VfxTriggerIndexes ??= [];
                        location.Festivals ??= [0, 0, 0, 0];
                        Validate(location);
                        locations[contentId] = location;
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

        public LocationModel GetLocationModel(ulong contentId)
        {
            if (locations.TryGetValue(contentId, out var locationModel))
            {
                return locationModel;
            }
            return DefaultLocation;
        }

        public void Validate(LocationModel locationModel)
        {
            if (locationModel.Version != 1)
            {
                throw new("Location Version is not valid");
            }
            if (!Services.DataManager.FileExists($"bg/{locationModel.TerritoryPath}.lvb"))
            {
                throw new("Game scene file not found"); ;
            }
            if (!locationModel.BgmPath.IsNullOrEmpty() && !Services.DataManager.FileExists(locationModel.BgmPath))
            {
                throw new("BGM file not found"); ;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            Services.Framework.Update -= Tick;
            Services.ClientState.Logout -= Logout;
            Services.ClientState.TerritoryChanged -= TerritoryChanged;
            Services.LayoutService.OnLayoutChange -= LayoutChanged;
            Services.BgmService.OnBgmChange -= BgmChanged;
            unsafe
            {
                Services.LayoutService.OnLayoutInstanceSetActive -= LayoutInstanceSetActive;
            }
            cancellationToken.Cancel();
            cancellationToken.Dispose();
        }
    }
}
