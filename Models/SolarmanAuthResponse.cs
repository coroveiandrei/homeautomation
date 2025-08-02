using System.Text.Json.Serialization;

namespace HomeAutomation.Models
{
    public class SolarmanAuthResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }
}
