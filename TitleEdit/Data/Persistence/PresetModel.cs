using System;

namespace TitleEdit.Data.Persistence
{
    public struct PresetModel
    {
        public readonly static int CurrentVersion = 3;

        public int? Version = CurrentVersion;
        public string Name = "";
        public string Author = "";
        public CameraFollowMode CameraFollowMode = CameraFollowMode.Inherit;
        public bool LastLocationMount = false;
        public LocationModel LocationModel = new();
        [NonSerialized]
        public string FileName = "";
        [NonSerialized]
        public string? Tooltip = null;

        public PresetModel()
        {
        }
    }

}
