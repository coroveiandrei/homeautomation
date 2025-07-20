using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Text;

namespace HomeAutomation.Services;

public class BoschService
{
    private readonly HttpClient _httpClient;
    private readonly string _boschControllerIP;
    private readonly string _boschClientName;
    private readonly string _boschCertificatePath;
    private readonly string _boschPassword;
    private readonly string _boschIotHubUrl;
    private readonly string _boschApiKey;
    
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public BoschService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        
        _boschControllerIP = Environment.GetEnvironmentVariable("BOSCH_CONTROLLER_IP") ?? "";
        _boschClientName = Environment.GetEnvironmentVariable("BOSCH_CLIENT_NAME") ?? "MyHomeApp";
        _boschCertificatePath = Environment.GetEnvironmentVariable("BOSCH_CERTIFICATE_PATH") ?? "";
        _boschPassword = Environment.GetEnvironmentVariable("BOSCH_CERTIFICATE_PASSWORD") ?? "";
        _boschIotHubUrl = Environment.GetEnvironmentVariable("BOSCH_IOT_HUB_URL") ?? "https://api.bosch-iot-suite.com/hub/1";
        _boschApiKey = Environment.GetEnvironmentVariable("BOSCH_API_KEY") ?? "";
    }

    public async Task<List<BoschDevice>> GetDevicesAsync()
    {
        var devices = new List<BoschDevice>();

        // Try Bosch Smart Home Controller first
        try
        {
            var shcDevices = await GetSmartHomeControllerDevicesAsync();
            devices.AddRange(shcDevices);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get Bosch Smart Home Controller devices: {ex.Message}");
        }

        // Try Bosch IoT Hub as fallback
        try
        {
            var iotDevices = await GetIoTHubDevicesAsync();
            devices.AddRange(iotDevices);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get Bosch IoT Hub devices: {ex.Message}");
        }

        // If no real devices found, return mock data for demonstration
        if (devices.Count == 0)
        {
            return GetMockBoschDevices();
        }

        return devices;
    }

    private async Task<List<BoschDevice>> GetSmartHomeControllerDevicesAsync()
    {
        if (string.IsNullOrEmpty(_boschControllerIP))
        {
            throw new InvalidOperationException("Bosch Smart Home Controller IP not configured");
        }

        // For Bosch Smart Home Controller, we typically need client certificate authentication
        var devices = new List<BoschDevice>();
        
        try
        {
            var response = await _httpClient.GetAsync($"https://{_boschControllerIP}:8444/smarthome/devices");
            
            if (response.IsSuccessStatusCode)
            {
                var shcResponse = await response.Content.ReadFromJsonAsync<BoschSHCDevicesResponse>();
                
                if (shcResponse?.Result != null)
                {
                    foreach (var device in shcResponse.Result)
                    {
                        devices.Add(new BoschDevice
                        {
                            Id = device.Id,
                            Name = device.Name ?? device.DeviceModel ?? "Unknown Device",
                            DeviceModel = device.DeviceModel,
                            Manufacturer = "Bosch",
                            Status = await GetDeviceStatusAsync(device.Id, DeviceType.SmartHomeController),
                            Type = MapBoschDeviceType(device.DeviceModel),
                            RoomName = device.RoomId != null ? await GetRoomNameAsync(device.RoomId) : "Unknown",
                            LastSeen = DateTime.Now, // SHC doesn't provide last seen in device list
                            Source = DeviceType.SmartHomeController
                        });
                    }
                }
            }
        }
        catch (HttpRequestException)
        {
            // Controller not reachable
            throw new InvalidOperationException("Cannot connect to Bosch Smart Home Controller");
        }

        return devices;
    }

    private async Task<List<BoschDevice>> GetIoTHubDevicesAsync()
    {
        if (string.IsNullOrEmpty(_boschApiKey))
        {
            throw new InvalidOperationException("Bosch IoT Hub API key not configured");
        }

        await EnsureIoTAuthenticatedAsync();

        var devices = new List<BoschDevice>();
        
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        
        var response = await _httpClient.GetAsync($"{_boschIotHubUrl}/devices");
        
        if (response.IsSuccessStatusCode)
        {
            var iotResponse = await response.Content.ReadFromJsonAsync<BoschIoTDevicesResponse>();
            
            if (iotResponse?.Items != null)
            {
                foreach (var device in iotResponse.Items)
                {
                    devices.Add(new BoschDevice
                    {
                        Id = device.ThingId,
                        Name = device.Attributes?.Name ?? device.ThingId,
                        DeviceModel = device.Attributes?.Model ?? "Unknown",
                        Manufacturer = "Bosch",
                        Status = await GetDeviceStatusAsync(device.ThingId, DeviceType.IoTHub),
                        Type = MapBoschDeviceType(device.Attributes?.Model),
                        RoomName = device.Attributes?.Location ?? "Unknown",
                        LastSeen = device.Modified,
                        Source = DeviceType.IoTHub
                    });
                }
            }
        }

        return devices;
    }

    private async Task EnsureIoTAuthenticatedAsync()
    {
        if (_accessToken == null || DateTime.UtcNow >= _tokenExpiry)
        {
            await AuthenticateIoTAsync();
        }
    }

    private async Task AuthenticateIoTAsync()
    {
        var authRequest = new
        {
            apiKey = _boschApiKey,
            grant_type = "client_credentials"
        };

        var response = await _httpClient.PostAsJsonAsync($"{_boschIotHubUrl}/auth/token", authRequest);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<BoschAuthResponse>();
            if (result != null)
            {
                _accessToken = result.AccessToken;
                _tokenExpiry = DateTime.UtcNow.AddSeconds(result.ExpiresIn - 60); // Refresh 1 minute early
            }
        }
    }

    private async Task<BoschDeviceStatus> GetDeviceStatusAsync(string deviceId, DeviceType source)
    {
        try
        {
            string endpoint = source == DeviceType.SmartHomeController
                ? $"https://{_boschControllerIP}:8444/smarthome/devices/{deviceId}/state"
                : $"{_boschIotHubUrl}/devices/{deviceId}/state";

            var response = await _httpClient.GetAsync(endpoint);
            
            if (response.IsSuccessStatusCode)
            {
                var stateData = await response.Content.ReadAsStringAsync();
                
                // Parse common Bosch device states
                return new BoschDeviceStatus
                {
                    IsOnline = true,
                    LastUpdate = DateTime.Now,
                    Properties = ParseDeviceProperties(stateData)
                };
            }
        }
        catch
        {
            // If we can't get status, return offline
        }

        return new BoschDeviceStatus
        {
            IsOnline = false,
            LastUpdate = DateTime.Now,
            Properties = new Dictionary<string, object>()
        };
    }

    private async Task<string> GetRoomNameAsync(string roomId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://{_boschControllerIP}:8444/smarthome/rooms/{roomId}");
            
            if (response.IsSuccessStatusCode)
            {
                var room = await response.Content.ReadFromJsonAsync<BoschRoom>();
                return room?.Name ?? "Unknown Room";
            }
        }
        catch
        {
            // If we can't get room name, return default
        }

        return "Unknown Room";
    }

    private string MapBoschDeviceType(string? deviceModel)
    {
        if (string.IsNullOrEmpty(deviceModel))
            return "Unknown";

        return deviceModel.ToLower() switch
        {
            var model when model.Contains("thermostat") => "Thermostat",
            var model when model.Contains("radiator") => "Smart Radiator Thermostat",
            var model when model.Contains("shutter") => "Smart Shutter Control",
            var model when model.Contains("motion") => "Motion Detector",
            var model when model.Contains("door") => "Door/Window Contact",
            var model when model.Contains("smoke") => "Smoke Detector",
            var model when model.Contains("camera") => "Security Camera",
            var model when model.Contains("switch") => "Smart Switch",
            var model when model.Contains("plug") => "Smart Plug",
            var model when model.Contains("light") => "Smart Light",
            var model when model.Contains("sensor") => "Universal Sensor",
            _ => deviceModel
        };
    }

    private Dictionary<string, object> ParseDeviceProperties(string stateData)
    {
        var properties = new Dictionary<string, object>();
        
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(stateData);
            var root = doc.RootElement;

            // Extract common properties based on Bosch device types
            if (root.TryGetProperty("temperature", out var temp))
                properties["Temperature"] = $"{temp.GetDecimal():F1}°C";
                
            if (root.TryGetProperty("humidity", out var humidity))
                properties["Humidity"] = $"{humidity.GetDecimal():F0}%";
                
            if (root.TryGetProperty("level", out var level))
                properties["Level"] = $"{level.GetDecimal():F0}%";
                
            if (root.TryGetProperty("on", out var isOn))
                properties["Power"] = isOn.GetBoolean() ? "On" : "Off";
                
            if (root.TryGetProperty("operationState", out var opState))
                properties["Operation State"] = opState.GetString() ?? "Unknown";
                
            if (root.TryGetProperty("childProtection", out var childLock))
                properties["Child Lock"] = childLock.GetBoolean() ? "Enabled" : "Disabled";
        }
        catch
        {
            // If parsing fails, add raw data
            properties["Raw State"] = stateData;
        }

        return properties;
    }

    private List<BoschDevice> GetMockBoschDevices()
    {
        return new List<BoschDevice>
        {
            new()
            {
                Id = "bosch-thermostat-01",
                Name = "Living Room Thermostat",
                DeviceModel = "Bosch Smart Thermostat",
                Manufacturer = "Bosch",
                Type = "Thermostat",
                RoomName = "Living Room",
                LastSeen = DateTime.Now.AddMinutes(-5),
                Source = DeviceType.SmartHomeController,
                Status = new BoschDeviceStatus
                {
                    IsOnline = true,
                    LastUpdate = DateTime.Now.AddMinutes(-2),
                    Properties = new Dictionary<string, object>
                    {
                        { "Temperature", "21.5°C" },
                        { "Target Temperature", "22.0°C" },
                        { "Mode", "Heat" },
                        { "Child Lock", "Disabled" }
                    }
                }
            },
            new()
            {
                Id = "bosch-shutter-01",
                Name = "Bedroom Window Shutter",
                DeviceModel = "Bosch Smart Shutter Control",
                Manufacturer = "Bosch",
                Type = "Smart Shutter Control",
                RoomName = "Bedroom",
                LastSeen = DateTime.Now.AddMinutes(-10),
                Source = DeviceType.SmartHomeController,
                Status = new BoschDeviceStatus
                {
                    IsOnline = true,
                    LastUpdate = DateTime.Now.AddMinutes(-3),
                    Properties = new Dictionary<string, object>
                    {
                        { "Level", "75%" },
                        { "Operation State", "Stopped" },
                        { "Child Protection", "Enabled" }
                    }
                }
            },
            new()
            {
                Id = "bosch-motion-01",
                Name = "Hallway Motion Detector",
                DeviceModel = "Bosch Motion Detector",
                Manufacturer = "Bosch",
                Type = "Motion Detector",
                RoomName = "Hallway",
                LastSeen = DateTime.Now.AddMinutes(-1),
                Source = DeviceType.SmartHomeController,
                Status = new BoschDeviceStatus
                {
                    IsOnline = true,
                    LastUpdate = DateTime.Now.AddMinutes(-1),
                    Properties = new Dictionary<string, object>
                    {
                        { "Motion", "No Motion" },
                        { "Battery Level", "85%" },
                        { "Temperature", "19.2°C" }
                    }
                }
            },
            new()
            {
                Id = "bosch-door-01",
                Name = "Front Door Contact",
                DeviceModel = "Bosch Door/Window Contact",
                Manufacturer = "Bosch",
                Type = "Door/Window Contact",
                RoomName = "Entrance",
                LastSeen = DateTime.Now.AddMinutes(-30),
                Source = DeviceType.SmartHomeController,
                Status = new BoschDeviceStatus
                {
                    IsOnline = true,
                    LastUpdate = DateTime.Now.AddMinutes(-30),
                    Properties = new Dictionary<string, object>
                    {
                        { "Contact", "Closed" },
                        { "Battery Level", "92%" }
                    }
                }
            }
        };
    }
}

