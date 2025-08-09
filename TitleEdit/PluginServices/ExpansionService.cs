using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using System;
using FFXIVClientStructs.Interop;
using TitleEdit.Data.Lobby;
using TitleEdit.Utility;

namespace TitleEdit.PluginServices
{
    public class ExpansionService : AbstractService
    {
        public ExpansionService() { }

        public unsafe bool HasExpansion(TitleScreenExpansion expansion)
        {
            if (expansion == TitleScreenExpansion.ARealmReborn)
            {
                return true;
            }

            var framework = Framework.Instance();
            return IsValidExpansionVersionString(expansion switch
            {
                TitleScreenExpansion.Heavensward => framework->ExVersions.GetValue(0)?.VersionString,
                TitleScreenExpansion.Stormblood => framework->ExVersions.GetValue(1)?.VersionString,
                TitleScreenExpansion.Shadowbringers => framework->ExVersions.GetValue(2)?.VersionString,
                TitleScreenExpansion.Endwalker => framework->ExVersions.GetValue(3)?.VersionString,
                TitleScreenExpansion.Dawntrail => framework->ExVersions.GetValue(4)?.VersionString,
                _ => throw new NotImplementedException()
            });
        }

        private bool IsValidExpansionVersionString(string? versionString) => !versionString.IsNullOrEmpty() && versionString != "none";
    }
}
