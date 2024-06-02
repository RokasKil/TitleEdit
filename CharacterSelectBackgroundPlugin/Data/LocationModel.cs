using System.Collections.Generic;
using System.Numerics;
using static CharacterSelectBackgroundPlugin.Data.CharacterExpanded;

namespace CharacterSelectBackgroundPlugin.Data
{
    public struct LocationModel
    {
        public int Version = 1;
        public string TerritoryPath = "";
        public ushort TerritoryTypeId;
        public Vector3 Position;
        public float Rotation;
        public byte WeatherId;
        public ushort TimeOffset;
        public int BgmId = -1;
        public string? BgmPath;
        public MovementMode MovementMode = MovementMode.Normal;
        public MountModel Mount;
        public HashSet<ulong> Active = [];
        public HashSet<ulong> Inactive = [];
        public Dictionary<ulong, short> VfxTriggerIndexes = [];
        public uint[] Festivals = new uint[4];

        public LocationModel()
        {
        }
    }

    public struct MountModel
    {
        public uint MountId = 0;
        public uint BuddyModelTop = 0;
        public uint BuddyModelBody = 0;
        public uint BuddyModelLegs = 0;
        public byte BuddyStain = 0;

        public MountModel()
        {
        }
    }
}
