using CommunityToolkit.Mvvm.ComponentModel;
using Starfield_Interactive_Smart_Slate.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

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

        // this is used for the lifeform edit combo box, because Terrormorphs have a scanned resource of None
        public List<Resource> SelectableOrganicResources
        {
            get => selectableOrganicResources;
            set => SetProperty(ref selectableOrganicResources, value);
        }

        private static PlanetaryDataViewModel? instance;
        private MainViewModel mainViewModel = MainViewModel.Instance;
        private List<Resource> selectableOrganicResources;

        private PlanetaryDataViewModel()
        {
            LoadSelectableResources();
            mainViewModel.PropertyChanged += HandlePropertyChanged;
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
            if (e.PropertyName == nameof(mainViewModel.AllResources))
            {
                LoadSelectableResources();
            }
        }
    }
}
