using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using TitleEdit.Utility;
using TitleEdit.Windows;

namespace TitleEdit;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/titleedit";
    private const string CommandNameAlias = "/te";

    public readonly WindowSystem WindowSystem = new("TitleEdit");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    private ConfigButtonOverlay ConfigButtonOverlay { get; init; }

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        Services.ConstructServices(pluginInterface, this);
        Services.LoadServiceData();
        if (!Services.ConfigurationService.SettingsMigrated)
        {
            Services.MigrationService.MigrateTitleScreenV2Presets();
            Services.MigrationService.MigrateTitleScreenV2Configuration();
        }
        Services.InitServices();

        ConfigWindow = new();
        MainWindow = new();
        ConfigButtonOverlay = new();

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        Services.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open Title Edit configuration"
        });
        Services.CommandManager.AddHandler(CommandNameAlias, new CommandInfo(OnCommand)
        {
            ShowInHelp = false
        });

        Services.PluginInterface.UiBuilder.Draw += DrawUI;

        Services.PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
#if DEBUG
        Services.PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
#endif
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        Services.CommandManager.RemoveHandler(CommandName);
        Services.CommandManager.RemoveHandler(CommandNameAlias);
        Services.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our main ui
        ToggleConfigUI();
    }

    private void DrawUI()
    {
        ConfigButtonOverlay.Draw();
        WindowSystem.Draw();
    }

    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
}
