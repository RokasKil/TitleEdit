using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using System;
using TitleEdit.Data.Lobby;

namespace TitleEdit.PluginServices
{
    public class ExpansionService : AbstractService
    {
        public ExpansionService()
        {

        }

        public unsafe bool HasExpansion(TitleScreenExpansion expansion)
        {
            var framework = Framework.Instance();
            return IsValidExpansionVersionString(expansion switch
            {
                TitleScreenExpansion.ARealmReborn => framework->GameVersionString,
                TitleScreenExpansion.Heavensward => framework->Ex1VersionString,
                TitleScreenExpansion.Stormblood => framework->Ex2VersionString,
                TitleScreenExpansion.Shadowbringers => framework->Ex3VersionString,
                TitleScreenExpansion.Endwalker => framework->Ex4VersionString,
                TitleScreenExpansion.Dawntrail => framework->Ex5VersionString,
                _ => throw new NotImplementedException()
            });
        }

        private bool IsValidExpansionVersionString(string versionString) => !versionString.IsNullOrEmpty() && versionString != "none";

    }
}
