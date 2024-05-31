using System.Collections.Generic;
using System.Numerics;

namespace CharacterSelectBackgroundPlugin.Data
{
    public struct LocationModel
    {
        public string TerritoryPath = "";
        public Vector3 Position;
        public float Rotation;
        public byte WeatherId;
        public ushort TimeOffset;
        public string? BgmPath;
        public ushort MountId;
        public HashSet<ulong> Active = [];
        public HashSet<ulong> Inactive = [];
        public Dictionary<ulong, short> VfxTriggerIndexes = [];
        public uint[] Festivals = new uint[4];

        public LocationModel()
        {
        }
    }
}
