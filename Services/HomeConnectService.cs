using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HomeAutomation.Services;

public class HomeConnectService
{
    private readonly HttpClient _httpClient;
    private string? _accessToken;
    private const string DeviceCodeEndpoint = "https://api.home-connect.com/security/oauth/device_authorization";
    private const string TokenEndpoint = "https://api.home-connect.com/security/oauth/token";
    private readonly string clientId = "6C0CF95D82BDC2042AE653F7C6871CB44B41076BDEDD45B522FA296BFD90FEB7";
    private readonly string clientSecret = "3062316D23266130D5081391FDDF18300584E784635EFDB48E43FE5C8177C626";

    public HomeConnectService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> AuthenticateAsync(string accessToken)
    {
        _accessToken = accessToken;
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
        return true;
    }

    public async Task<dynamic?> GetAppliancesAsync()
    {
        if (string.IsNullOrEmpty(_accessToken)) throw new InvalidOperationException("Not authenticated");
        var resp = await _httpClient.GetAsync("https://api.home-connect.com/api/homeappliances");
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync();
    }

    public async Task<bool> StartProgramAsync(string haid, string programJson)
    {
        if (string.IsNullOrEmpty(_accessToken)) throw new InvalidOperationException("Not authenticated");
        var content = new StringContent(programJson, Encoding.UTF8, "application/vnd.bsh.sdk.v1+json");
        var resp = await _httpClient.PutAsync($"https://api.home-connect.com/api/homeappliances/{haid}/programs/active", content);
        return resp.IsSuccessStatusCode;
    }

    // Generates the Home Connect authorization URL for the user to log in
    public string GetAuthorizationUrl(string redirectUri, string scope = "IdentifyAppliance")
    {
        var url = $"https://api.home-connect.com/security/oauth/authorize?response_type=code&client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}";
        return url;
    }

    // Exchanges the authorization code for an access token
    public async Task<bool> ExchangeAuthorizationCodeAsync(string code, string redirectUri)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret
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
}

public class DeviceCodeResponse
{
    [JsonProperty("device_code")]
    public string DeviceCode { get; set; } = "";
    [JsonProperty("user_code")]
    public string UserCode { get; set; } = "";
    [JsonProperty("verification_uri")]
    public string VerificationUri { get; set; } = "";
    [JsonProperty("expires_in")]
    public int ExpiresIn { get; set; }
    [JsonProperty("interval")]
    public int Interval { get; set; }
}
