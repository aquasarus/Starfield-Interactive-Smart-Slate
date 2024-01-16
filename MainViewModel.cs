using CommunityToolkit.Mvvm.ComponentModel;
using Starfield_Interactive_Smart_Slate.Models;
using Starfield_Interactive_Smart_Slate.Models.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Path = System.IO.Path;

namespace Starfield_Interactive_Smart_Slate
{
    public class MainViewModel : ObservableObject
    {
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

        public List<Resource> InorganicResources
        {
            get => inorganicResources;
            set => SetProperty(ref inorganicResources, value);
        }

        public List<Resource> OrganicResources
        {
            get => organicResources;
            set => SetProperty(ref organicResources, value);
        }

        public List<Resource> AllResources
        {
            get => allResources;
            set => SetProperty(ref allResources, value);
        }

        public List<SolarSystem> AllSolarSystems
        {
            get => allSolarSystems;
            set => SetProperty(ref allSolarSystems, value);
        }

        public List<SolarSystem> DiscoveredSolarSystems
        {
            get => discoveredSolarSystems;
            set => SetProperty(ref discoveredSolarSystems, value);
        }

        private static MainViewModel? instance;
        private List<Resource> allResources;
        private List<Resource> inorganicResources;
        private List<Resource> organicResources;
        private List<SolarSystem> allSolarSystems;
        private List<SolarSystem> discoveredSolarSystems;

        // map of language -> map of fauna/flora -> map of lowercase name -> actual lifeform name
        private Dictionary<string, Dictionary<LifeformType, Dictionary<string, string>>> lifeformNames;

        private MainViewModel() { }

        public void ReloadAllData()
        {
            // load all resources
            var resources = DataRepository.GetResources();
            AllResources = resources;
            InorganicResources = resources.Where(r => r.GetType() == ResourceType.Inorganic).ToList();
            OrganicResources = resources.Where(r => r.GetType() == ResourceType.Organic).ToList();

            // load all solar systems
            var solarSystems = DataRepository.GetSolarSystems();
            AllSolarSystems = solarSystems;
            DiscoveredSolarSystems = solarSystems
                .Where(solarSystem => solarSystem.Discovered)
                .ToList();

            // load all lifeform names
            lifeformNames = new Dictionary<string, Dictionary<LifeformType, Dictionary<string, string>>>();
            var defaultNames = DataRepository.GetLifeformNames();
            lifeformNames.Add("English", defaultNames);

            // load other languages
            // TODO: once we add a lot of languages, load upon user selection instead
            var frenchLifeformNames = new Dictionary<LifeformType, Dictionary<string, string>>();
            frenchLifeformNames[LifeformType.Fauna] = new Dictionary<string, string>();
            frenchLifeformNames[LifeformType.Flora] = new Dictionary<string, string>();
            lifeformNames.Add("French", frenchLifeformNames);

            var folderPath = AppDomain.CurrentDomain.BaseDirectory;
            folderPath = Path.Combine(folderPath, "Localization");
            folderPath = Path.Combine(folderPath, "fr");

            try
            {
                var frenchFaunasFile = Path.Combine(folderPath, "translated_french_fauna_names.txt");

                if (File.Exists(frenchFaunasFile))
                {
                    var frenchFaunasList = File.ReadAllLines(frenchFaunasFile).ToList();

                    // build auto-complete dictionary
                    foreach (var faunaName in frenchFaunasList)
                    {
                        var autocompleteMatcher = faunaName.ToLower();
                        frenchLifeformNames[LifeformType.Fauna][autocompleteMatcher] = faunaName;
                    }
                }
                else
                {
                    throw new Exception($"{frenchFaunasFile} not found!");
                }
            }
            catch (IOException e)
            {
                MessageBox.Show("An error occurred while reading language files: " + e.Message);
            }

            try
            {
                var frenchFlorasFile = Path.Combine(folderPath, "translated_french_flora_names.txt");

                if (File.Exists(frenchFlorasFile))
                {
                    var frenchFlorasList = File.ReadAllLines(frenchFlorasFile).ToList();

                    // build auto-complete dictionary
                    foreach (var floraName in frenchFlorasList)
                    {
                        var autocompleteMatcher = floraName.ToLower();
                        frenchLifeformNames[LifeformType.Flora][autocompleteMatcher] = floraName;
                    }
                }
                else
                {
                    throw new Exception($"{frenchFlorasFile} not found!");
                }
            }
            catch (IOException e)
            {
                MessageBox.Show("An error occurred while reading language files: " + e.Message);
            }
        }

        public Dictionary<string, string> GetLifeformNames(LifeformType type)
        {
            var language = App.Current.UserSettings.Language;
            return lifeformNames[language][type];
        }

        public void DiscoverSolarSystem(SolarSystem solarSystem)
        {
            solarSystem.Discovered = true;
            DiscoveredSolarSystems = AllSolarSystems
                .Where(solarSystem => solarSystem.Discovered)
                .ToList();
            OnPropertyChanged(nameof(DiscoveredSolarSystems));
        }
    }
}
