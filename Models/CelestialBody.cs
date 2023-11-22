using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Starfield_Interactive_Smart_Slate.Models
{
    public class CelestialBody : INotifyPropertyChanged
    {
        public int BodyID { get; set; }
        public string BodyName { get; set; }
        public string SystemName { get; set; }
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
        public ObservableCollection<Fauna>? Faunas { get; set; }
        public ObservableCollection<Flora>? Floras { get; set; }

        // helper attributes to display resource search
        public List<CelestialBody>? Moons;
        public bool Show { get; set; }
        public bool GrayOut { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public string FormattedBodyName
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
                    return $"{BodyName} \U0001f9ec ({surveyPercent:F0}%)";
                }
                else
                {
                    return BodyName;
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

        public override string ToString()
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

        public CelestialBody DeepCopy(bool fast = false)
        {
            ObservableCollection<Fauna> faunaCollection = null;
            if (Faunas != null)
            {
                faunaCollection = new ObservableCollection<Fauna>(
                    Faunas.Select(fauna => fauna.DeepCopy(fast))
                );
            }

            ObservableCollection<Flora> floraCollection = null;
            if (Floras != null)
            {
                floraCollection = new ObservableCollection<Flora>(
                    Floras.Select(flora => flora.DeepCopy(fast))
                );
            }

            return new CelestialBody
            {
                BodyID = BodyID,
                BodyName = BodyName,
                SystemName = SystemName,
                IsMoon = IsMoon,
                BodyType = BodyType,
                Gravity = Gravity,
                Temperature = Temperature,
                Atmosphere = Atmosphere,
                Magnetosphere = Magnetosphere,
                Water = Water,
                TotalFauna = TotalFauna,
                TotalFlora = TotalFlora,
                Resources = Resources?.ConvertAll(resource => resource.DeepCopy()),
                Faunas = faunaCollection,
                Floras = floraCollection,
                // Moons will be handled by the SolarSystem for now
                //Moons = Moons?.ConvertAll(moon => moon.DeepCopy()),
                Show = Show,
                GrayOut = GrayOut
            };
        }

        public void AddFauna(Fauna fauna)
        {
            if (Faunas == null)
            {
                Faunas = new ObservableCollection<Fauna>();
            }
            Faunas.Add(fauna);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedBodyName)));
        }

        public void AddFlora(Flora flora)
        {
            if (Floras == null)
            {
                Floras = new ObservableCollection<Flora>();
            }
            Floras.Add(flora);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedBodyName)));
        }

        public void EditFauna(Fauna editedFauna)
        {
            foreach (var fauna in Faunas)
            {
                if (fauna.FaunaID == editedFauna.FaunaID)
                {
                    Faunas[Faunas.IndexOf(fauna)] = editedFauna;
                    break;
                }
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedBodyName)));
        }

        public void EditFlora(Flora editedFlora)
        {
            foreach (var flora in Floras)
            {
                if (flora.FloraID == editedFlora.FloraID)
                {
                    Floras[Floras.IndexOf(flora)] = editedFlora;
                    break;
                }
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedBodyName)));
        }

        public void AddMoon(CelestialBody celestialBody)
        {
            if (Moons == null)
            {
                Moons = new List<CelestialBody>();
            }

            Moons.Add(celestialBody);
        }

        public bool SurfaceContainsResource(Resource resource)
        {
            return Resources?.Contains(resource) ?? false;
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
