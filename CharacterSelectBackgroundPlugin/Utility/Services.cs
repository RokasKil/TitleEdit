using CharacterSelectBackgroundPlugin.PluginServices;
using CharacterSelectBackgroundPlugin.PluginServices.Lobby;
using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace CharacterSelectBackgroundPlugin.Utility
{
    public class Services
    {
        [PluginService] public static IClientState ClientState { get; set; } = null!;
        [PluginService] public static ISigScanner SigScanner { get; set; } = null!;
        [PluginService] public static IDataManager DataManager { get; set; } = null!;
        [PluginService] public static ICondition Condition { get; set; } = null!;
        [PluginService] public static IGameGui GameGui { get; set; } = null!;
        [PluginService] public static IPluginLog Log { get; set; } = null!;
        [PluginService] public static IGameInteropProvider GameInteropProvider { get; set; } = null!;
        [PluginService] public static IFramework Framework { get; set; } = null!;
        [PluginService] public static ICommandManager CommandManager { get; set; } = null!;
        public static IDalamudPluginInterface PluginInterface { get; set; } = null!;
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
        public static MigrationService MigrationService { get; set; } = null!;
        public static Plugin Plugin { get; set; } = null!;

        public static void ConstructServices(IDalamudPluginInterface pluginInterface, Plugin plugin)
        {
            pluginInterface.Create<Services>();
            Plugin = plugin;
            PluginInterface = pluginInterface;
            ConfigurationService = pluginInterface.GetPluginConfig() as ConfigurationService ?? new();
            ConfigurationService.Initialize(pluginInterface);
            try
            {
                LayoutService = new();
                LobbyService = new();
                LocationService = new();
                BgmService = new();
                WeatherService = new();
                PresetService = new();
                CharactersService = new();
                MountService = new();
                BoneService = new();
                MigrationService = new();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public static void LoadServiceData()
        {
            try
            {
                LayoutService.LoadData();
                LobbyService.LoadData();
                LocationService.LoadData();
                BgmService.LoadData();
                WeatherService.LoadData();
                PresetService.LoadData();
                CharactersService.LoadData();
                MountService.LoadData();
                BoneService.LoadData();
                MigrationService.LoadData();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public static void InitServices()
        {
            try
            {
                LayoutService.Init();
                LobbyService.Init();
                LocationService.Init();
                BgmService.Init();
                WeatherService.Init();
                PresetService.Init();
                CharactersService.Init();
                MountService.Init();
                BoneService.Init();
                MigrationService.Init();
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
            MountService?.Dispose();
            BoneService?.Dispose();
            LobbyService?.Dispose();
            CharactersService?.Dispose();
        }
    }
}
