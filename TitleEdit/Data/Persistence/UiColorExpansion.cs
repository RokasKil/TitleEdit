using TitleEdit.Utility;

namespace TitleEdit.Data.Persistence
{
    public enum UiColorExpansion : int
    {
        [EnumTranslation("Unspecified")]
        Unspecified = -2,
        [EnumTranslation("Custom")]
        Custom = -1,
        [EnumTranslation("A Realm Reborn")]
        ARealmReborn = 0,
        [EnumTranslation("Heavensward")]
        Heavensward = 1,
        [EnumTranslation("Stormblood")]
        Stormblood = 2,
        [EnumTranslation("Shadowbringers")]
        Shadowbringers = 3,
        [EnumTranslation("Endwalker")]
        Endwalker = 4,
        [EnumTranslation("Dawntrail")]
        Dawntrail = 5
    }
}
