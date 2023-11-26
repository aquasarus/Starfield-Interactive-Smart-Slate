using System.Collections.ObjectModel;
using System.Linq;

namespace Starfield_Interactive_Smart_Slate.Models.Entities
{
    public class Outpost : Entity
    {
        public Outpost()
        {
            Pictures = new ObservableCollection<Picture> { new Picture() };
        }

        public Outpost DeepCopy(bool fast = false)
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

            return new Outpost
            {
                ID = ID,
                Name = Name,
                Notes = Notes,
                Pictures = pictureCollection
            };
        }
    }
}
