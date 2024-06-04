using System;

namespace CharacterSelectBackgroundPlugin.Data.Persistence
{
    public struct PresetModel
    {
        public int Version = 1;
        public string Name = "";
        public string Author = "";
        [NonSerialized]
        public string FileName = "";
        public CameraFollowMode CameraFollowMode = CameraFollowMode.Inherit;
        public bool LastLocationMount = false;
        public LocationModel LocationModel = new();

        public PresetModel()
        {
        }
    }

}
