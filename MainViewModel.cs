using Starfield_Interactive_Smart_Slate.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Starfield_Interactive_Smart_Slate
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event EventHandler? SolarSystemsUpdated;

        public List<Resource> InorganicResources { get; set; }

        public List<Resource> OrganicResources { get; set; }

        // call OnSolarSystemsUpdated() when updating these
        public List<SolarSystem> AllSolarSystems;
        public List<SolarSystem> DiscoveredSolarSystems;

        public event PropertyChangedEventHandler? PropertyChanged;

        private static MainViewModel? instance;

        private MainViewModel() { }

        public static MainViewModel Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MainViewModel();
                }

                return instance;
            }
        }

        public void ReloadAllData()
        {
            // load all resources
            var resources = DataRepository.GetResources();
            InorganicResources = resources.Where(r => r.GetType() == ResourceType.Inorganic).ToList();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InorganicResources)));

            OrganicResources = resources.Where(r => r.GetType() == ResourceType.Organic).ToList();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OrganicResources)));

            // load all solar systems
            var solarSystems = DataRepository.GetSolarSystems();
            AllSolarSystems = solarSystems;
            DiscoveredSolarSystems = solarSystems
                .Where(solarSystem => solarSystem.Discovered)
                .ToList();

            OnSolarSystemsUpdated(); // notify other view models
        }

        private void OnSolarSystemsUpdated()
        {
            SolarSystemsUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}
