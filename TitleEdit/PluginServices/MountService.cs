using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using TitleEdit.Utility;

namespace TitleEdit.PluginServices
{
    public class MountService : AbstractService
    {
        private const int MountBitmaskArraySize = 37;
        public IReadOnlySet<uint> Mounts => mounts;
        private HashSet<uint> mounts = [];

        private readonly string filePath;
        private byte[]? lastMountBytes;

        public MountService()
        {
            filePath = Path.Join(ConfigurationService.GetBaseConfigDirectory().CreateSubdirectory(PersistanceConsts.DataFolder).FullName, PersistanceConsts.MountsName);
        }

        public override void LoadData()
        {
            LoadMounts();
            mounts.Add(0);
            if (Services.ClientState.LocalPlayer != null)
            {
                RefreshMounts();
            }
        }

        public override void Init()
        {
            Services.Framework.Update += Tick;
        }

        private void Tick(IFramework framework)
        {
            if (Services.ClientState.LocalPlayer != null)
            {
                unsafe
                {
                    var playerState = PlayerState.Instance();
                    if (playerState != null)
                    {
                        lastMountBytes ??= new byte[playerState->UnlockedMountsBitmask.Length];
                        if (!playerState->UnlockedMountsBitmask.SequenceEqual(lastMountBytes))
                        {
                            Services.Log.Debug("Mounts changed!");
                            playerState->UnlockedMountsBitmask.CopyTo(lastMountBytes);
                            RefreshMounts();
                        }
                    }
                }
            }
        }

        private void LoadMounts()
        {
            try
            {
                if (File.Exists(filePath))
                {
                    mounts = JsonConvert.DeserializeObject<HashSet<uint>>(File.ReadAllText(filePath)) ?? [];
                    Services.Log.Debug($"Loaded mounts {mounts.Count}");
                }
            }
            catch (Exception e)
            {
                Services.Log.Error(e, e.Message);
            }
        }


        public void SaveMounts()
        {
            try
            {
                Services.Log.Debug($"Saving mounts");
                Util.WriteAllTextSafe(filePath, JsonConvert.SerializeObject(mounts));
            }
            catch (Exception e)
            {
                Services.Log.Error(e, e.Message);
            }
        }

        public unsafe void RefreshMounts()
        {
            if (Services.ClientState.LocalPlayer != null)
            {
                var playerState = PlayerState.Instance();
                if (playerState != null)
                {
                    foreach (var mount in Services.DataManager.GetExcelSheet<Mount>()!)
                    {
                        if (playerState->IsMountUnlocked(mount.RowId))
                        {
                            mounts.Add(mount.RowId);
                        }
                    }
                }
            }

            SaveMounts();
        }


        public override void Dispose()
        {
            base.Dispose();
            Services.Framework.Update -= Tick;
        }
    }
}
