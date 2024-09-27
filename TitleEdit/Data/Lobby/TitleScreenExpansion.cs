using TitleEdit.Utility;

namespace TitleEdit.Data.Lobby
{
    public enum TitleScreenExpansion : int
    {
        [EnumExpansion(ARealmReborn)]
        ARealmReborn = 0,
        [EnumExpansion(Heavensward)]
        Heavensward = 1,
        [EnumExpansion(Stormblood)]
        Stormblood = 2,
        [EnumExpansion(Shadowbringers)]
        Shadowbringers = 3,
        [EnumExpansion(Endwalker)]
        Endwalker = 4,
        [EnumExpansion(Dawntrail)]
        Dawntrail = 5
    }
}
