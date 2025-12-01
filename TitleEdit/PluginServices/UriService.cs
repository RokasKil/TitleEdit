using System;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Networking.Pipes;
using Dalamud.Utility;
using TitleEdit.PluginServices.Preset;
using TitleEdit.Utility;

namespace TitleEdit.PluginServices;

public class UriService : AbstractService
{
    [Experimental("DAL_RPC")]
    public override void Init()
    {
        Services.PluginLinkHandler.OnUriReceived += OnUriReceived;
    }

    private void OnUriReceived(DalamudUri uri)
    {
        Services.Log.Debug($"Received uri {uri.ToString()}");
        var command = uri.Segments.GetValue(2)?.ToString();
        if (command == "importShared")
        {
            HandleImportShared(uri.QueryParams);
        }
        else
        {
            Services.Log.Error($"Received invalid uri {uri}");
        }
    }

    private void HandleImportShared(NameValueCollection queryParams)
    {
        var code = queryParams.GetValues("code")?.FirstOrDefault();
        if (code == null)
        {
            Services.Log.Error($"[HandleImportShared] Missing share code");
        }
        else
        {
            Services.Log.Info($"[HandleImportShared] Importing {code}");

            Task.Run(() => Services.ShareService.GetPreset(code).ContinueWith(async result =>
            {
                try
                {
                    var preset = result.Result;
                    if (!Services.ConfigurationService.PromptForUrlImport)
                    {
                        await Services.ShareService.ConfirmImportAsync(preset);
                    }
                    else
                    {
                        Services.ShareService.ConfirmationQueue.Enqueue(preset);
                    }
                }
                catch (Exception ex)
                {
                    Services.Log.Error(ex, "Failed to retrieve preset");
                    Services.NotificationManager.AddNotification(new()
                    {
                        Content = "Failed to retrieve preset",
                        Type = NotificationType.Error,
                        Minimized = true
                    });
                }
            }));
        }
    }
}
