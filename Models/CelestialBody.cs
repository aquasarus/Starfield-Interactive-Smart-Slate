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
                $"Fauna: {getFaunaCountString()}\n" +
                $"Flora: {getFloraCountString()}\n" +
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

        public void AddFauna(Fauna fauna)
        {
            if (Faunas == null)
            {
                Faunas = new ObservableCollection<Fauna>();
            }
            Faunas.Add(fauna);
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedBodyName)));
        }

        public void AddFlora(Flora flora)
        {
            if (Floras == null)
            {
                Floras = new ObservableCollection<Flora>();
            }
            Floras.Add(flora);
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedBodyName)));
        }

        public void EditFauna(Fauna editedFauna)
        {
            foreach(var fauna in Faunas)
            {
                if (fauna.FaunaID == editedFauna.FaunaID)
                {
                    Faunas[Faunas.IndexOf(fauna)] = editedFauna;
                    break;
                }
            }
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedBodyName)));
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
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedBodyName)));
        }

        private string getFaunaCountString()
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
        private string getFloraCountString()
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
