using CharacterSelectBackgroundPlugin.Data.Bgm;
using CharacterSelectBackgroundPlugin.Utils;
using Dalamud.Plugin.Services;
using System;
using System.Runtime.InteropServices;

namespace CharacterSelectBackgroundPlugin.PluginServices
{
    //Taken from https://github.com/lmcintyre/OrchestrionPlugin/blob/main/Orchestrion/BgmSystem
    public class BgmService : IDisposable
    {
        private nint baseAddress;
        private const int SceneCount = 12;
        public nint BgmSceneManager
        {
            get
            {
                var baseObject = Marshal.ReadIntPtr(baseAddress);

                return baseObject;
            }
        }

        public nint BgmSceneList
        {
            get
            {
                var baseObject = Marshal.ReadIntPtr(baseAddress);

                // I've never seen this happen, but the game checks for it in a number of places
                return baseObject == nint.Zero ? nint.Zero : Marshal.ReadIntPtr(baseObject + 0xC0);
            }
        }

        public int CurrentSongId { get; private set; }


        public delegate void BgmChangedDelegate(int songId);

        public unsafe event BgmChangedDelegate? OnBgmChange;

        public BgmService()
        {
            baseAddress = Services.SigScanner.GetStaticAddressFromSig("48 8B 05 ?? ?? ?? ?? 48 85 C0 74 37 83 78 08 04");
            Services.Framework.Update += Tick;

        }

        private unsafe void Tick(IFramework framework)
        {

            var bgms = (BgmScene*)BgmSceneList.ToPointer();

            for (int sceneIdx = 0; sceneIdx < SceneCount; sceneIdx++)
            {
                if (bgms[sceneIdx].BgmReference == 0) continue;

                if (bgms[sceneIdx].BgmId != 0 && bgms[sceneIdx].BgmId != 9999)
                {
                    if (CurrentSongId != bgms[sceneIdx].BgmId)
                    {
                        SongChanged(bgms[sceneIdx].BgmId);
                    }
                    break;
                }
            }

        }

        private void SongChanged(int songId)
        {
            Services.Log.Debug($"SongChanged {songId}");
            CurrentSongId = songId;
            OnBgmChange?.Invoke(CurrentSongId);
        }

        public void Dispose()
        {
            Services.Framework.Update -= Tick;
        }
    }
}
