using System;
using System.Collections.Generic;
using System.Numerics;
using TitleEdit.Data.Character;
using TitleEdit.Data.Lobby;

namespace TitleEdit.Data.Persistence
{
    // Could seperate these into title screen and character select but I kinda want to allow users to easily convert between the two maybe (also am lazy)
    public struct LocationModel
    {
        public static readonly int CurrentVersion = 5;

        public int Version = CurrentVersion;
        public LocationType LocationType = LocationType.TitleScreen;
        public TitleScreenLogo TitleScreenLogo = TitleScreenLogo.Dawntrail;
        public string TerritoryPath = ""; // TODO: Why am I even saving both
        public ushort TerritoryTypeId;
        public uint LayoutTerritoryTypeId;
        public uint LayoutLayerFilterKey;
        public Vector3 Position;
        public Vector3 CameraPosition;
        public float Rotation;
        public float Yaw;   // side
        public float Roll;  // roll
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
        public bool SaveHousing = true;
        public bool SaveFestivals = true;
        // Used to force the title screen expansion to specific one and let the game handle the scene loading natively
        public TitleScreenExpansion? TitleScreenOverride = null;
        public UiColorModel UiColor = UiColors.Dawntrail;
        public TitleScreenMovie TitleScreenMovie = TitleScreenMovie.Unspecified;
        public bool UseLiveTime = false;

        public List<HousingFurnitureModel>? Furniture = null;
        public List<HousingPlotModel>? Plots = null;
        public HousingEstateModel? Estate = null;

        // Only set when used with a preset
        [NonSerialized]
        public CameraFollowMode CameraFollowMode = CameraFollowMode.Inherit;

        // What to say in the toast notification
        [NonSerialized]
        public string ToastNotificationText = "";

        // Currently only used for seasonal easter eggs
        [NonSerialized]
        public List<NpcModel>? Npcs = null;

        public LocationModel() { }
    }
}
