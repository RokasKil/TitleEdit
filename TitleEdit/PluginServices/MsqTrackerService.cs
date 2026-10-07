using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using TitleEdit.Data.Lobby;
using TitleEdit.Data.Msq;
using TitleEdit.Data.Persistence;
using TitleEdit.Utility;
using Dalamud.Game.Config;

namespace TitleEdit.PluginServices
{
    /// <summary>
    /// Tracks the currently logged-in character's MSQ expansion and maps it to
    /// the vanilla title screen + idle movie. Unlike patch 7.1's built-in
    /// "Title Screen Displayed on Launch", this:
    ///   - follows the character you actually just played, not the furthest on the account
    ///   - ignores the Free Trial lock that forces a single title
    ///   - is persisted immediately so returning to the title screen (logout, no relaunch) already shows it
    /// </summary>
    public unsafe class MsqTrackerService : AbstractService
    {
        private readonly Dictionary<string, uint> gateQuestIds = new(StringComparer.OrdinalIgnoreCase);
        private bool sheetResolved;

        // Debounce rapid Login/Logout sequences that happen during world / datacenter travel.
        // Without this the expensive DetectExpansion path (and cfg.Save) can run many times
        // in a short window while native quest / player state is half-destroyed, producing
        // the "errors every second" spam.
        private DateTime lastRefreshUtc = DateTime.MinValue;
        private static readonly TimeSpan MinRefreshInterval = TimeSpan.FromSeconds(2.5);

        public TitleScreenExpansion CurrentExpansion { get; private set; } = TitleScreenExpansion.ARealmReborn;
        public string CurrentGateQuest { get; private set; } = "A Realm Reborn";
        public ulong CurrentContentId { get; private set; }
        public string CurrentCharacterName { get; private set; } = "";

        public override void Init()
        {
            ResolveQuestSheet();
            Services.ClientState.Login += OnLogin;
            Services.ClientState.Logout += OnLogout;
            // No Framework.Update poll. MSQ only changes on accept/complete of a gate
            // quest; login + logout already snapshot the state. A crash simply leaves
            // the previous successful snapshot, which the next login corrects.
            if (Services.ClientState.IsLoggedIn)
            {
                RefreshFromClient("init-logged-in");
            }
        }

        public override void Dispose()
        {
            Services.ClientState.Login -= OnLogin;
            Services.ClientState.Logout -= OnLogout;
            base.Dispose();
        }

        public TitleScreenExpansion GetExpansionForNextLogin()
        {
            var cfg = Services.ConfigurationService;
            var key = cfg.LastLoggedContentId.ToString("X");
            if (cfg.LastLoggedContentId != 0 &&
                cfg.CharacterMsqExpansions.TryGetValue(key, out var stored))
            {
                return ClampToAvailable(stored);
            }

            return ClampToAvailable(CurrentExpansion);
        }

        public string GetVanillaPresetPath() => MsqVanillaPresets.PresetPath(GetExpansionForNextLogin());

        /// <param name="requireLoggedIn">
        /// When true (default), bail if ClientState reports not logged in.
        /// Logout passes false so we can still read QuestManager while the
        /// character data is tearing down and write the final snapshot.
        /// </param>
        public void RefreshFromClient(string reason, bool requireLoggedIn = true)
        {
            // Hard outer guard: never let an exception from this path take down the plugin
            // or flood the log during travel transitions.
            try
            {
                if (requireLoggedIn && !Services.ClientState.IsLoggedIn)
                {
                    return;
                }

                // Debounce: world/DC travel produces rapid successive Login + Logout events
                // while native structures are unstable. Skip if we just ran a short time ago
                // (except for the very first init call).
                var now = DateTime.UtcNow;
                if (reason != "init-logged-in" && (now - lastRefreshUtc) < MinRefreshInterval)
                {
                    return;
                }

                // Prefer live PlayerState; fall back to whatever we already knew this session
                // so a logout that races IsLoggedIn still has a usable Content ID / name.
                ulong contentId = 0;
                string name = "";
                try
                {
                    // Extra safety: PlayerState can be in a bad intermediate state during travel.
                    if (Services.PlayerState != null)
                    {
                        contentId = Services.PlayerState.ContentId;
                        name = Services.PlayerState.CharacterName ?? "";
                    }
                }
                catch
                {
                    // Swallow – we will fall back to previously known values.
                }

                if (contentId != 0)
                {
                    CurrentContentId = contentId;
                }

                if (!string.IsNullOrEmpty(name))
                {
                    CurrentCharacterName = name;
                }

                if (CurrentContentId == 0)
                {
                    return;
                }

                // On pure logout during travel we often already have a good expansion for this
                // ContentId. Prefer the cheap cached path over a full DetectExpansion that
                // touches QuestManager while it is being torn down.
                var cfg = Services.ConfigurationService;
                var key = CurrentContentId.ToString("X");
                if (!requireLoggedIn &&
                    cfg.CharacterMsqExpansions.TryGetValue(key, out var cached) &&
                    cached != TitleScreenExpansion.ARealmReborn)
                {
                    // Still update the "last logged" pointers so the next title screen is correct,
                    // but skip the expensive / fragile quest scan.
                    CurrentExpansion = ClampToAvailable(cached);
                    Persist(reason, changed: false);
                    lastRefreshUtc = now;
                    return;
                }

                var (expansion, gate) = DetectExpansion();
                var changed = expansion != CurrentExpansion || CurrentContentId != cfg.LastLoggedContentId;
                CurrentExpansion = expansion;
                CurrentGateQuest = gate;
                Persist(reason, changed);
                lastRefreshUtc = now;
            }
            catch (Exception ex)
            {
                // Never let travel-time failures become a continuous error spam.
                Services.Log.Warning(ex, $"[MsqTracker] RefreshFromClient(\"{reason}\") failed – ignoring (common during world/DC travel)");
            }
        }

