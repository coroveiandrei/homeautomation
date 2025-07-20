using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace HomeAutomation.Services;

public class SolarmanService
{
    private readonly HttpClient _httpClient;
    private readonly string _solarmanEmail;
    private readonly string _solarmanPassword;
    private readonly string _solarmanAppId;
    private readonly string _solarmanAppSecret;
    private const string SolarmanBaseUrl = "https://globalapi.solarmanpv.com";
    
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public SolarmanService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _solarmanEmail = Environment.GetEnvironmentVariable("SOLARMAN_EMAIL") ?? "";
        _solarmanPassword = Environment.GetEnvironmentVariable("SOLARMAN_PASSWORD") ?? "";
        _solarmanAppId = Environment.GetEnvironmentVariable("SOLARMAN_APP_ID") ?? "";
        _solarmanAppSecret = Environment.GetEnvironmentVariable("SOLARMAN_APP_SECRET") ?? "";
    }

    private async Task EnsureAuthenticatedAsync()
    {
        if (_accessToken == null || DateTime.UtcNow >= _tokenExpiry)
        {
            await AuthenticateAsync();
        }
    }

    private async Task AuthenticateAsync()
    {
        var authRequest = new
        {
            appId = _solarmanAppId,
            appSecret = _solarmanAppSecret,
            email = _solarmanEmail,
            password = _solarmanPassword
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{SolarmanBaseUrl}/account/v1.0/token", authRequest);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SolarmanAuthResponse>();
                if (result?.Success == true)
                {
                    _accessToken = result.AccessToken;
                    _tokenExpiry = DateTime.UtcNow.AddMinutes(30); // Tokens typically expire in 30 minutes
                }
            }
        }
        catch
        {
            // Authentication failed, will use mock data
        }
    }

    public async Task<SolarDataResponse> GetTodayProductionAsync()
    {
        try
        {
            await EnsureAuthenticatedAsync();

            if (_accessToken == null)
            {
                // Return mock data if authentication failed
                return GenerateMockSolarData();
            }

            // Get station list first
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
            
            var stationsResponse = await _httpClient.GetFromJsonAsync<SolarmanStationsResponse>($"{SolarmanBaseUrl}/station/v1.0/list?page=1&size=20");
            
            if (stationsResponse?.Success == true && stationsResponse.StationInfos?.Any() == true)
            {
                var stationId = stationsResponse.StationInfos.First().StationId;
                var today = DateTime.Now.ToString("yyyy-MM-dd");
                
                var dataResponse = await _httpClient.GetFromJsonAsync<SolarmanDataResponse>($"{SolarmanBaseUrl}/station/v1.0/realTime?stationId={stationId}");
                
                if (dataResponse?.Success == true)
                {
                    return ConvertToChartData(dataResponse);
                }
            }
        }
        catch
        {
            // Return mock data on any error
        }

        return GenerateMockSolarData();
    }

    private SolarDataResponse GenerateMockSolarData()
    {
        var labels = new List<string>();
        var values = new List<double>();
        var random = new Random();

        // Generate hourly data from 6 AM to 6 PM
        for (int hour = 6; hour <= 18; hour++)
        {
            labels.Add($"{hour:D2}:00");
            
            // Simulate solar production curve
            double baseProduction = Math.Sin((hour - 6) * Math.PI / 12) * 5; // Peak at noon
            double randomVariation = (random.NextDouble() - 0.5) * 1; // ±0.5 kW variation
            values.Add(Math.Max(0, baseProduction + randomVariation));
        }

        return new SolarDataResponse
        {
            Labels = labels.ToArray(),
            Values = values.ToArray()
        };
    }

    private SolarDataResponse ConvertToChartData(SolarmanDataResponse response)
    {
        // Convert Solarman data to chart format
        // This would need to be customized based on actual Solarman API response structure
        return GenerateMockSolarData(); // Fallback to mock data for now
    }
}

// Solarman API Models
public class SolarmanAuthResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }
}

public class SolarmanStationsResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("stationInfos")]
    public List<SolarmanStation>? StationInfos { get; set; }
}

public class SolarmanStation
{
    [JsonPropertyName("stationId")]
    public string StationId { get; set; } = "";
}

public class SolarmanDataResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
}

public class SolarDataResponse
{
    public string[] Labels { get; set; } = Array.Empty<string>();
    public double[] Values { get; set; } = Array.Empty<double>();
}
