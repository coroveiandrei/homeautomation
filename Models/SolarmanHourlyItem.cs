using System.Text.Json.Serialization;

namespace HomeAutomation.Models
{
    public class SolarmanHourlyItem
    {
        [JsonPropertyName("generationPower")]
        public double? GenerationPower { get; set; }

        [JsonPropertyName("dateTime")]
        public double DateTimeUnix { get; set; }

        [JsonPropertyName("year")]
        public int Year { get; set; }

        [JsonPropertyName("month")]
        public int Month { get; set; }

        [JsonPropertyName("day")]
        public int Day { get; set; }
    }
}
