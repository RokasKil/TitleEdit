using Newtonsoft.Json;
using TitleEdit.Data.Persistence;

namespace TitleEdit.PluginServices.Migration
{
    public partial class MigrationService
    {
        public GroupModel MigrateGroup(string groupTextData) => MigrateGroup(groupTextData, out _);

        public GroupModel MigrateGroup(string groupTextData, out bool changed)
        {
            changed = false;
            var preset = JsonConvert.DeserializeObject<GroupModel>(groupTextData);
            switch (preset.Version)
            {
                default:
                    break;
            }
            return preset;
        }

    }
}
