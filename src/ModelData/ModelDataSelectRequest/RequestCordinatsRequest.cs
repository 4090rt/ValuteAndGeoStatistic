using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValuteAndWeatherStatistic.ModelData.ModelDataSelectRequest
{
    public class RequestCordinatsRequest
    {
        public string Id { get; set; } 
        public string timezone { get; set; }
        public string TimezoneOffset { get; set; }
        public string date_time { get; set; }
        public string date_time_unix { get; set; }
        public string current_tz_full_name { get; set; }
        public string week { get; set; }
        public string year { get; set; }
        public string DateUpdate { get; set; }
    }
}
