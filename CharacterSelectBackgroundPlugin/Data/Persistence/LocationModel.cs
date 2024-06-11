using CharacterSelectBackgroundPlugin.Data.Character;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace CharacterSelectBackgroundPlugin.Data.Persistence
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
        public uint BgmId = 0;
        public string? BgmPath;
        public MovementMode MovementMode = MovementMode.Normal;
        public MountModel Mount;
        public HashSet<ulong> Active = [];
        public HashSet<ulong> Inactive = [];
        public Dictionary<ulong, short> VfxTriggerIndexes = [];
        public uint[] Festivals = new uint[4];
        // Only set when used with a preset
        [NonSerialized]
        public CameraFollowMode CameraFollowMode = CameraFollowMode.Inherit;

        public LocationModel()
        {
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj?.GetType() == typeof(LocationModel))
            {
                var other = (LocationModel)obj;
                return Equals(Version, other.Version) &&
                    Equals(TerritoryPath, other.TerritoryPath) &&
                    Equals(TerritoryTypeId, other.TerritoryTypeId) &&
                    Equals(Position, other.Position) &&
                    Equals(Rotation, other.Rotation) &&
                    Equals(WeatherId, other.WeatherId) &&
                    Equals(TimeOffset, other.TimeOffset) &&
                    Equals(BgmId, other.BgmId) &&
                    Equals(BgmPath, other.BgmPath) &&
                    Equals(MovementMode, other.MovementMode) &&
                    Equals(Active, other.Active) &&
                    Equals(Inactive, other.Inactive) &&
                    Equals(VfxTriggerIndexes, other.VfxTriggerIndexes) &&
                    Equals(Festivals, other.Festivals);
            }
            return false;
        }
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Version);
            hash.Add(TerritoryPath);
            hash.Add(TerritoryTypeId);
            hash.Add(Position);
            hash.Add(Rotation);
            hash.Add(WeatherId);
            hash.Add(TimeOffset);
            hash.Add(BgmId);
            hash.Add(BgmPath);
            hash.Add(MovementMode);
            hash.Add(Active);
            hash.Add(Inactive);
            hash.Add(VfxTriggerIndexes);
            hash.Add(Festivals);
            return hash.ToHashCode();
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
