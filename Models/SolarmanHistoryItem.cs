using System.Text.Json.Serialization;

namespace HomeAutomation.Models
{
    public class SolarmanHistoryItem
    {
        [JsonPropertyName("generationValue")]
        public double GenerationValue { get; set; }

        [JsonPropertyName("useValue")]
        public double UseValue { get; set; }

        [JsonPropertyName("gridValue")]
        public double GridValue { get; set; }

        [JsonPropertyName("chargeValue")]
        public double ChargeValue { get; set; }

        [JsonPropertyName("year")]
        public int year { get; set; }

        [JsonPropertyName("month")]
        public int month { get; set; }

        [JsonPropertyName("day")]
        public int day { get; set; }
        // Add other properties as needed
    }
}
