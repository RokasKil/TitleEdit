using CharacterSelectBackgroundPlugin.Utility;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace CharacterSelectBackgroundPlugin.PluginServices
{
    public class MountService : AbstractService
    {

        private const int MountBitmaskArraySize = 37;
        public IReadOnlySet<uint> Mounts => mounts;
        private HashSet<uint> mounts = [];

        private readonly string filePath;
        private readonly byte[] lastMountBytes = new byte[MountBitmaskArraySize];
        public MountService()
        {
            filePath = Path.Join(Services.PluginInterface.ConfigDirectory.CreateSubdirectory("data").FullName, "mounts.json");
            LoadMounts();
            mounts.Add(0);
            if (Services.ClientState.LocalPlayer != null)
            {
                RefreshMounts();
            }
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
