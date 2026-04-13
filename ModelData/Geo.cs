using System.Text.Json.Serialization;

namespace ValuteAndWeatherStatistic.ModelData
{
    public class Geo
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
