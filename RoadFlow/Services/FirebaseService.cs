using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using RoadFlow.Models;
using System.Text;

namespace RoadFlow.Services
{
    public class FirebaseService
    {
        public const string FirebaseBaseUrl= $"{Secrets.FirebaseBaseUrl}radari-sbk";
        private readonly string _firebaseApiKey = Secrets.FirebaseApiKey;
        private readonly HttpClient _httpClient;
        private string _cachedToken = null;

        public FirebaseService()
        {
            _httpClient = new HttpClient();
        }
        
        private async Task<string> GetAuthTokenAsync()
        {
            if (!string.IsNullOrEmpty(_cachedToken)) return _cachedToken;

            try
            {
                var authUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_firebaseApiKey}";
                var response = await _httpClient.PostAsync(authUrl,
                    new StringContent("{\"returnSecureToken\":true}", Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    _cachedToken = doc.RootElement.GetProperty("idToken").GetString();
                    return _cachedToken;
                }
            }
            catch { }

            return null;
        }

        private async Task<string> GetAuthenticatedUrl(string path)
        {
            var token = await GetAuthTokenAsync();
            string separator = path.Contains("?") ? "&" : "?";
            return $"{path}{separator}auth={token}";
        }
        public async Task<List<RadarData>> GetFirebaseRadarsAsync(DateTime date)
        {
            var radars = new List<RadarData>();
            string dateStr = date.ToString("yyyy-MM-dd");
            string url = await GetAuthenticatedUrl($"{FirebaseBaseUrl}/{dateStr}.json");

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return radars;

                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json) || json == "null") return radars;

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<Dictionary<string, List<FirebaseRadarItem>>>(json, options);

                if (data != null)
                {
                    foreach (var cityKv in data)
                    {
                        string cityName = cityKv.Key;
                        var configLoc = RadarConfig.Locations.FirstOrDefault(l => l.Name == cityName && l.FromFirebase);
                        if (configLoc == null) continue;

                        foreach (var item in cityKv.Value)
                        {
                            var locationPart = NormalizeFirebaseLocation(item.Location);
                            
                            var radar = new RadarData
                            {
                                City = cityName,
                                Time = item.Time,
                                Location = locationPart,
                                PageDate = date
                            };

                            if (configLoc.MapEnabled)
                            {
                                var coords = RadarConfig.FindCoordinatesByName(locationPart);
                                if (coords.Any())
                                {
                                    var first = coords.First();
                                    radar.Coordinate = first;
                                    radar.Latitude = first.Latitude;
                                    radar.Longitude = first.Longitude;
                                    radar.SpeedLimit = first.SpeedLimit;
                                }
                            }
                            radars.Add(radar);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FirebaseService] Error: {ex.Message}");
            }

            return radars;
        }

        private string NormalizeFirebaseLocation(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;
            return raw.Trim();
        }
    }

}