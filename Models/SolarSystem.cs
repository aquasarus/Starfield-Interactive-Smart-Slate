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

        public SolarSystem DeepCopy()
        {
            var solarSystemCopy = new SolarSystem
            {
                SystemID = SystemID,
                SystemName = SystemName,
                SystemLevel = SystemLevel,
                Discovered = Discovered,
                CelestialBodies = CelestialBodies.ConvertAll(celestialBody => celestialBody.DeepCopy())
            };

            CelestialBody parentPlanet = null;
            foreach (var celestialBody in solarSystemCopy.CelestialBodies)
            {
                if (!celestialBody.IsMoon)
                {
                    parentPlanet = celestialBody;
                }
                else
                {
                    parentPlanet.AddMoon(celestialBody);
                }
            }

            return solarSystemCopy;
        }
    }
}
