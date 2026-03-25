namespace RadarApp.Models
{
    public enum RadarGroupPosition { Header, Only, First, Middle, Last }

    public class RadarFlatItem
    {
        public RadarGroupPosition Position { get; set; }
        public bool IsHeader => Position == RadarGroupPosition.Header;
        public bool IsRadarItem => !IsHeader;
        public bool ShowTopBorder => Position == RadarGroupPosition.Only || Position == RadarGroupPosition.First;
        public bool ShowBottomBorder => Position == RadarGroupPosition.Only || Position == RadarGroupPosition.Last;
        public bool IsMiddleItem => Position == RadarGroupPosition.Middle;
        public bool IsFirstInGroup => Position == RadarGroupPosition.First;
        public bool IsLastInGroup => Position == RadarGroupPosition.Last;
        public string CityName { get; set; }
        public string Time { get; set; }
        public string Location { get; set; }
        
        public Microsoft.Maui.Thickness ItemMargin =>
            Position == RadarGroupPosition.Last || Position == RadarGroupPosition.Only
                ? new Microsoft.Maui.Thickness(0, 0, 0, 10)
                : new Microsoft.Maui.Thickness(0);
    }
}