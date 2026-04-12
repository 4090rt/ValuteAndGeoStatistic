using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ValuteAndWeatherStatistic.ModelData
{
    public class CordinatsData
    {
        [JsonPropertyName("geo")]
        public Geo Geo { get; set; }

        [JsonPropertyName("timezone")]
        public string Timezone { get; set; }

        [JsonPropertyName("timezone_offset")]
        public int TimezoneOffset { get; set; }

        [JsonPropertyName("timezone_offset_with_dst")]
        public int TimezoneOffsetWithDst { get; set; }

        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("date_time")]
        public string DateTime { get; set; }

        [JsonPropertyName("date_time_txt")]
        public string DateTimeTxt { get; set; }

        [JsonPropertyName("date_time_wti")]
        public string DateTimeWti { get; set; }

        [JsonPropertyName("date_time_ymd")]
        public string DateTimeYmd { get; set; }

        [JsonPropertyName("date_time_unix")]
        public double DateTimeUnix { get; set; }

        [JsonPropertyName("time_24")]
        public string Time24 { get; set; }

        [JsonPropertyName("time_12")]
        public string Time12 { get; set; }

        [JsonPropertyName("week")]
        public int Week { get; set; }

        [JsonPropertyName("month")]
        public int Month { get; set; }

        [JsonPropertyName("year")]
        public int Year { get; set; }

        [JsonPropertyName("year_abbr")]
        public string YearAbbr { get; set; }

        [JsonPropertyName("current_tz_abbreviation")]
        public string CurrentTzAbbreviation { get; set; }

        [JsonPropertyName("current_tz_full_name")]
        public string CurrentTzFullName { get; set; }

        [JsonPropertyName("standard_tz_abbreviation")]
        public string StandardTzAbbreviation { get; set; }

        [JsonPropertyName("standard_tz_full_name")]
        public string StandardTzFullName { get; set; }

        [JsonPropertyName("is_dst")]
        public bool IsDst { get; set; }

        [JsonPropertyName("dst_savings")]
        public int DstSavings { get; set; }

        [JsonPropertyName("dst_exists")]
        public bool DstExists { get; set; }

        [JsonPropertyName("dst_tz_abbreviation")]
        public string DstTzAbbreviation { get; set; }

        [JsonPropertyName("dst_tz_full_name")]
        public string DstTzFullName { get; set; }

        [JsonPropertyName("dst_start")]
        public string DstStart { get; set; }

        [JsonPropertyName("dst_end")]
        public string DstEnd { get; set; }
    }

    public class Geo
    {
        [JsonPropertyName("location")]
        public string Location { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("locality")]
        public string Locality { get; set; }

        [JsonPropertyName("latitude")]
        public string Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public string Longitude { get; set; }
    }
}
