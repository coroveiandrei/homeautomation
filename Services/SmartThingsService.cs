using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace HomeAutomation.Services;

public class SmartThingsService
{
    private readonly HttpClient _httpClient;
    private string? _accessToken;
    private readonly string _clientId = "70b6cb9e-a787-4e30-8c90-d39abdb90333"; // TODO: Set from config/env
    private readonly string _clientSecret = ""; // TODO: Set from config/env
    private const string BaseUrl = "https://api.smartthings.com/v1";
    private const string AuthUrl = "https://api.smartthings.com/v1/oauth/authorize";
    private const string TokenUrl = "https://api.smartthings.com/v1/oauth/token";

    public SmartThingsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string GetAuthorizationUrl(string redirectUri, string scope = "r:devices:*")
    {
        var url = $"{AuthUrl}?response_type=code&client_id={_clientId}&scope={Uri.EscapeDataString(scope)}&redirect_uri={Uri.EscapeDataString(redirectUri)}";
        return url;
    }

    public async Task<bool> ExchangeAuthorizationCodeAsync(string code, string redirectUri)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["code"] = code,
                ["redirect_uri"] = redirectUri
            })
        };
        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            dynamic? tokenResponse = JsonConvert.DeserializeObject(json);
            string? accessToken = tokenResponse?.access_token;
            if (!string.IsNullOrEmpty(accessToken))
            {
                await AuthenticateAsync(accessToken);
                return true;
            }
        }
        return false;
    }

    public async Task<bool> AuthenticateAsync(string accessToken)
    {
        _accessToken = accessToken;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        return true;
    }

    public async Task<List<DeviceWithStatus>> GetDevicesWithStatusAsync()
    {
        if (string.IsNullOrEmpty(_accessToken))
            throw new InvalidOperationException("Not authenticated with SmartThings");
        var devices = await _httpClient.GetFromJsonAsync<DeviceList>($"{BaseUrl}/devices");
        var devicesWithStatus = new List<DeviceWithStatus>();

        if (devices?.Items != null)
        {
            foreach (var device in devices.Items)
            {
                var deviceWithStatus = new DeviceWithStatus
                {
                    DeviceId = device.DeviceId,
                    Name = device.Name,
                    Label = device.Label,
                    DeviceTypeName = device.DeviceTypeName,
                    Capabilities = new List<DeviceCapability>()
                };

                try
                {
                    var status = await _httpClient.GetFromJsonAsync<DeviceStatus>($"{BaseUrl}/devices/{device.DeviceId}/status");
                    if (status?.Components?.TryGetValue("main", out var mainComponent) == true)
                    {
                        AddCapabilityIfExists(deviceWithStatus.Capabilities, mainComponent, "switch");
                        AddCapabilityIfExists(deviceWithStatus.Capabilities, mainComponent, "switchLevel");
                        AddCapabilityIfExists(deviceWithStatus.Capabilities, mainComponent, "temperatureMeasurement", "temperature");
                        AddCapabilityIfExists(deviceWithStatus.Capabilities, mainComponent, "thermostatMode");
                        AddCapabilityIfExists(deviceWithStatus.Capabilities, mainComponent, "dryerOperatingState", "machineState");
                        AddCapabilityIfExists(deviceWithStatus.Capabilities, mainComponent, "machineState");
                    }
                }
                catch
                {
                    // If we can't get status, still add the device
                }

                devicesWithStatus.Add(deviceWithStatus);
            }
        }

        return devicesWithStatus;
    }

    public async Task<bool> SendCommandAsync(string deviceId, string capability, string command, List<object>? arguments = null)
    {
        if (string.IsNullOrEmpty(_accessToken))
            throw new InvalidOperationException("Not authenticated with SmartThings");
        var commandPayload = new CommandPayload
        {
            Commands = new List<DeviceCommand>
            {
                new()
                {
                    Capability = capability,
                    Command = command,
                    Arguments = arguments ?? new List<object>()
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/devices/{deviceId}/commands", commandPayload);
        return response.IsSuccessStatusCode;
    }

    private void AddCapabilityIfExists(List<DeviceCapability> capabilities, Dictionary<string, Dictionary<string, object>> component, string capabilityName, string? attributeName = null)
    {
        attributeName ??= capabilityName;
        if (component.TryGetValue(capabilityName, out var capability) && 
            capability.TryGetValue(attributeName, out var attribute) && 
            attribute is JsonElement element && 
            element.TryGetProperty("value", out var value))
        {
            capabilities.Add(new DeviceCapability
            {
                Name = capabilityName,
                Value = value.ToString()
            });
        }
    }
}

// SmartThings Data Models
public class DeviceWithStatus
{
    public string DeviceId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Label { get; set; } = "";
    public string DeviceTypeName { get; set; } = "";
    public List<DeviceCapability> Capabilities { get; set; } = new();
}

public class DeviceCapability
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
}

public class DeviceCommandRequest
{
    public string Capability { get; set; } = "";
    public string Command { get; set; } = "";
    public List<object>? Arguments { get; set; }
}

public class DeviceList
{
    [JsonPropertyName("items")]
    public List<Device>? Items { get; set; }
}

public class CommandPayload
{
    [JsonPropertyName("commands")]
    public List<DeviceCommand> Commands { get; set; } = new();
}

public class DeviceCommand
{
    [JsonPropertyName("component")]
    public string Component { get; set; } = "main";

    [JsonPropertyName("capability")]
    public string Capability { get; set; } = "";

    [JsonPropertyName("command")]
    public string Command { get; set; } = "";

    [JsonPropertyName("arguments")]
    public List<object> Arguments { get; set; } = new();
}

public class DeviceStatus
{
    [JsonPropertyName("components")]
    public Dictionary<string, Dictionary<string, Dictionary<string, object>>>? Components { get; set; }
}

public class Device
{
    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";
    
    [JsonPropertyName("deviceTypeName")]
    public string DeviceTypeName { get; set; } = "";
}
