using TitleEdit.Data.Lobby;
using TitleEdit.Data.Persistence;

namespace TitleEdit.Data.Msq
{
    public static class MsqVanillaPresets
    {
        public static string PresetPath(TitleScreenExpansion expansion) => expansion switch
        {
            TitleScreenExpansion.Heavensward => "?/Heavensward.json",
            TitleScreenExpansion.Stormblood => "?/Stormblood.json",
            TitleScreenExpansion.Shadowbringers => "?/Shadowbringers.json",
            TitleScreenExpansion.Endwalker => "?/Endwalker.json",
            TitleScreenExpansion.Dawntrail => "?/Dawntrail.json",
            _ => "?/ARealmReborn.json"
        };

        public static TitleScreenLogo Logo(TitleScreenExpansion expansion) => expansion switch
        {
            TitleScreenExpansion.Heavensward => TitleScreenLogo.Heavensward,
            TitleScreenExpansion.Stormblood => TitleScreenLogo.Stormblood,
            TitleScreenExpansion.Shadowbringers => TitleScreenLogo.Shadowbringers,
            TitleScreenExpansion.Endwalker => TitleScreenLogo.Endwalker,
            TitleScreenExpansion.Dawntrail => TitleScreenLogo.Dawntrail,
            _ => TitleScreenLogo.ARealmReborn
        };

        public static TitleScreenMovie Movie(TitleScreenExpansion expansion) => expansion switch
        {
            TitleScreenExpansion.Heavensward => TitleScreenMovie.Heavensward,
            TitleScreenExpansion.Stormblood => TitleScreenMovie.Stormblood,
            TitleScreenExpansion.Shadowbringers => TitleScreenMovie.Shadowbringers,
            TitleScreenExpansion.Endwalker => TitleScreenMovie.Endwalker,
            TitleScreenExpansion.Dawntrail => TitleScreenMovie.Dawntrail,
            _ => TitleScreenMovie.ARealmReborn
        };
    }

    public static class MsqGateQuests
    {
        // First MSQ of each expansion. Resolved against the Quest sheet by name at runtime.
        public static readonly (TitleScreenExpansion Expansion, string[] Names)[] Gates =
        [
            (TitleScreenExpansion.Dawntrail, ["A New World to Explore"]),
            (TitleScreenExpansion.Endwalker, ["The Next Hunt", "Old Sharlayan, New to You"]),
            (TitleScreenExpansion.Shadowbringers, ["The Syrcus Trench"]),
            (TitleScreenExpansion.Stormblood, ["Beyond the Great Wall"]),
            (TitleScreenExpansion.Heavensward, ["Coming to Ishgard"]),
        ];
    }
}
