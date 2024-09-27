using TitleEdit.Data.Lobby;
using TitleEdit.Utility;

namespace TitleEdit.Data.Persistence
{
    public enum TitleScreenLogo
    {
        [EnumTranslation("None")]
        None,
        [EnumTranslation("Unspecified")]
        Unspecified,
        [EnumExpansion(TitleScreenExpansion.ARealmReborn)]
        [EnumTranslation("A Realm Reborn")]
        ARealmReborn,
        [EnumExpansion(TitleScreenExpansion.ARealmReborn)]
        [EnumTranslation("FFXIV Free Trial")]
        FreeTrial,
        [EnumExpansion(TitleScreenExpansion.Heavensward)]
        [EnumTranslation("Heavensward")]
        Heavensward,
        [EnumExpansion(TitleScreenExpansion.Stormblood)]
        [EnumTranslation("Stormblood")]
        Stormblood,
        [EnumExpansion(TitleScreenExpansion.Shadowbringers)]
        [EnumTranslation("Shadowbringers")]
        Shadowbringers,
        [EnumExpansion(TitleScreenExpansion.Endwalker)]
        [EnumTranslation("Endwalker")]
        Endwalker,
        [EnumExpansion(TitleScreenExpansion.Dawntrail)]
        [EnumTranslation("Dawntrail")]
        Dawntrail
    }
}
