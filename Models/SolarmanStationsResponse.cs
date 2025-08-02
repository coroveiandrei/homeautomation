using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HomeAutomation.Models
{
    public class SolarmanStationsResponse
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("msg")]
        public string? Msg { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("requestId")]
        public string? RequestId { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("stationList")]
        public List<SolarmanStation>? StationList { get; set; }
    }
}
