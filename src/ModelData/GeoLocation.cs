using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ValuteAndWeatherStatistic.ModelData
{
    public class GeoLocationResponse
    {
        [JsonPropertyName("geo")]
        public GeoLocation Geo { get; set; }
    }

    public class GeoLocation
    {
        [JsonPropertyName("continent_code")]
        public string? ContinentCode { get; set; }

        [JsonPropertyName("continent_name")]
        public string? ContinentName { get; set; }

        [JsonPropertyName("country_code2")]
        public string? CountryCode2 { get; set; }

        [JsonPropertyName("country_code3")]
        public string? CountryCode3 { get; set; }

        [JsonPropertyName("country_name")]
        public string? CountryName { get; set; }

        [JsonPropertyName("country_name_official")]
        public string? CountryNameOfficial { get; set; }

        [JsonPropertyName("is_eu")]
        public bool IsEu { get; set; }

        [JsonPropertyName("state_prov")]
        public string? StateProv { get; set; }

        [JsonPropertyName("state_code")]
        public string? StateCode { get; set; }

        [JsonPropertyName("district")]
        public string? District { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("zipcode")]
        public string? Zipcode { get; set; }

        [JsonPropertyName("latitude")]
        public string? Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public string? Longitude { get; set; }
    }
}
