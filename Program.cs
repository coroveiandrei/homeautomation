using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeAutomation.Services;
using DotNetEnv;

// Load environment variables from .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddHttpClient();
builder.Services.AddSingleton<SmartThingsService>();
builder.Services.AddTransient<SolarmanService>();
builder.Services.AddSingleton<HomeConnectService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();

// API endpoints
app.MapGet("/", async (HttpContext context) =>
{
    var html = """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>SmartThings Home Automation</title>
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
    <style>
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 20px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); min-height: 100vh; }
        .container { max-width: 1200px; margin: 0 auto; background: white; border-radius: 15px; box-shadow: 0 10px 30px rgba(0,0,0,0.2); overflow: hidden; }
        .header { background: linear-gradient(45deg, #2196F3, #21CBF3); color: white; padding: 30px; text-align: center; }
        .header h1 { margin: 0; font-size: 2.5em; font-weight: 300; }
        .content { padding: 30px; }
        .device { background: #f8f9fa; border-radius: 10px; padding: 20px; margin-bottom: 20px; border-left: 5px solid #2196F3; transition: transform 0.2s; }
        .device:hover { transform: translateY(-2px); box-shadow: 0 5px 15px rgba(0,0,0,0.1); }
        .device.bosch { border-left-color: #4CAF50; }
        .device.smartthings { border-left-color: #2196F3; }
        .device-header { font-size: 1.3em; font-weight: 600; color: #333; margin-bottom: 10px; display: flex; justify-content: space-between; align-items: center; }
        .device-source { font-size: 0.8em; background: #e0e0e0; color: #666; padding: 4px 8px; border-radius: 12px; }
        .device-source.bosch { background: #e8f5e8; color: #388e3c; }
        .device-source.smartthings { background: #e3f2fd; color: #1976d2; }
        .device-info { color: #666; margin-bottom: 15px; }
        .device-room { color: #888; font-size: 0.9em; margin-bottom: 10px; }
        .device-status { margin-bottom: 10px; }
        .device-online { color: #4CAF50; font-weight: 500; }
        .device-offline { color: #f44336; font-weight: 500; }
        .capability { background: white; padding: 10px 15px, 5px 15px; margin: 5px 0; border-radius: 5px; display: flex; justify-content: space-between; align-items: center; }
        .capability-name { font-weight: 500; color: #555; }
        .capability-value { background: #e3f2fd; color: #1976d2; padding: 4px 12px; border-radius: 15px; font-weight: 500; }
        .loading { text-align: center; padding: 50px; color: #666; font-size: 1.2em; }
        .error { background: #ffebee; color: #c62828; padding: 20px; border-radius: 10px; margin: 20px; border-left: 5px solid #f44336; }
        .refresh-btn { background: #2196F3; color: white; border: none; padding: 12px 24px; border-radius: 25px; cursor: pointer; font-size: 1em; transition: background 0.3s; }
        .refresh-btn:hover { background: #1976D2; }
        .solar-section { margin-top: 30px; padding: 20px; background: #f8f9fa; border-radius: 10px; }
        .chart-container { position: relative; height: 400px; margin-top: 20px; }
        .smartthings-btn { background: #1976D2; color: white; border: none; padding: 12px 24px; border-radius: 25px; cursor: pointer; font-size: 1em; margin-left: 10px; text-decoration: none; transition: background 0.3s; }
        .smartthings-btn:hover { background: #0d47a1; }
        .homeconnect-btn:hover { background: #d84315; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>🏠 Smart Home Automation</h1>
            <button class="refresh-btn" onclick="loadDevices()">🔄 Refresh Devices</button>
            <a href="/api/homeconnect/login" class="homeconnect-btn">🔑 Login with Home Connect</a>
            <a href="/api/smartthings/login" class="smartthings-btn">🔑 Connect to SmartThings</a>
        </div>
        <div class="content">
            <div class="solar-section">
                <h2>☀️ Solar Production Today</h2>
                <div class="chart-container">
                    <canvas id="solarChart"></canvas>
                </div>
            </div>
            <div id="devices" class="loading">Loading devices...</div>
        </div>
    </div>

    <script>
        let solarChart;
        
        async function loadSolarData() {
            try {
                const response = await fetch('/api/solar/today');
                const data = await response.json();
                
                if (solarChart) {
                    solarChart.destroy();
                }
                
                const ctx = document.getElementById('solarChart').getContext('2d');
                solarChart = new Chart(ctx, {
                    type: 'line',
                    data: {
                        labels: data.labels,
                        datasets: [{
                            label: 'Solar Production (kW)',
                            data: data.values,
                            borderColor: '#FFA726',
                            backgroundColor: 'rgba(255, 167, 38, 0.1)',
                            borderWidth: 2,
                            fill: true,
                            tension: 0.4
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        scales: {
                            y: {
                                beginAtZero: true,
                                title: {
                                    display: true,
                                    text: 'Power (kW)'
                                }
                            },
                            x: {
                                title: {
                                    display: true,
                                    text: 'Time'
                                }
                            }
                        },
                        plugins: {
                            legend: {
                                display: true,
                                position: 'top'
                            }
                        }
                    }
                });
            } catch (error) {
                console.error('Error loading solar data:', error);
                document.getElementById('solarChart').innerHTML = '<div class="error">Error loading solar data</div>';
            }
        }
        
        async function loadDevices() {
            const devicesDiv = document.getElementById('devices');
            devicesDiv.innerHTML = '<div class="loading">Loading devices...</div>';
            
            try {
                const response = await fetch('/api/devices');
                const data = await response.json();
                
                console.log('API Response:', data); // Debug log
                
                // Ensure we have an array
                const devices = Array.isArray(data) ? data : [];
                
                if (devices.length === 0) {
                    devicesDiv.innerHTML = '<div class="error">No devices found.</div>';
                    return;
                }

                devicesDiv.innerHTML = devices.map(device => {
                    const sourceClass = device.source?.toLowerCase() === 'bosch' ? 'bosch' : 'smartthings';
                    const sourceIcon = device.source?.toLowerCase() === 'bosch' ? '🔧' : '📱';
                    const manufacturerIcon = device.manufacturer === 'Bosch' ? '🔧' : device.manufacturer === 'Samsung' ? '📱' : '🏠';
                    
                    return `
                        <div class="device ${sourceClass}">
                            <div class="device-header">
                                <span>${manufacturerIcon} ${device.label || device.name}</span>
                                <span class="device-source ${sourceClass}">${device.source || 'Unknown'}</span>
                            </div>
                            ${device.roomName ? `<div class="device-room">📍 ${device.roomName}</div>` : ''}
                            ${device.isOnline !== undefined ? `<div class="device-status ${device.isOnline ? 'device-online' : 'device-offline'}">
                                ${device.isOnline ? '🟢 Online' : '🔴 Offline'}
                            </div>` : ''}
                            ${device.lastSeen ? `<div class="device-info">Last seen: ${new Date(device.lastSeen).toLocaleString()}</div>` : ''}
                            <div class="capabilities">
                                ${device.capabilities && device.capabilities.length > 0 ? device.capabilities.map(cap => `
                                    <div class="capability">
                                        <span class="capability-name">${cap.name}</span>
                                        <span class="capability-value">${cap.value}</span>
                                    </div>
                                `).join('') : '<div class="capability"><span class="capability-name">No capabilities available</span></div>'}
                            </div>
                        </div>
                    `;
                }).join('');
            } catch (error) {
                console.error('Error:', error); // Debug log
                devicesDiv.innerHTML = `<div class="error">Error loading devices: ${error.message}</div>`;
            }
        }
        
        // Load data when page loads
        loadSolarData();
        loadDevices();
        
        // Auto-refresh every 30 seconds
        setInterval(() => {
            loadSolarData();
            loadDevices();
        }, 30000);
    </script>
</body>
</html>
""";
    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync(html);
});

