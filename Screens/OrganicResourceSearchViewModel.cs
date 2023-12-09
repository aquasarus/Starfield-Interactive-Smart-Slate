using CommunityToolkit.Mvvm.ComponentModel;
using Starfield_Interactive_Smart_Slate.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Starfield_Interactive_Smart_Slate.Screens
{
    public class OrganicResourceSearchViewModel : ObservableObject
    {
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

        public IEnumerable<SolarSystem> OrganicSearchResult
        {
            get => organicSearchResult;
            set => SetProperty(ref organicSearchResult, value);
        }

        public CelestialBody? SelectedCelestialBody
        {
            get => selectedCelestialBody;
            set => SetProperty(ref selectedCelestialBody, value);
        }

        public CelestialBody? DisplayedCelestialBody
        {
            get => displayedCelestialBody;
            set => SetProperty(ref displayedCelestialBody, value);
        }

        private static OrganicResourceSearchViewModel? instance;
        private MainViewModel mainViewModel = MainViewModel.Instance;

        private Resource currentSearch;
        private IEnumerable<SolarSystem> organicSearchResult;
        private CelestialBody? selectedCelestialBody;
        private CelestialBody? displayedCelestialBody;

        private OrganicResourceSearchViewModel()
        {
            mainViewModel.PropertyChanged += HandlePropertyChanged;
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
        }

        public void ClearSelections(bool propagateDisplay = true)
        {
            SelectCelestialBody(null, propagateDisplay);
        }

        public void SelectCelestialBody(CelestialBody? celestialBody, bool propagateDisplay = true)
        {
            SelectedCelestialBody = celestialBody;

            if (propagateDisplay)
            {
                DisplayCelestialBody(celestialBody);
            }
        }

        public void DisplayCelestialBody(CelestialBody? celestialBody)
        {
            DisplayedCelestialBody = celestialBody;
        }

        private void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // refresh existing resource search, if applicable
            if (e.PropertyName == nameof(MainViewModel.Instance.DiscoveredSolarSystems))
            {
                if (currentSearch != null)
                {
                    SearchCelestialBodiesForResource(currentSearch);
                }
            }
        }
    }
}
