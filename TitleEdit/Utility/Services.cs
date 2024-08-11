using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System.Collections.Generic;
using TitleEdit.PluginServices;
using TitleEdit.PluginServices.Lobby;
using TitleEdit.PluginServices.Migration;

namespace TitleEdit.Utility
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
        public static CameraService CameraService { get; set; } = null!;
        public static GroupService GroupService { get; set; } = null!;
        public static Plugin Plugin { get; set; } = null!;

        private static List<AbstractService> ServiceList = [];

        public static void ConstructServices(IDalamudPluginInterface pluginInterface, Plugin plugin)
        {
            pluginInterface.Create<Services>();
            Plugin = plugin;
            PluginInterface = pluginInterface;
            try
            {
                ServiceList.Add(LayoutService = new());
                ServiceList.Add(LobbyService = new());
                ServiceList.Add(LocationService = new());
                ServiceList.Add(BgmService = new());
                ServiceList.Add(WeatherService = new());
                ServiceList.Add(PresetService = new());
                ServiceList.Add(CharactersService = new());
                ServiceList.Add(MountService = new());
                ServiceList.Add(BoneService = new());
                ServiceList.Add(MigrationService = new());
                ServiceList.Add(CameraService = new());
                ServiceList.Add(GroupService = new());
            }
            catch
            {
                Dispose();
                throw;
            }
            ConfigurationService = ConfigurationService.Initialize(pluginInterface);
        }

        public static void LoadServiceData()
        {
            try
            {
                ServiceList.ForEach(service => service.LoadData());
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
                ServiceList.ForEach(service => service.Init());
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public static void Dispose()
        {
            ServiceList.ForEach(service => service.Dispose());
        }
    }
}