app.MapGet("/api/devices", async (SmartThingsService smartThingsService, HomeConnectService homeConnectService) =>
{
    try
    {
        var allDevices = new List<object>();
        try
        {
            // Get SmartThings devices
            var smartThingsDevices = await smartThingsService.GetDevicesWithStatusAsync();
            allDevices.AddRange(smartThingsDevices.Select(d => new
            {
                deviceId = d.DeviceId,
                name = d.Name,
                label = d.Label,
                deviceTypeName = d.DeviceTypeName,
                capabilities = d.Capabilities,
                source = "SmartThings",
                manufacturer = "Samsung"
            }));
        }
        catch (Exception ex)
        {
            // Log SmartThings errors if needed
            Console.WriteLine($"Error fetching SmartThings devices: {ex.Message}");
        }
        try
        {
            var appliancesJson = await homeConnectService.GetAppliancesAsync();
            if (!string.IsNullOrEmpty(appliancesJson))
            {
                dynamic? appliancesObj = Newtonsoft.Json.JsonConvert.DeserializeObject(appliancesJson);
                var appliances = appliancesObj?.data?.homeappliances;
                if (appliances != null)
                {
                    foreach (var appliance in appliances)
                    {
                        allDevices.Add(new
                        {
                            deviceId = (string?)appliance.haId ?? "",
                            name = (string?)appliance.name ?? "",
                            label = (string?)appliance.brand ?? "",
                            deviceTypeName = (string?)appliance.type ?? "",
                            capabilities = new List<object>(), // Home Connect API: add if needed
                            source = "HomeConnect",
                            manufacturer = (string?)appliance.brand ?? "Home Connect"
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log SmartThings errors if needed
            Console.WriteLine($"Error fetching SmartThings devices: {ex.Message}");
        }

        return Results.Json(allDevices);
    }
    catch (Exception)
    {
        return Results.Json(new List<object>());
    }
});

app.MapGet("/api/solar/today", async (SolarmanService solarmanService) =>
{
    try
    {
        var data = await solarmanService.GetTodayProductionAsync();
        return Results.Json(data);
    }
    catch (Exception)
    {
        return Results.Json(new { labels = new string[0], values = new double[0] });
    }
});

app.MapPost("/api/devices/{deviceId}/command", async (string deviceId, DeviceCommandRequest request, SmartThingsService smartThingsService) =>
{
    try
    {
        var result = await smartThingsService.SendCommandAsync(deviceId, request.Capability, request.Command, request.Arguments);
        return Results.Ok(new { success = result });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error sending command: {ex.Message}");
    }
});

// Home Connect Authorization Code Flow Endpoints
app.MapGet("/api/homeconnect/login", (HttpContext context, HomeConnectService homeConnectService) =>
{
    // Set your redirect URI (must match what is registered in Home Connect dev portal)
    var redirectUri = "https://localhost:55272/api/homeconnect/callback";
    var url = homeConnectService.GetAuthorizationUrl(redirectUri, "IdentifyAppliance ApplianceSettings" /* add scopes as needed */);
    context.Response.Redirect(url);
    return Task.CompletedTask;
});

app.MapGet("/api/homeconnect/callback", async (HttpContext context, HomeConnectService homeConnectService) =>
{
    var code = context.Request.Query["code"].ToString();
    var error = context.Request.Query["error"].ToString();
    if (!string.IsNullOrEmpty(error))
    {
        await context.Response.WriteAsync($"<h2>Authorization failed: {error}</h2>");
        return;
    }
    if (string.IsNullOrEmpty(code))
    {
        await context.Response.WriteAsync("<h2>No authorization code received.</h2>");
        return;
    }
    var redirectUri = "https://localhost:55272/api/homeconnect/callback";
    var success = await homeConnectService.ExchangeAuthorizationCodeAsync(code, redirectUri);
    if (success)
        await context.Response.WriteAsync("<h2>Authorization successful! You may now use Home Connect features.</h2>");
    else
        await context.Response.WriteAsync("<h2>Failed to obtain access token.</h2>");
});

// SmartThings Authorization Code Flow Endpoints
app.MapGet("/api/smartthings/login", (HttpContext context, SmartThingsService smartThingsService) =>
{
    // Set your redirect URI (must match what is registered in SmartThings dev portal)
    var redirectUri = "https://85efd086bd29.ngrok-free.app/api/smartthings/callback";
    // var redirectUri = "localhost";
    var url = smartThingsService.GetAuthorizationUrl(redirectUri, "r:devices:*");
    context.Response.Redirect(url);
    return Task.CompletedTask;
});

app.MapGet("/api/smartthings/callback", async (HttpContext context, SmartThingsService smartThingsService) =>
{
    var code = context.Request.Query["code"].ToString();
    var error = context.Request.Query["error"].ToString();
    if (!string.IsNullOrEmpty(error))
    {
        await context.Response.WriteAsync($"<h2>Authorization failed: {error}</h2>");
        return;
    }
    if (string.IsNullOrEmpty(code))
    {
        await context.Response.WriteAsync("<h2>No authorization code received.</h2>");
        return;
    }
    var redirectUri = "https://85efd086bd29.ngrok-free.app/api/smartthings/callback";
    var success = await smartThingsService.ExchangeAuthorizationCodeAsync(code, redirectUri);
    if (success)
        await context.Response.WriteAsync("<h2>SmartThings authorization successful! You may now use SmartThings features.</h2>");
    else
        await context.Response.WriteAsync("<h2>Failed to obtain SmartThings access token.</h2>");
});

app.Run();

// Data models for API requests
public class DeviceCommandRequest
{
    public string Capability { get; set; } = "";
    public string Command { get; set; } = "";
    public List<object>? Arguments { get; set; }
}
