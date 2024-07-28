using CharacterSelectBackgroundPlugin.Data.Persistence;
using CharacterSelectBackgroundPlugin.Utility;

namespace CharacterSelectBackgroundPlugin.PluginServices
{
    public class MigrationService : AbstractService
    {
        public MigrationService() { }

        public PresetModel Migrate(PresetModel preset) => Migrate(preset, out _);

        public PresetModel Migrate(PresetModel preset, out bool changed)
        {
            switch (preset.Version)
            {
                case 1:
                    preset = MigrateV1(preset);
                    changed = true;
                    break;
                default:
                    changed = false;
                    break;
            }
            return preset;
        }

        public LocationModel Migrate(LocationModel location) => Migrate(location, out _);

        public LocationModel Migrate(LocationModel location, out bool changed)
        {
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

        private PresetModel MigrateV1(PresetModel preset)
        {
            Services.Log.Info($"Migrating preset to v2 {preset.Name}");
            preset.Version = 2;
            preset.LocationModel = MigrateV1(preset.LocationModel);
            return preset;
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