        private void Persist(string reason, bool changed)
        {
            try
            {
                var cfg = Services.ConfigurationService;
                var key = CurrentContentId.ToString("X");
                cfg.LastLoggedContentId = CurrentContentId;
                cfg.LastLoggedCharacterName = CurrentCharacterName;
                cfg.CharacterMsqExpansions[key] = CurrentExpansion;
                // Keep writing the two historically present fields for config compatibility
                // even though nothing currently reads them back.
                cfg.CharacterMsqGateQuests[key] = CurrentGateQuest;
                cfg.LastMsqObservedAt = DateTime.UtcNow;
                cfg.Save();
                Services.Log.Info($"[MsqTracker] {reason}: {CurrentCharacterName} ({CurrentContentId:X}) → {CurrentExpansion} via {CurrentGateQuest}");
                ApplyGameTitleScreenSetting(CurrentExpansion);
                if (changed && cfg.ApplyMsqTitleImmediately && cfg.TitleDisplayTypeOption.Type == TitleDisplayType.MsqProgress)
                {
                    ApplyToLobbyIfOnTitle();
                }
            }
            catch (Exception ex)
            {
                Services.Log.Warning(ex, $"[MsqTracker] Persist(\"{reason}\") failed");
            }
        }

        public void ApplyToLobbyIfOnTitle()
        {
            try
            {
                if (Services.LobbyService.CanReloadTitleScreen)
                {
                    Services.LobbyService.ReloadTitleScreen();
                }
            }
            catch (Exception ex)
            {
                Services.Log.Warning(ex, "[MsqTracker] Could not reload title screen immediately");
            }
        }
        private void ApplyGameTitleScreenSetting(TitleScreenExpansion expansion)
        {
            if (!Services.ConfigurationService.ReflectMsqOntoGameSettings)
                return;

            // Confirmed: 0 ARR, 1 HW, 2 SB, 3 ShB. Inferred: 4 EW, 5 DT
            // (matches TitleScreenExpansion enum values).
            var value = (uint)ClampToAvailable(expansion);

            try
            {
                if (Services.GameConfig.TryGet(SystemConfigOption.TitleScreenType, out uint current)
                    && current == value)
                    return;

                Services.GameConfig.Set(SystemConfigOption.TitleScreenType, value);
                Services.Log.Info($"[MsqTracker] Set TitleScreenType → {value} ({expansion})");
            }
            catch (Exception ex)
            {
                Services.Log.Warning(ex, "[MsqTracker] Failed to set TitleScreenType");
            }
        }
        private (TitleScreenExpansion Expansion, string Gate) DetectExpansion()
        {
            try
            {
                foreach (var (expansion, names) in MsqGateQuests.Gates)
                {
                    foreach (var name in names)
                    {
                        if (!gateQuestIds.TryGetValue(name, out var id) || id == 0)
                        {
                            continue;
                        }

                        if (IsQuestReached(id))
                        {
                            return (expansion, name);
                        }
                    }
                }

                var scanned = ScanHighestMsqExpansion();
                if (scanned != TitleScreenExpansion.ARealmReborn)
                {
                    return (scanned, $"MSQ sheet ({scanned})");
                }
            }
            catch (Exception ex)
            {
                Services.Log.Warning(ex, "[MsqTracker] DetectExpansion failed – falling back to ARR");
            }

            return (TitleScreenExpansion.ARealmReborn, "A Realm Reborn");
        }

