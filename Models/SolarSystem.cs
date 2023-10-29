using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Starfield_Interactive_Smart_Slate.Models
{
    public class SolarSystem
    {
        public int SystemID { get; set; }
        public string SystemName { get; set; }
        public int SystemLevel { get; set; }
        public bool Discovered { get; set; }
        public OrderedDictionary CelestialBodiesBuilder { get; set; }
        public List<CelestialBody> CelestialBodies { get; set; }

        public void BuildList()
        {
            CelestialBodies = CelestialBodiesBuilder.Values.OfType<CelestialBody>().ToList();
        }
    }
}
