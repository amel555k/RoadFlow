using System.Collections.Generic;

namespace RadarApp.Models
{
    public class RadarLocation
    {
        public string Name { get; set; }
        public List<int> PossibleIds { get; set; }
        public Canton Canton {get;set;}
        public bool MapEnabled{get;set;} = false;
    }
}