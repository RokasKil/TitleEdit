using System;

namespace CharacterSelectBackgroundPlugin.Data.Persistence
{
    public struct PresetModel
    {
        public int Version = 1;
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
