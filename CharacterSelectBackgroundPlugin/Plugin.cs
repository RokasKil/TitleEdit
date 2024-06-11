using CharacterSelectBackgroundPlugin.Utility;
using CharacterSelectBackgroundPlugin.Windows;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace CharacterSelectBackgroundPlugin;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/ics";

    private DalamudPluginInterface PluginInterface { get; init; }
    private ICommandManager CommandManager { get; init; }

    public readonly WindowSystem WindowSystem = new("CharacterSelectBackgroundPlugin");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    private ConfigButtonOverlay ConfigButtonOverlay { get; init; }

    public Plugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] ICommandManager commandManager,
        [RequiredVersion("1.0")] ITextureProvider textureProvider)
    {
        PluginInterface = pluginInterface;
        Services.Initialize(PluginInterface, this);
        CommandManager = commandManager;

        ConfigWindow = new();
        MainWindow = new();
        ConfigButtonOverlay = new();

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open Immersive Character Select configuration"
        });

        PluginInterface.UiBuilder.Draw += DrawUI;

        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
#if DEBUG
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
#endif
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
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
