namespace RadarApp;

public class CityGroupViewModel
{
    public string CityName { get; set; }
    public List<RadarItemViewModel> Radars { get; set; } = new();
}
