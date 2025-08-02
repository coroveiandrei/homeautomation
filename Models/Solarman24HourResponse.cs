using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HomeAutomation.Models
{
    public class Solarman24HourResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("stationDataItems")]
        public List<SolarmanHourlyItem>? StationDataItems { get; set; }
    }
}
