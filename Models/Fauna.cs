using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Starfield_Interactive_Smart_Slate.Models
{
    public class Fauna : INotifyPropertyChanged
    {
        public int FaunaID { get; set; }
        public string FaunaName { get; set; }
        public string FaunaNotes { get; set; }
        public List<Resource>? PrimaryDrops { get; set; }
        public List<Resource>? SecondaryDrops { get; set; } // TODO: not yet hooked up with UI
        public ObservableCollection<Picture>? Pictures { get; set; }
        public bool IsSurveyed
        {
            get
            {
                return (PrimaryDrops?.Count ?? 0) > 0 ? true : false;
            }
        }
        public string SurveyedString
        {
            get
            {
                if (IsSurveyed && Pictures.Count > 1)
                {
                    return "📷 ✓";
                }
                else if (IsSurveyed)
                {
                    return "✓";
                }
                else if (Pictures.Count > 1)
                {
                    return "📷";
                }
                else
                {
                    return null;
                }
            }
        }
        public string ResourceString
        {
            get
            {
                if ((PrimaryDrops?.Count ?? 0) == 0)
                {
                    return "Unknown";
                }
                else
                {
                    return PrimaryDrops[0].PrettifiedName;
                }
            }
        }
        public string NotesString
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FaunaNotes))
                {
                    return "None";
                }
                else
                {
                    return FaunaNotes;
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public Fauna()
        {
            Pictures = new ObservableCollection<Picture> { new Picture() };
        }

        public Fauna DeepCopy(bool fast = false)
        {
            ObservableCollection<Picture> pictureCollection = null;
            if (!fast)
            {
                if (Pictures != null)
                {
                    pictureCollection = new ObservableCollection<Picture>(
                        Pictures.Select(picture => picture.DeepCopy())
                    );
                }
            }

            return new Fauna
            {
                FaunaID = FaunaID,
                FaunaName = FaunaName,
                FaunaNotes = FaunaNotes,
                PrimaryDrops = PrimaryDrops?.ConvertAll(drop => drop.DeepCopy()),
                SecondaryDrops = SecondaryDrops?.ConvertAll(drop => drop.DeepCopy()),
                Pictures = pictureCollection
            };
        }

        public void AddPrimaryDrop(Resource primaryDrop)
        {
            if (PrimaryDrops == null)
            {
                PrimaryDrops = new List<Resource>();
            }
            PrimaryDrops.Add(primaryDrop);
        }

        public void AddSecondaryDrop(Resource secondaryDrop)
        {
            if (SecondaryDrops == null)
            {
                SecondaryDrops = new List<Resource>();
            }
            SecondaryDrops.Add(secondaryDrop);
        }

        public void AddPicture(Picture picture)
        {
            Pictures.Insert(Pictures.Count - 1, picture);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SurveyedString)));
        }
    }
}
