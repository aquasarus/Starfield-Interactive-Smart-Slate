using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Starfield_Interactive_Smart_Slate.Models.Entities
{
    public class Flora : INotifyPropertyChanged
    {
        public int FloraID { get; set; }
        public string FloraName { get; set; }
        public string FloraNotes { get; set; }
        public List<Resource> PrimaryDrops { get; set; }
        public List<Resource> SecondaryDrops { get; set; } // TODO: not yet hooked up with UI
        public ObservableCollection<Picture>? Pictures { get; set; }

        public bool IsSurveyed
        {
            get
            {
                return (PrimaryDrops?.Count ?? 0) > 0 ? true : false;
            }
        }
        public string? SurveyedString
        {
            get
            {
                var icons = "";

                if (!string.IsNullOrWhiteSpace(FloraNotes))
                {
                    icons += "📝";
                }

                if ((Pictures?.Count ?? 0) > 1)
                {
                    if (icons != "") { icons += " "; }
                    icons += "📷";
                }

                if (IsSurveyed)
                {
                    if (icons != "") { icons += " "; }
                    icons += "✓";
                }

                return icons == "" ? null : icons;
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
                if (string.IsNullOrWhiteSpace(FloraNotes))
                {
                    return "None";
                }
                else
                {
                    return FloraNotes;
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public Flora()
        {
            Pictures = new ObservableCollection<Picture> { new Picture() };
        }

        public Flora DeepCopy(bool fast = false)
        {
            ObservableCollection<Picture> pictureCollection = null;
            // skip picture copying in fast mode
            if (!fast)
            {
                if (Pictures != null)
                {
                    pictureCollection = new ObservableCollection<Picture>(
                        Pictures.Select(picture => picture.DeepCopy())
                    );
                }
            }

            return new Flora
            {
                FloraID = FloraID,
                FloraName = FloraName,
                FloraNotes = FloraNotes,
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
