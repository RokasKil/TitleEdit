using System.Numerics;
using TitleEdit.Data.Lobby;

namespace TitleEdit.Data.Persistence.Migration
{
    public struct TitleEditV2Screen
    {
        public string? Name;
        public string? Logo;
        public bool? DisplayLogo;
        public string? TerritoryPath;
        public Vector3? CameraPos;
        public Vector3? FixOnPos;
        public float? FovY;
        public byte? WeatherId;
        public ushort? TimeOffset;
        public string? BgmPath;
        public TitleScreenExpansion? TitleOverride;
    }
}
