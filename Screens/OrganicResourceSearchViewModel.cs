using Starfield_Interactive_Smart_Slate.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Starfield_Interactive_Smart_Slate.Screens
{
    public class OrganicResourceSearchViewModel : INotifyPropertyChanged
    {
        public IEnumerable<SolarSystem> OrganicSearchResult { get; set; }

        public CelestialBody? SelectedCelestialBody { get; set; }

        public CelestialBody? DisplayedCelestialBody { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        private static OrganicResourceSearchViewModel? instance;
        private MainViewModel mainViewModel = MainViewModel.Instance;
        private Resource currentSearch;

        private OrganicResourceSearchViewModel()
        {
            mainViewModel.SolarSystemsUpdated += HandleSolarSystemsUpdated;
        }

        public static OrganicResourceSearchViewModel Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new OrganicResourceSearchViewModel();
                }

                return instance;
            }
        }

        public void SearchCelestialBodiesForResource(Resource resource)
        {
            currentSearch = resource;
            OrganicSearchResult = mainViewModel.DiscoveredSolarSystems.Select(
                solarSystem =>
                {
                    // make shallow copies to allow separate instances of Show and GrayOut properties
                    var solarSystemCopy = solarSystem.Copy();

                    // chaining .Select here ended up with new instances of moons for some reason
                    // so I have to loop it manually
                    foreach (var celestialBody in solarSystemCopy.CelestialBodies)
                    {
                        var lifeformResult = celestialBody.GetLifeformsWithResource(resource);
                        var found = lifeformResult.Item1;
                        var faunaList = lifeformResult.Item2;
                        var floraList = lifeformResult.Item3;

                        if (found)
                        {
                            celestialBody.Faunas = faunaList;
                            celestialBody.Floras = floraList;
                            celestialBody.Show = true;
                            celestialBody.GrayOut = false;
                        }
                        else
                        {
                            celestialBody.Show = false;
                        }
                    }

                    foreach (var celestialBody in solarSystemCopy.CelestialBodies)
                    {
                        if (!celestialBody.Show)
                        {
                            if (celestialBody.Moons?.Any(moon => moon.Show) ?? false)
                            {
                                // gray out the parent planet if its surface doesn't actually contain the resource
                                celestialBody.Show = true;
                                celestialBody.GrayOut = true;
                            }
                        }
                    }

                    solarSystemCopy.CelestialBodies = solarSystemCopy.CelestialBodies.Where(
                        celestialBody => celestialBody.Show
                    ).ToList();

                    return solarSystemCopy;
                }
            ).Where(solarSystem => solarSystem.CelestialBodies.Any());

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OrganicSearchResult)));
        }

        public void ClearSelections(bool propagateDisplay = true)
        {
            SelectCelestialBody(null, propagateDisplay);
        }

        public void SelectCelestialBody(CelestialBody? celestialBody, bool propagateDisplay = true)
        {
            SelectedCelestialBody = celestialBody;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedCelestialBody)));

            if (propagateDisplay)
            {
                DisplayCelestialBody(celestialBody);
            }
        }

        public void DisplayCelestialBody(CelestialBody? celestialBody)
        {
            DisplayedCelestialBody = celestialBody;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayedCelestialBody)));
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
