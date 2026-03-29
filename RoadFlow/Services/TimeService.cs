using System.Text.Json;

namespace RoadFlow.Services
{
    public class TimeService
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public async Task<DateTime> GetCurrentDateAsync()
        {

            try
            {
                var response = await _httpClient.GetStringAsync(
                    "https://timeapi.io/api/time/current/zone?timeZone=Europe/Sarajevo");
                using var doc = JsonDocument.Parse(response);
                var year = doc.RootElement.GetProperty("year").GetInt32();
                var month = doc.RootElement.GetProperty("month").GetInt32();
                var day = doc.RootElement.GetProperty("day").GetInt32();
                return new DateTime(year, month, day);
            }
            catch { }

            try
            {
                var response = await _httpClient.GetStringAsync(
                    "https://worldtimeapi.org/api/timezone/Europe/Sarajevo");
                using var doc = JsonDocument.Parse(response);
                var datetimeStr = doc.RootElement.GetProperty("datetime").GetString();
                return DateTime.Parse(datetimeStr).Date;
            }
            catch { }

            return DateTime.Today;
        }
    }
}