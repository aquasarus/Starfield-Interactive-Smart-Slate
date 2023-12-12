using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public ObservableCollection<CelestialBody> ShownCelestialBodies { get; set; }

        public void BuildList()
        {
            CelestialBodies = CelestialBodiesBuilder.Values.OfType<CelestialBody>().ToList();
        }

        public void ResetShownCelestialBodies()
        {
            // reset observable collection
            if (ShownCelestialBodies == null)
            {
                ShownCelestialBodies = new ObservableCollection<CelestialBody>();
            }
            else
            {
                ShownCelestialBodies.Clear();
            }
        }

        public void ShowAllCelestialBodies()
        {
            ResetShownCelestialBodies();

            // add each celestial body without gray out property
            foreach (var celestialBody in CelestialBodies)
            {
                celestialBody.GrayOut = false;
                ShownCelestialBodies.Add(celestialBody);
            }
        }

        public SolarSystem Copy()
        {
            var solarSystemCopy = new SolarSystem
            {
                SystemID = SystemID,
                SystemName = SystemName,
                SystemLevel = SystemLevel,
                Discovered = Discovered,
                CelestialBodies = CelestialBodies.ConvertAll(celestialBody => celestialBody.Copy())
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
