using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValuteAndWeatherStatistic.ModelData.ModelDataSelectRequest
{
    public class RequestWeatherSelect
    {
        public string Id { get; set; }
        public string Timezone { get; set; }
        public string Temperature { get; set; }
        public string ApparentTemperature { get; set; }
        public string RelativeHumidity { get; set; }
        public string Precipitation { get; set; }
        public string WeatherCode { get; set; }
        public string WindSpeed { get; set; }
        public string WindDirection { get; set; }
        public string DateUpdate { get; set; }
    }
}