        private TitleScreenExpansion ScanHighestMsqExpansion()
        {
            var highest = TitleScreenExpansion.ARealmReborn;
            try
            {
                var sheet = Services.DataManager.GetExcelSheet<Quest>();
                if (sheet == null)
                {
                    return highest;
                }

                foreach (var quest in sheet)
                {
                    if (quest.RowId == 0 || !IsQuestReached(quest.RowId))
                    {
                        continue;
                    }

                    var genre = "";
                    var category = "";
                    try
                    {
                        genre = quest.JournalGenre.ValueNullable?.Name.ExtractText() ?? "";
                        category = quest.JournalGenre.ValueNullable?.JournalCategory.ValueNullable?.Name.ExtractText() ?? "";
                    }
                    catch
                    {
                        continue;
                    }

                    var mapped = MapJournalToExpansion(genre, category);
                    if (mapped > highest)
                    {
                        highest = mapped;
                    }
                }
            }
            catch (Exception ex)
            {
                Services.Log.Warning(ex, "[MsqTracker] Excel MSQ scan failed");
            }

            return highest;
        }

        private static TitleScreenExpansion MapJournalToExpansion(string genre, string category)
        {
            var text = $"{genre} {category}";
            if (text.Contains("Dawntrail", StringComparison.OrdinalIgnoreCase))
                return TitleScreenExpansion.Dawntrail;
            if (text.Contains("Endwalker", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("Growing Light", StringComparison.OrdinalIgnoreCase))
                return TitleScreenExpansion.Endwalker;
            if (text.Contains("Shadowbringers", StringComparison.OrdinalIgnoreCase))
                return TitleScreenExpansion.Shadowbringers;
            if (text.Contains("Stormblood", StringComparison.OrdinalIgnoreCase))
                return TitleScreenExpansion.Stormblood;
            if (text.Contains("Heavensward", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("Dragonsong", StringComparison.OrdinalIgnoreCase))
                return TitleScreenExpansion.Heavensward;
            return TitleScreenExpansion.ARealmReborn;
        }

        private static bool IsQuestReached(uint questId)
        {
            if (questId == 0)
            {
                return false;
            }

            try
            {
                if (QuestManager.IsQuestComplete(questId))
                {
                    return true;
                }

                var qm = QuestManager.Instance();
                if (qm == null)
                {
                    return false;
                }

                var acceptedId = questId > 0xFFFF ? (ushort)(questId - 0x10000) : (ushort)questId;
                return qm->IsQuestAccepted(acceptedId);
            }
            catch
            {
                // QuestManager can be in an invalid state during logout / travel.
                return false;
            }
        }

        private TitleScreenExpansion ClampToAvailable(TitleScreenExpansion expansion)
        {
            if (Services.ConfigurationService.BypassFreeTrialTitleLock)
            {
                // Still cannot play movies that are not on disk. Walk down until files exist,
                // but never force ARR purely because the account is a Free Trial.
                var candidate = expansion;
                while (candidate > TitleScreenExpansion.ARealmReborn &&
                       !Services.ExpansionService.HasExpansion(candidate))
                {
                    candidate--;
                }

                return candidate;
            }

            return expansion.IsInAvailableExpansion() ? expansion : TitleScreenExpansion.ARealmReborn;
        }

        private void ResolveQuestSheet()
        {
            if (sheetResolved)
            {
                return;
            }

            try
            {
                var sheet = Services.DataManager.GetExcelSheet<Quest>();
                if (sheet == null)
                {
                    return;
                }

                foreach (var quest in sheet)
                {
                    var name = quest.Name.ExtractText();
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    foreach (var (_, names) in MsqGateQuests.Gates)
                    {
                        foreach (var gate in names)
                        {
                            if (name.Equals(gate, StringComparison.OrdinalIgnoreCase))
                            {
                                gateQuestIds[gate] = quest.RowId;
                            }
                        }
                    }
                }

                sheetResolved = true;
                Services.Log.Info($"[MsqTracker] Resolved {gateQuestIds.Count} MSQ gate quests from Excel");
            }
            catch (Exception ex)
            {
                Services.Log.Warning(ex, "[MsqTracker] Failed to resolve Quest sheet; will retry");
            }
        }

        private void OnLogin()
        {
            try
            {
                ResolveQuestSheet();
                RefreshFromClient("login");
            }
            catch (Exception ex)
            {
                Services.Log.Warning(ex, "[MsqTracker] OnLogin failed");
            }
        }

        private void OnLogout(int _type, int _code)
        {
            try
            {
                // Final snapshot for this session (progress made while logged in).
                // requireLoggedIn: false — ClientState.IsLoggedIn may already be false
                // by the time this event runs, but QuestManager is usually still readable.
                RefreshFromClient("logout", requireLoggedIn: false);

                if (Services.ConfigurationService.ApplyMsqTitleImmediately &&
                    Services.ConfigurationService.TitleDisplayTypeOption.Type == TitleDisplayType.MsqProgress)
                {
                    // TitleEdit loads at the lobby. Reloading here makes the matching
                    // title/movie appear as soon as you hit the title screen — no ffxiv_dx11 relaunch.
                    ApplyToLobbyIfOnTitle();
                }
            }
            catch (Exception ex)
            {
                Services.Log.Warning(ex, "[MsqTracker] OnLogout failed");
            }
        }
    }
}
