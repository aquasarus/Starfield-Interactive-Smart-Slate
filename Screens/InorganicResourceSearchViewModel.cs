using Starfield_Interactive_Smart_Slate.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Starfield_Interactive_Smart_Slate.Screens
{
    public class InorganicResourceSearchViewModel : INotifyPropertyChanged
    {
        public IEnumerable<SolarSystem> InorganicSearchResult { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        private static InorganicResourceSearchViewModel? instance;
        private MainViewModel mainViewModel = MainViewModel.Instance;
        private Resource currentSearch;

        private InorganicResourceSearchViewModel()
        {
            mainViewModel.SolarSystemsUpdated += HandleSolarSystemsUpdated;
        }

        public static InorganicResourceSearchViewModel Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new InorganicResourceSearchViewModel();
                }

                return instance;
            }
        }

        public void SearchCelestialBodiesForResource(Resource resource)
        {
            currentSearch = resource;
            InorganicSearchResult = mainViewModel.DiscoveredSolarSystems.Select(
                solarSystem =>
                {
                    // make shallow copies to allow separate instances of Show and GrayOut properties
                    var solarSystemCopy = solarSystem.Copy();

                    solarSystemCopy.CelestialBodies = solarSystemCopy.CelestialBodies.Select(
                        celestialBody =>
                        {
                            var surfaceHasResource = celestialBody.SurfaceContainsResource(resource);
                            var moonsHaveResource = celestialBody.Moons?.Any(moon => moon.SurfaceContainsResource(resource)) ?? false;

                            // if celestial body (or any of its moons) contains the resource, include it in the list
                            if (surfaceHasResource || moonsHaveResource)
                            {
                                // gray out the parent planet if its surface doesn't actually contain the resource
                                celestialBody.GrayOut = !surfaceHasResource;
                                celestialBody.Show = true;
                            }
                            else
                            {
                                celestialBody.Show = false;
                            }

                            return celestialBody;
                        }
                    )
                    .Where(celestialBody => celestialBody.Show)
                    .ToList();
                    return solarSystemCopy;
                }
            ).Where(solarSystem => solarSystem.CelestialBodies.Any());

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InorganicSearchResult)));
        }

        private void HandleSolarSystemsUpdated(object? sender, EventArgs e)
        {
            // refresh existing resource search, if applicable
            if (currentSearch != null)
            {
                SearchCelestialBodiesForResource(currentSearch);
            }
        }
    }
}
