using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Starfield_Interactive_Smart_Slate.Models.Entities
{
    public class LifeformEntity : Entity, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public List<Resource> PrimaryDrops { get; set; }
        public List<Resource> SecondaryDrops { get; set; } // TODO: not yet hooked up with UI

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

                if (!string.IsNullOrWhiteSpace(Notes))
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

        public LifeformEntity()
        {
            Pictures = new ObservableCollection<Picture> { new Picture() };
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

        public override void AddPicture(Picture picture)
        {
            base.AddPicture(picture);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SurveyedString)));
        }
    }
}
