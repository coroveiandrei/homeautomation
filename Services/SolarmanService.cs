using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using HomeAutomation.Models;

namespace HomeAutomation.Services
{
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
            var url = $"{SolarmanBaseUrl}/account/v1.0/token?appId={_solarmanAppId}&language=en";
            var payload = new
            {
                email = _solarmanEmail,
                password = _solarmanPassword,
                appSecret = _solarmanAppSecret
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("User-Agent", "Paw/3.3.1 (Macintosh; OS X/12.0.1) GCDHTTPRequest");

            try
            {
                var response = await _httpClient.SendAsync(request);
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

        public async Task<SolarDataResponse> GetLast30DaysChartAsync()
        {
            try
            {
                await EnsureAuthenticatedAsync();

                if (_accessToken == null)
                {
                    // Return mock data if authentication failed
                    return GenerateMockSolarData();
                }

                // Use /station/v1.0/history endpoint for daily data
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

                // TODO: Make stationId configurable or select from list if needed
                var stationId = 63818743L;
                var startTime = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
                var endTime = DateTime.Now.ToString("yyyy-MM-dd");
                var historyBody = new
                {
                    stationId = stationId,
                    timeType = 2,
                    startTime = startTime,
                    endTime = endTime
                };
                var historyRequest = new HttpRequestMessage(HttpMethod.Post, $"{SolarmanBaseUrl}/station/v1.0/history")
                {
                    Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(historyBody), Encoding.UTF8, "application/json")
                };
                var historyResponse = await _httpClient.SendAsync(historyRequest);
                if (historyResponse.IsSuccessStatusCode)
                {
                    var historyData = await historyResponse.Content.ReadFromJsonAsync<SolarmanHistoryResponse>();
                    if (historyData?.Success == true && historyData.StationDataItems?.Any() == true)
                    {
                        return ConvertToChartData(historyData);
                    }
                }
            }
            catch
            {
                // Return mock data on any error
            }

            return GenerateMockSolarData();
        }

        public async Task<SolarDataResponse> GetLast24HoursHistoryAsync()
        {
            try
            {
                await EnsureAuthenticatedAsync();

                if (_accessToken == null)
                {
                    // Return mock data if authentication failed
                    return GenerateMockSolarData();
                }

                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

                var stationId = 63818743L; // TODO: Make configurable
                var endTime = DateTime.UtcNow;
                var startTime = endTime.AddHours(-24);
                var historyBody = new
                {
                    stationId = stationId,
                    timeType = 1, // 1 = hour granularity
                    startTime = startTime.ToString("yyyy-MM-dd HH:00:00"),
                    endTime = endTime.ToString("yyyy-MM-dd HH:00:00")
                };
                var historyRequest = new HttpRequestMessage(HttpMethod.Post, $"{SolarmanBaseUrl}/station/v1.0/history")
                {
                    Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(historyBody), Encoding.UTF8, "application/json")
                };
                var historyResponse = await _httpClient.SendAsync(historyRequest);
                if (historyResponse.IsSuccessStatusCode)
                {
                    var historyData = await historyResponse.Content.ReadFromJsonAsync<SolarmanHistoryResponse>();
                    if (historyData?.Success == true && historyData.StationDataItems?.Any() == true)
                    {
                        return ConvertToHourlyChartData(historyData);
                    }
                }
            }
            catch
            {
                // Return mock data on any error
            }

            return GenerateMockSolarData();
        }

        private SolarDataResponse ConvertToChartData(SolarmanHistoryResponse response)
        {
            // Map SolarmanHistoryResponse to chart data (daily)
            if (response.StationDataItems != null)
            {
                var labels = new List<string>();
                var values = new List<double>();
                foreach (var item in response.StationDataItems)
                {
                    // Use day/month/year for label
                    labels.Add($"{item.year}-{item.month:D2}-{item.day:D2}");
                    values.Add(item.GenerationValue);
                }
                if (labels.Count > 0)
                {
                    return new SolarDataResponse
                    {
                        Labels = labels.ToArray(),
                        Values = values.ToArray()
                    };
                }
            }
            return GenerateMockSolarData();
        }

        private SolarDataResponse ConvertToHourlyChartData(SolarmanHistoryResponse response)
        {
            // Map SolarmanHistoryResponse to chart data for hourly data
            if (response.StationDataItems != null)
            {
                var labels = new List<string>();
                var values = new List<double>();
                foreach (var item in response.StationDataItems)
                {
                    string label = $"{item.year}-{item.month:D2}-{item.day:D2}";
                    var hourProp = item.GetType().GetProperty("hour");
                    if (hourProp != null)
                    {
                        int hour = (int)(hourProp.GetValue(item) ?? 0);
                        label += $" {hour:D2}:00";
                    }
                    labels.Add(label);
                    values.Add(item.GenerationValue);
                }
                if (labels.Count > 0)
                {
                    return new SolarDataResponse
                    {
                        Labels = labels.ToArray(),
                        Values = values.ToArray()
                    };
                }
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

        /// <summary>
        /// Returns chart data for the last 24 hours (hourly granularity).
        /// </summary>
        public async Task<SolarDataResponse> GetLast24HoursChartAsync()
        {
            try
            {
                await EnsureAuthenticatedAsync();

                if (_accessToken == null)
                {
                    // Return mock data if authentication failed
                    return GenerateMockSolarData();
                }

                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

                var stationId = 63818743L; // TODO: Make configurable
                var endTime = DateTime.UtcNow;
                var startTime = endTime.AddHours(-24);
                var historyBody = new
                {
                    stationId = stationId,
                    timeType = 1, // 1 = hour granularity
                    startTime = startTime.ToString("yyyy-MM-dd HH:00:00"),
                    endTime = endTime.ToString("yyyy-MM-dd HH:00:00")
                };
                var historyRequest = new HttpRequestMessage(HttpMethod.Post, $"{SolarmanBaseUrl}/station/v1.0/history")
                {
                    Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(historyBody), Encoding.UTF8, "application/json")
                };
                var historyResponse = await _httpClient.SendAsync(historyRequest);
                if (historyResponse.IsSuccessStatusCode)
                {
                    var historyData = await historyResponse.Content.ReadFromJsonAsync<SolarmanHistoryResponse>();
                    if (historyData?.Success == true && historyData.StationDataItems?.Any() == true)
                    {
                        return ConvertToHourlyChartData(historyData);
                    }
                }
            }
            catch
            {
                // Return mock data on any error
            }

            return GenerateMockSolarData();
        }
    }
}