namespace RadarApp.Models
{
    public class RadarCoordinate
    {
        public string MainName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int SpeedLimit { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public bool Stacionaran { get; set; } = false;
    }
}