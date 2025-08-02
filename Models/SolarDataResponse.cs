using System.Text.Json.Serialization;

namespace HomeAutomation.Models
{
    public class SolarDataResponse
    {
        [JsonPropertyName("labels")]
        public string[] Labels { get; set; } = System.Array.Empty<string>();

        [JsonPropertyName("values")]
        public double[] Values { get; set; } = System.Array.Empty<double>();


        [JsonPropertyName("selfConsumed")]
        public double[] SelfConsumed { get; set; } = System.Array.Empty<double>();

        [JsonPropertyName("exportedToGrid")]
        public double[] ExportedToGrid { get; set; } = System.Array.Empty<double>();

        [JsonPropertyName("batterySoc")]
        public double[] BatterySoc { get; set; } = System.Array.Empty<double>();

        [JsonPropertyName("usePower")]
        public double[] UsePower { get; set; } = System.Array.Empty<double>();
    }
}
