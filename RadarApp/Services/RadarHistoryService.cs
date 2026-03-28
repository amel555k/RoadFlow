using System.Text;
using System.Text.Json;
using RadarApp.Models;

namespace RadarApp.Services;

public class RadarHistoryService
{
    private readonly string _baseUrl = Secrets.FirebaseBaseUrl;
    private readonly string _firebaseApiKey = Secrets.FirebaseApiKey;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private string _cachedToken = null;

    public RadarHistoryService()
    {
        _httpClient = new HttpClient();
        _jsonOptions = new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        };
    }

    private async Task<string> GetAuthTokenAsync()
    {
        if (!string.IsNullOrEmpty(_cachedToken)) return _cachedToken;

        try
        {
            var authUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_firebaseApiKey}";
            var response = await _httpClient.PostAsync(authUrl, new StringContent("{\"returnSecureToken\":true}", Encoding.UTF8, "application/json"));
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                _cachedToken = doc.RootElement.GetProperty("idToken").GetString();
                return _cachedToken;
            }
        }
        catch {}
        
        return null;
    }
    private async Task<string> GetAuthenticatedUrl(string path)
    {
        var token = await GetAuthTokenAsync();
        string separator = path.Contains("?") ? "&" : "?";
        return $"{_baseUrl}{path}{separator}auth={token}";
    }

    public async Task<bool> CheckIfTodayExistsAsync()
    {
        try
        {
            string dateKey = DateTime.Today.ToString("yyyy-MM-dd");
            var url = await GetAuthenticatedUrl($"history/{dateKey}.json");
            var response = await _httpClient.GetStringAsync(url);
            
            return !string.IsNullOrEmpty(response) && response != "null";
        }
        catch { return false; }
    }

    public async Task<List<DateTime>> GetAvailableDatesAsync()
    {
        try
        {
            var url = await GetAuthenticatedUrl("history.json?shallow=true");
            var response = await _httpClient.GetStringAsync(url);
            
            if (string.IsNullOrEmpty(response) || response == "null") 
                return new List<DateTime>();

            var datesDict = JsonSerializer.Deserialize<Dictionary<string, bool>>(response, _jsonOptions);
            var result = new List<DateTime>();

            if (datesDict == null) return result;

            foreach (var key in datesDict.Keys)
            {
                if (DateTime.TryParseExact(key, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var date))
                    result.Add(date);
            }
            return result.OrderByDescending(d => d).ToList();
        }
        catch { return new List<DateTime>(); }
    }

    public async Task<List<RadarData>> LoadRadarsForDateAsync(DateTime date)
    {
        try
        {
            string dateKey = date.ToString("yyyy-MM-dd");
            var url = await GetAuthenticatedUrl($"history/{dateKey}.json");
            var response = await _httpClient.GetStringAsync(url);

            if (string.IsNullOrEmpty(response) || response == "null") 
                return new List<RadarData>();

            var data = JsonSerializer.Deserialize<Dictionary<string, List<RadarData>>>(response, _jsonOptions);
            var result = new List<RadarData>();

            if (data == null) return result;

            foreach (var entry in data)
            {
                foreach (var radar in entry.Value)
                {
                    radar.City = entry.Key;
                    result.Add(radar);
                }
            }
            return result;
        }
        catch { return new List<RadarData>(); }
    }

    public async Task SaveRadarsForDateAsync(DateTime date, List<RadarData> radars)
    {
        try
        {
            if (radars == null || !radars.Any()) return;

            string dateKey = date.ToString("yyyy-MM-dd");
            var url = await GetAuthenticatedUrl($"history/{dateKey}.json");

            var groupedData = radars
                .GroupBy(r => r.City)
                .ToDictionary(
                    g => g.Key, 
                    g => g.Select(r => new { r.Time, r.Location }).ToList()
                );

            var json = JsonSerializer.Serialize(groupedData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            await _httpClient.PutAsync(url, content);
        }
        catch { }
    }
}