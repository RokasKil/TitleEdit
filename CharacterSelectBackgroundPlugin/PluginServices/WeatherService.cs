using CharacterSelectBackgroundPlugin.Utils;
using System;
using System.Runtime.InteropServices;

namespace CharacterSelectBackgroundPlugin.PluginServices
{
    public unsafe class WeatherService : IDisposable
    {


        public IntPtr WeatherPtr => Marshal.ReadIntPtr(WeatherPtrBase) + 0x27;
        public IntPtr WeatherPtrBase { get; private set; }
        public WeatherService()
        {
            WeatherPtrBase = Services.SigScanner.GetStaticAddressFromSig("48 8B 05 ?? ?? ?? ?? 48 8B D9 0F 29 7C 24 ?? 41 8B FF");
        }

        public void Dispose()
        {
        }
    }
}
