using TitleEdit.Utility;

namespace TitleEdit.Data.Lobby
{
    public enum TitleScreenMovie : int
    {
        [EnumTranslation("Unspecified")]
        Unspecified = -1,
        [EnumExpansion(TitleScreenExpansion.ARealmReborn)]
        [EnumTranslation("A Realm Reborn")]
        ARealmReborn = 1,
        [EnumExpansion(TitleScreenExpansion.Heavensward)]
        [EnumTranslation("Heavensward")]
        Heavensward = 4,
        [EnumExpansion(TitleScreenExpansion.Stormblood)]
        [EnumTranslation("Stormblood")]
        Stormblood = 5,
        [EnumExpansion(TitleScreenExpansion.Shadowbringers)]
        [EnumTranslation("Shadowbringers")]
        Shadowbringers = 6,
        [EnumExpansion(TitleScreenExpansion.Endwalker)]
        [EnumTranslation("Endwalker")]
        Endwalker = 7,
        [EnumExpansion(TitleScreenExpansion.Dawntrail)]
        [EnumTranslation("Dawntrail")]
        Dawntrail = 8
    }
}
