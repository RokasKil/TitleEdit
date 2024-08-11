using TitleEdit.Utility;

namespace TitleEdit.Data.Persistence
{

    public enum CameraFollowMode
    {
        [EnumTranslation("Use user setting")]
        Inherit = 0, // Only used in presets
        [EnumTranslation("Character model position")]
        ModelPosition = 1,
        [EnumTranslation("Static character object position")]
        ObjectPosition = 2
    }
}
