using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using TitleEdit.Data.Character;
using TitleEdit.Data.Lobby;

namespace TitleEdit.Data.Persistence
{
    // Could seperate these into title screen and character select but I kinda want to allow users to easily convert between the two maybe (also am lazy)
    public struct LocationModel
    {
        public readonly static int CurrentVersion = 4;

        public int Version = CurrentVersion;
        public LocationType LocationType = LocationType.TitleScreen;
        public TitleScreenLogo TitleScreenLogo = TitleScreenLogo.Dawntrail;
        public string TerritoryPath = ""; // TODO: Why am I even saving both
        public ushort TerritoryTypeId;
        public Vector3 Position;
        public Vector3 CameraPosition;
        public float Rotation;
        public float Yaw; // side
        public float Roll; // roll
        public float Pitch; // up/down
        public float Fov = 1;
        public byte WeatherId;
        public ushort TimeOffset;
        public uint BgmId = 0; // TODO: Why am I even saving both
        public string? BgmPath;
        public MovementMode MovementMode = MovementMode.Normal;
        public MountModel Mount;
        public HashSet<ulong> Active = [];
        public HashSet<ulong> Inactive = [];
        public Dictionary<ulong, short> VfxTriggerIndexes = [];
        // TODO: Rework this into the new Festival objects
        public uint[] Festivals = new uint[4];
        public bool SaveLayout = false;
        public bool UseVfx = true;
        public bool SaveFestivals = true;
        public TitleScreenExpansion? TitleScreenOverride = null;
        public UiColorModel UiColor = UiColors.Dawntrail;

        // Only set when used with a preset
        [NonSerialized]
        public CameraFollowMode CameraFollowMode = CameraFollowMode.Inherit;
        // What to say in the toast notification
        [NonSerialized]
        public string ToastNotificationText = "";
        public LocationModel()
        {
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj?.GetType() == typeof(LocationModel))
            {
                var other = (LocationModel)obj;
                return Equals(Version, other.Version) &&
                    Equals(LocationType, other.LocationType) &&
                    Equals(TitleScreenLogo, other.TitleScreenLogo) &&
                    Equals(TerritoryPath, other.TerritoryPath) &&
                    Equals(TerritoryTypeId, other.TerritoryTypeId) &&
                    Equals(Position, other.Position) &&
                    Equals(CameraPosition, other.CameraPosition) &&
                    Equals(Rotation, other.Rotation) &&
                    Equals(Yaw, other.Yaw) &&
                    Equals(Roll, other.Roll) &&
                    Equals(Pitch, other.Pitch) &&
                    Equals(Fov, other.Fov) &&
                    Equals(WeatherId, other.WeatherId) &&
                    Equals(TimeOffset, other.TimeOffset) &&
                    Equals(BgmId, other.BgmId) &&
                    Equals(BgmPath, other.BgmPath) &&
                    Equals(MovementMode, other.MovementMode) &&
                    Equals(Active, other.Active) &&
                    Equals(Inactive, other.Inactive) &&
                    Equals(VfxTriggerIndexes, other.VfxTriggerIndexes) &&
                    Equals(Festivals, other.Festivals) &&
                    Equals(TitleScreenOverride, other.TitleScreenOverride) &&
                    Equals(UiColor, other.UiColor);
            }
            return false;
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Version);
            hash.Add(LocationType);
            hash.Add(TitleScreenLogo);
            hash.Add(TerritoryPath);
            hash.Add(TerritoryTypeId);
            hash.Add(Position);
            hash.Add(CameraPosition);
            hash.Add(Rotation);
            hash.Add(Yaw);
            hash.Add(Roll);
            hash.Add(Pitch);
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
}
