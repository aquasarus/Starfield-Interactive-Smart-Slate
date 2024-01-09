using CommunityToolkit.Mvvm.ComponentModel;
using Starfield_Interactive_Smart_Slate.Models.Entities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Starfield_Interactive_Smart_Slate.Models
{
    public class CelestialBody : ObservableObject
    {
        // TODO: most of these should probably be "set once only" properties
        public int BodyID { get; set; }

        public string BodyName { get; set; }

        // be careful with this circular reference! it should only be used as a helper pointer.
        public SolarSystem ParentSystem { get; set; }

        public bool IsMoon { get; set; }

        public string BodyType { get; set; }

        public double Gravity { get; set; }

        public string Temperature { get; set; }

        public string Atmosphere { get; set; }

        public string Magnetosphere { get; set; }

        public string Water { get; set; }

        public int TotalFauna { get; set; }

        public int TotalFlora { get; set; }

        public List<Resource>? Resources { get; set; }

        public ObservableCollection<Fauna>? Faunas
        {
            get => faunas;
            set
            {
                SetProperty(ref faunas, value);

                if (faunas != null)
                {
                    faunas.CollectionChanged += (sender, e) =>
                    {
                        OnPropertyChanged(nameof(LifeformProgress));
                        OnPropertyChanged(nameof(CanAddFauna));
                        OnPropertyChanged(nameof(OverviewString));
                    };
                }
            }
        }

        public ObservableCollection<Flora>? Floras
        {
            get => floras;
            set
            {
                SetProperty(ref floras, value);

                if (floras != null)
                {
                    floras.CollectionChanged += (sender, e) =>
                    {
                        OnPropertyChanged(nameof(LifeformProgress));
                        OnPropertyChanged(nameof(CanAddFlora));
                        OnPropertyChanged(nameof(OverviewString));
                    };
                }
            }
        }

        public ObservableCollection<Outpost>? Outposts
        {
            get => outposts;
            set
            {
                SetProperty(ref outposts, value);

                if (outposts != null)
                {
                    outposts.CollectionChanged += (sender, e) =>
                    {
                        OnPropertyChanged(nameof(HasOutpost));
                    };
                }
            }
        }

        #region ---- helper attributes to display resource search ----
        public List<CelestialBody>? Moons;

        public bool Show { get; set; }

        public bool GrayOut { get; set; }
        #endregion

        public string? LifeformProgress
        {
            get
            {
                if (TotalFauna > 0 || TotalFlora > 0)
                {
                    int surveyedFaunas = Faunas?.Where(fauna => fauna.IsSurveyed).Count() ?? 0;
                    int faunasPoints = surveyedFaunas + (Faunas?.Count ?? 0);
                    int surveyedFloras = Floras?.Where(flora => flora.IsSurveyed).Count() ?? 0;
                    int florasPoints = surveyedFloras + (Floras?.Count ?? 0);
                    double surveyPercent = 100.0 * (faunasPoints + florasPoints) / ((TotalFauna + TotalFlora) * 2);
                    return $"🧬 ({surveyPercent:F0}%)";
                }
                else
                {
                    return null;
                }
            }
        }

        public string ResourcesString
        {
            get
            {
                if (Resources != null)
                {
                    return $"{string.Join("\n", Resources.Select(r =>
                    {
                        return r.PrettifiedName;
                    }))}";
                }
                else
                {
                    return "None";
                }
            }
        }

        public bool HasOutpost
        {
            get { return Outposts != null && Outposts.Count > 0; }
        }

        public bool HasLifeform
        {
            get { return TotalFauna > 0 || TotalFlora > 0; }
        }

        public bool CanAddFauna
        {
            get
            {
                if (App.Current.UserSettings.UnlockLifeformCounts)
                {
                    return true;
                }

                return !((Faunas?.Count ?? 0) >= TotalFauna);
            }
        }

        public bool CanAddFlora
        {
            get
            {
                if (App.Current.UserSettings.UnlockLifeformCounts)
                {
                    return true;
                }

                return !((Floras?.Count ?? 0) >= TotalFlora);
            }
        }

        public string OverviewString
        {
            get
            {
                string overviewString = $"Type: {BodyType}\n" +
                $"Gravity: {Gravity}\n" +
                $"Temperature: {Temperature}\n" +
                $"Atmosphere: {Atmosphere}\n" +
                $"Magnetosphere: {Magnetosphere}\n" +
                $"Fauna: {GetFaunaCountString()}\n" +
                $"Flora: {GetFloraCountString()}\n" +
                $"Water: {Water}";

                return overviewString;
            }
        }

        private ObservableCollection<Fauna>? faunas;
        private ObservableCollection<Flora>? floras;
        private ObservableCollection<Outpost>? outposts;

        public override bool Equals(object? obj)
        {
            if (obj is CelestialBody)
            {
                return BodyID == ((CelestialBody)obj).BodyID;
            }
            else
            {
                return false;
            }
        }

        public CelestialBody Copy()
        {
            return new CelestialBody
            {
                BodyID = BodyID,
                BodyName = BodyName,
                ParentSystem = ParentSystem,
                IsMoon = IsMoon,
                BodyType = BodyType,
                Gravity = Gravity,
                Temperature = Temperature,
                Atmosphere = Atmosphere,
                Magnetosphere = Magnetosphere,
                Water = Water,
                TotalFauna = TotalFauna,
                TotalFlora = TotalFlora,
                Resources = Resources,
                Faunas = Faunas,
                Floras = Floras,
                Outposts = Outposts,
                // Moons will be handled by the SolarSystem for now
                //Moons = Moons?.ConvertAll(moon => moon.DeepCopy()),
                Show = Show,
                GrayOut = GrayOut
            };
        }

        public void AddFauna(Fauna fauna)
        {
            // initialize and set up binding dependencies
            if (Faunas == null)
            {
                Faunas = new ObservableCollection<Fauna>();
            }

            Faunas.Add(fauna);
        }

        public void AddFlora(Flora flora)
        {
            // initialize and set up binding dependencies
            if (Floras == null)
            {
                Floras = new ObservableCollection<Flora>();
            }

            Floras.Add(flora);
        }

        public void AddOutpost(Outpost outpost)
        {
            // initialize and set up binding dependencies
            if (Outposts == null)
            {
                Outposts = new ObservableCollection<Outpost>();
            }

            Outposts.Add(outpost);
        }

        public void DeleteOutpost(Outpost deletedOutpost)
        {
            foreach (var outpost in Outposts)
            {
                if (outpost.ID == deletedOutpost.ID)
                {
                    Outposts.Remove(outpost);
                    break;
                }
            }
        }

        public void EditFauna(Fauna editedFauna)
        {
            foreach (var fauna in Faunas)
            {
                if (fauna.ID == editedFauna.ID)
                {
                    Faunas[Faunas.IndexOf(fauna)] = editedFauna;
                    break;
                }
            }
            OnPropertyChanged(nameof(LifeformProgress));
        }

        public void EditFlora(Flora editedFlora)
        {
            foreach (var flora in Floras)
            {
                if (flora.ID == editedFlora.ID)
                {
                    Floras[Floras.IndexOf(flora)] = editedFlora;
                    break;
                }
            }
            OnPropertyChanged(nameof(LifeformProgress));
        }

        public void EditOutpost(Outpost editedOutpost)
        {
            foreach (var outpost in Outposts)
            {
                if (outpost.ID == editedOutpost.ID)
                {
                    Outposts[Outposts.IndexOf(outpost)] = editedOutpost;
                    break;
                }
            }
        }

        public void AddMoon(CelestialBody celestialBody)
        {
            if (Moons == null)
            {
                Moons = new List<CelestialBody>();
            }

            Moons.Add(celestialBody);
        }

        // TODO: maybe delete this if SurfaceContainsResources() makes this obsolete
        public bool SurfaceContainsResource(Resource resource)
        {
            return Resources?.Contains(resource) ?? false;
        }

        // check if surface contains all input resources
        public bool SurfaceContainsResources(IEnumerable<Resource> resources)
        {
            if (resources == null || resources.Count() == 0)
            {
                return false;
            }

            foreach (var resource in resources)
            {
                if (Resources == null || !Resources.Contains(resource))
                {
                    return false;
                }
            }

            return true;
        }

        public (bool, ObservableCollection<Fauna>?, ObservableCollection<Flora>?) GetLifeformsWithResource(Resource resource)
        {
            if (TotalFauna == 0 && TotalFlora == 0)
            {
                return (false, null, null); // short circuit for celestial bodies with no life
            }

            var found = false;

            var faunaList = Faunas?.Where(
                fauna =>
                {
                    if (fauna.PrimaryDrops?.Any(drop => drop.Equals(resource)) ?? false)
                    {
                        found = true;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            ).ToList();

            ObservableCollection<Fauna> faunaCollection = null;
            if (faunaList != null)
            {
                faunaCollection = new ObservableCollection<Fauna>(faunaList);
            }

            var floraList = Floras?.Where(
                flora =>
                {
                    if (flora.PrimaryDrops?.Any(drop => drop.Equals(resource)) ?? false)
                    {
                        found = true;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            ).ToList();

            ObservableCollection<Flora> floraCollection = null;
            if (floraList != null)
            {
                floraCollection = new ObservableCollection<Flora>(floraList);
            }

            return (found, faunaCollection, floraCollection);
        }

        public void NotifyLifeformUnlockChanged()
        {
            OnPropertyChanged(nameof(CanAddFauna));
            OnPropertyChanged(nameof(CanAddFlora));
        }

        private string GetFaunaCountString()
        {
            if ((Faunas?.Count ?? 0 + TotalFauna) == 0)
            {
                return "None";
            }
            else
            {
                return $"{Faunas?.Count ?? 0}/{TotalFauna}";
            }
        }

        private string GetFloraCountString()
        {
            if ((Floras?.Count ?? 0 + TotalFlora) == 0)
            {
                return "None";
            }
            else
            {
                return $"{Floras?.Count ?? 0}/{TotalFlora}";
            }
        }
    }
}
