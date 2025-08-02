using System.Text.Json.Serialization;

namespace HomeAutomation.Models
{
    public class SolarmanStation
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("locationLat")]
        public double LocationLat { get; set; }

        [JsonPropertyName("locationLng")]
        public double LocationLng { get; set; }

        [JsonPropertyName("locationAddress")]
        public string? LocationAddress { get; set; }
        // ... add other properties as needed ...
    }
}
