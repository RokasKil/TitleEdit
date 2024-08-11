using Newtonsoft.Json;
using TitleEdit.Data.Persistence;
using TitleEdit.Utility;

namespace TitleEdit.PluginServices.Migration
{
    public partial class MigrationService
    {

        public LocationModel MigrateLocation(string locationTextData) => MigrateLocation(locationTextData, out _);

        public LocationModel MigrateLocation(string locationTextData, out bool changed)
        {
            var location = JsonConvert.DeserializeObject<LocationModel>(locationTextData);
            switch (location.Version)
            {
                case 1:
                    location = MigrateV1(location);
                    changed = true;
                    break;
                default:
                    changed = false;
                    break;
            }
            return location;
        }

        private LocationModel MigrateV1(LocationModel location)
        {
            Services.Log.Info($"Migrating location to v2");
            location.Version = 2;
            location.LocationType = LocationType.CharacterSelect;
            return location;
        }
    }
}
