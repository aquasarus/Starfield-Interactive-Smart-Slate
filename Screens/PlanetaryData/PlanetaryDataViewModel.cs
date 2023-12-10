using CommunityToolkit.Mvvm.ComponentModel;
using Starfield_Interactive_Smart_Slate.Models;
using Starfield_Interactive_Smart_Slate.Models.Entities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace Starfield_Interactive_Smart_Slate.Screens.PlanetaryData
{
    public class PlanetaryDataViewModel : ObservableObject
    {
        public static PlanetaryDataViewModel Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new PlanetaryDataViewModel();
                }

                return instance;
            }
        }

        public List<SolarSystem> DisplayedSolarSystems
        {
            get => displayedSolarSystems;
            set => SetProperty(ref displayedSolarSystems, value);
        }

        // this is used for the lifeform edit combo box, because Terrormorphs have a scanned resource of None
        public List<Resource> SelectableOrganicResources
        {
            get => selectableOrganicResources;
            set => SetProperty(ref selectableOrganicResources, value);
        }

        public CelestialBody? DisplayedCelestialBody
        {
            get => displayedCelestialBody;
            set => SetProperty(ref displayedCelestialBody, value);
        }

        public CelestialBody? SelectedCelestialBody
        {
            get => selectedCelestialBody;
            set => SetProperty(ref selectedCelestialBody, value);
        }

        public Entity? DisplayedEntity
        {
            get => displayedEntity;
            set => SetProperty(ref displayedEntity, value);
        }

        public Entity? SelectedEntity
        {
            get => selectedEntity;
            set => SetProperty(ref selectedEntity, value);
        }

        public int PictureGridColumns
        {
            get => pictureGridColumns;
            set => SetProperty(ref pictureGridColumns, value);
        }

        private static PlanetaryDataViewModel? instance;
        private MainViewModel mainViewModel = MainViewModel.Instance;
        private List<SolarSystem> displayedSolarSystems;
        private List<Resource> selectableOrganicResources;
        private CelestialBody? displayedCelestialBody;
        private CelestialBody? selectedCelestialBody;
        private Entity? selectedEntity;
        private Entity? displayedEntity;

        private int pictureGridColumns;

        private PlanetaryDataViewModel()
        {
            LoadSelectableResources();
            mainViewModel.PropertyChanged += HandlePropertyChanged;
        }

        // find solar systems where solar system name or a child celestial body name matches filterText
        // ignores case, whitespace, symbols
        public void FilterSolarSystems(string filterText)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(DisplayedSolarSystems);

            if (string.IsNullOrWhiteSpace(filterText))
            {
                view.Filter = null;
            }
            else
            {
                filterText = new string(filterText.Where(char.IsLetter).ToArray()).ToLower();
                view.Filter = item =>
                {
                    if (item is SolarSystem solarSystem)
                    {
                        return new string(solarSystem.SystemName.Where(char.IsLetter).ToArray()).ToLower().Contains(filterText)
                        || solarSystem.CelestialBodies.Where(
                            cb => new string(cb.BodyName.Where(char.IsLetter).ToArray()).ToLower().Contains(filterText)).Any();
                    }
                    return false;
                };
            }
        }

        private void LoadSelectableResources()
        {
            if (mainViewModel.AllResources != null)
            {
                SelectableOrganicResources = mainViewModel.AllResources.Where(r =>
                {
                    return r.GetType() == ResourceType.Organic || r.GetType() == ResourceType.Placeholders;
                })
                .OrderBy(r => r.FullName)
                .ToList();

                // add placeholder to allow un-setting lifeform resource
                SelectableOrganicResources.Insert(
                    0, new Resource(-1, ResourceType.Placeholders, "Unknown", null, Rarity.Common));
            }
        }

        private void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(mainViewModel.AllResources):
                    LoadSelectableResources();
                    break;
                case nameof(mainViewModel.DiscoveredSolarSystems):
                    if (DisplayedSolarSystems == null) // initialize
                    {
                        DisplayedSolarSystems = mainViewModel.DiscoveredSolarSystems;
                    }
                    break;
            }
        }
    }
}
