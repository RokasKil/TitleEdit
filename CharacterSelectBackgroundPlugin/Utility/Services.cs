using CharacterSelectBackgroundPlugin.PluginServices;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace CharacterSelectBackgroundPlugin.Utility
{
    public class Services
    {
        [PluginService] public static IChatGui ChatGui { get; set; } = null!;
        [PluginService] public static IObjectTable ObjectTable { get; set; } = null!;
        [PluginService] public static IClientState ClientState { get; set; } = null!;
        [PluginService] public static ISigScanner SigScanner { get; set; } = null!;
        [PluginService] public static IDataManager DataManager { get; set; } = null!;
        [PluginService] public static ICondition Condition { get; set; } = null!;
        [PluginService] public static IGameGui GameGui { get; set; } = null!;
        [PluginService] public static ICommandManager CommandManager { get; set; } = null!;
        [PluginService] public static IPluginLog Log { get; set; } = null!;
        [PluginService] public static IGameInteropProvider GameInteropProvider { get; set; } = null!;
        [PluginService] public static ITargetManager TargetManager { get; set; } = null!;
        [PluginService] public static IFramework Framework { get; set; } = null!;
        public static DalamudPluginInterface PluginInterface { get; set; } = null!;
        public static ConfigurationService ConfigurationService { get; set; } = null!;
        public static LayoutService LayoutService { get; set; } = null!;
        public static LobbyService LobbyService { get; set; } = null!;
        public static LocationService LocationService { get; set; } = null!;
        public static BgmService BgmService { get; set; } = null!;
        public static WeatherService WeatherService { get; set; } = null!;
        public static PresetService PresetService { get; set; } = null!;
        public static CharactersService CharactersService { get; set; } = null!;
        public static MountService MountService { get; set; } = null!;
        public static BoneService BoneService { get; set; } = null!;

        public static void Initialize(DalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<Services>();
            PluginInterface = pluginInterface;
            ConfigurationService = pluginInterface.GetPluginConfig() as ConfigurationService ?? new();
            ConfigurationService.Initialize(pluginInterface);
            //Rework this into a 2 stage initalization cause some stuff depends on each other 
            try
            {
                LayoutService = new();
                BgmService = new();
                LocationService = new();
                WeatherService = new();
                PresetService = new();
                LobbyService = new();
                CharactersService = new();
                MountService = new();
                BoneService = new();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public static void Dispose()
        {
            LayoutService?.Dispose();
            BgmService?.Dispose();
            LocationService?.Dispose();
            WeatherService?.Dispose();
            PresetService?.Dispose();
            LobbyService?.Dispose();
            CharactersService?.Dispose();
            MountService?.Dispose();
            BoneService?.Dispose();
        }
    }
}
