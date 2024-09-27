using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using System.Collections.Generic;
using TitleEdit.Data.Layout;
using TitleEdit.Utility;

namespace TitleEdit.PluginServices
{
    public class WeatherService : AbstractService
    {
        public unsafe byte WeatherId
        {
            get => EnvManager.Instance()->ActiveWeather;
            set => EnvManager.Instance()->ActiveWeather = value;
        }

        private readonly Dictionary<string, List<byte>> weathers = [];
        public WeatherService()
        {

        }

        public List<byte> GetWeathers(string territoryPath)
        {
            if (territoryPath == null)
            {
                return [];
            }
            if (weathers.TryGetValue(territoryPath, out var value))
            {
                return value;
            }
            try
            {
                List<byte> result = [];
                var file = Services.DataManager.GetFile<LvbFile>($"bg/{territoryPath}.lvb");
                if (file?.weatherIds != null && file.weatherIds.Length > 0)
                {

                    foreach (var weather in file.weatherIds)
                    {
                        if (weather > 0 && weather < 255)
                        {
                            result.Add((byte)weather);
                        }
                    }
                }
                weathers.Add(territoryPath, result);

                return result;
            }
            catch
            {
                weathers.Add(territoryPath, []);
                return [];
            }
        }
    }
}
