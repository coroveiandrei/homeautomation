using System.Text.Json.Serialization;

namespace HomeAutomation.Models
{
    public class SolarmanDataResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }
}
