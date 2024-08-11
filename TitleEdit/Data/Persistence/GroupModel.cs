using System;
using System.Collections.Generic;

namespace TitleEdit.Data.Persistence
{
    public struct GroupModel
    {
        public int? Version = 1;
        public string Name = "";
        public LocationType LocationType = LocationType.TitleScreen;
        public List<string> PresetFileNames = [];
        [NonSerialized]
        public string FileName = "";

        public GroupModel()
        {
        }
    }

}
