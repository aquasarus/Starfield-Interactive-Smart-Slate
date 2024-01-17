using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Starfield_Interactive_Smart_Slate.Models.Entities
{
    public abstract class LifeformEntity : Entity, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public abstract LifeformType LifeformType { get; }

        public override bool SupportsResources => true;

        public List<Resource> PrimaryDrops { get; set; }

        public List<Resource> SecondaryDrops { get; set; } // TODO: not yet hooked up with UI

        public bool IsFarmable { get; set; } // TODO: update this if we want to support multiple PrimaryDrops/SecondaryDrops

        public bool IsSurveyed
        {
            get
            {
                return (PrimaryDrops?.Count ?? 0) > 0 ? true : false;
            }
        }

        public string? IconsString
        {
            get
            {
                var icons = "";

                if (IsFarmable)
                {
                    if (this is Fauna)
                    {
                        icons += "🐄";
                    }
                    else
                    {
                        icons += "🥕";
                    }
                }

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

        // like IconsString, but only include icons relevant for resource search
        public string? IconsStringRelevantForSearch
        {
            get
            {
                var icons = "";

                if (IsFarmable)
                {
                    if (this is Fauna)
                    {
                        icons += "🐄";
                    }
                    else
                    {
                        icons += "🥕";
                    }
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IconsString)));
        }

        public override void RemovePicture(Picture picture)
        {
            base.RemovePicture(picture);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IconsString)));
        }
    }
}
