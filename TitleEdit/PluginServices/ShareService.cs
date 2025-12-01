using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dalamud.Interface.ImGuiNotification;
using Newtonsoft.Json;
using Serilog;
using TitleEdit.Data.Network;
using TitleEdit.Data.Persistence;
using TitleEdit.Utility;

namespace TitleEdit.PluginServices;

public class ShareService : AbstractService
{
#if DEBUG
    // private const string ServiceUrl = "http://localhost:5000";
    private const string ServiceUrl = "https://te.rokas.place";
#elif RELEASE
    private const string ServiceUrl = "https://te.rokas.place";
#endif

    private readonly HttpClient httpClient = new();

    public ConcurrentQueue<PresetModel> ConfirmationQueue { get; init; } = new();

    public ShareService()
    {
        // Limit response size to 10 MB a preset should never reach this
        httpClient.MaxResponseContentBufferSize = 10 * 1024 * 1024;
    }

    public async Task<string> SharePreset(string presetFileName)
    {
        if (Services.PresetService.Presets.TryGetValue(presetFileName, out var preset))
        {
            return await SharePreset(preset);
        }

        throw new("Preset not found");
    }

    public async Task<string> SharePreset(PresetModel preset)
    {
        Services.Log.Info("SharePreset");
        var requestObject = new SharePresetRequest
        {
            Preset = preset
        };

        var request = JsonConvert.SerializeObject(requestObject);

        var response = await httpClient.PostAsync($"{ServiceUrl}/share", new StringContent(request, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<SharePresetResponse>(responseString);
        if (result == null)
        {
            throw new Exception($"Failed to get share code from {responseString}");
        }

        return result.Code;
    }

    public async Task<string> ConfirmImportAsync(PresetModel preset)
    {
        return await Services.Framework.RunOnFrameworkThread(() => ConfirmImport(preset));
    }

    public string ConfirmImport(PresetModel preset)
    {
        var saveResult = Services.PresetService.Save(preset);
        string content = $"{preset.Name}";
        if (!string.IsNullOrEmpty(preset.Author))
        {
            content += $"\nBy {preset.Author}";
        }

        Services.NotificationManager.AddNotification(new()
        {
            MinimizedText = $"Preset imported: {preset.Name}",
            Title = "Preset imported",
            Content = content,
            Type = NotificationType.Success,
            Minimized = false,
            InitialDuration = TimeSpan.FromSeconds(20)
        });
        return saveResult;
    }

    public async Task<PresetModel> GetPreset(string code)
    {
        return await GetPresetByUrl(GetShareFileUrl(code));
    }

    public async Task<PresetModel> GetPresetByUrl(string url)
    {
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync();
        return Services.PresetService.LoadText(responseString);
    }

    private string GetShareFileUrl(string code)
    {
        return $"{ServiceUrl}/share/file/{code}";
    }

    public string GetShareUrl(string code)
    {
        return $"{ServiceUrl}/share/{code}";
    }

    public string? GetCodeFromShareUrl(string url)
    {
        var match = Regex.Match(url.Trim(), $"^{ServiceUrl}/share/([A-Za-z0-9]+)$");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        else
        {
            return null;
        }
    }
}
