using System;
using System.Collections.Generic;

namespace TitleEdit.Data.Persistence
{
    public struct GroupModel
    {
        public readonly static int CurrentVersion = 1;

        public int? Version = CurrentVersion;
        public string Name = "";
        public LocationType LocationType = LocationType.TitleScreen;
        public List<string?> PresetFileNames = [];
        [NonSerialized]
        public string FileName = "";
        [NonSerialized]
        public string? Tooltip = null;

        public GroupModel()
        {
        }
    }

}