// Bosch device models and enums
public enum DeviceType
{
    SmartHomeController,
    IoTHub
}

public class BoschDevice
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string DeviceModel { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string Type { get; set; } = "";
    public string RoomName { get; set; } = "";
    public DateTime LastSeen { get; set; }
    public DeviceType Source { get; set; }
    public BoschDeviceStatus Status { get; set; } = new();
}

public class BoschDeviceStatus
{
    public bool IsOnline { get; set; }
    public DateTime LastUpdate { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

// API response models for Bosch Smart Home Controller
public class BoschSHCDevicesResponse
{
    [JsonPropertyName("result")]
    public List<BoschSHCDevice>? Result { get; set; }
}

public class BoschSHCDevice
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("deviceModel")]
    public string? DeviceModel { get; set; }
    
    [JsonPropertyName("roomId")]
    public string? RoomId { get; set; }
}

public class BoschRoom
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

// API response models for Bosch IoT Hub
public class BoschIoTDevicesResponse
{
    [JsonPropertyName("items")]
    public List<BoschIoTDevice>? Items { get; set; }
}

public class BoschIoTDevice
{
    [JsonPropertyName("thingId")]
    public string ThingId { get; set; } = "";
    
    [JsonPropertyName("attributes")]
    public BoschIoTAttributes? Attributes { get; set; }
    
    [JsonPropertyName("_modified")]
    public DateTime Modified { get; set; }
}

public class BoschIoTAttributes
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("model")]
    public string? Model { get; set; }
    
    [JsonPropertyName("location")]
    public string? Location { get; set; }
}

public class BoschAuthResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";
    
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}
