using TitleEdit.Utility;

namespace TitleEdit.Data.Persistence
{

    public enum CameraFollowMode
    {
        [EnumTranslation("Use user setting")]
        Inherit = 0, // Only used in presets
        [EnumTranslation("Character model position", tooltip: "Keeps camera pointed at the actual character model when offset on a mount")]
        ModelPosition = 1,
        [EnumTranslation("Static character object position", tooltip: "Keep camera pointed at the charater game position ignoring the mounted model offset")]
        ObjectPosition = 2
    }
}
