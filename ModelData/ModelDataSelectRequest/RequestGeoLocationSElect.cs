using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValuteAndWeatherStatistic.ModelData.ModelDataSelectRequest
{
    public class RequestGeoLocationSElect
    {
        public  string ContinentName { get; set; }
        public string CountryCode2 { get; set; }
        public string CountryName { get; set; }
        public string CountryNameOfficial { get; set; }
        public string StateProv { get; set; }
        public string District { get; set; }
        public string City { get; set; }
        public string DateUpdate { get; set; }

    }
}
