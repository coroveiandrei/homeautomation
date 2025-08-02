using System.Text.Json.Serialization;

namespace HomeAutomation.Models
{
    public class SolarDataResponse
    {
        [JsonPropertyName("labels")]
        public string[] Labels { get; set; } = System.Array.Empty<string>();

        [JsonPropertyName("values")]
        public double[] Values { get; set; } = System.Array.Empty<double>();
    }
}
