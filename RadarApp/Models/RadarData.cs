using System;

namespace RadarApp.Models
{
    public class RadarData
    {
        public string City { get; set; }
        public string Time { get; set; }
        public string Location { get; set; }
        public DateTime? PageDate { get; set; }
        public RadarCoordinate Coordinate { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int? SpeedLimit { get; set; }
        public bool IsActiveAt(TimeSpan currentTime)
        {
            if (Time == "INFO" || Time == "GREŠKA") return true;
            try
            {
                var parts = Time.Split(new[] { " do " }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2) return false;

                if (!TimeSpan.TryParse(parts[0].Trim(), out TimeSpan startTime)) return false;
                if (!TimeSpan.TryParse(parts[1].Trim(), out TimeSpan endTime)) return false;

                return currentTime >= startTime && currentTime <= endTime;
            }
            catch { return false; }
        }
        public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;
    }
}