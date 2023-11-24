using System.Collections.ObjectModel;
using System.Linq;

namespace Starfield_Interactive_Smart_Slate.Models.Entities
{
    public class Fauna : LifeformEntity
    {
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
                ID = ID,
                Name = Name,
                Notes = Notes,
                PrimaryDrops = PrimaryDrops?.ConvertAll(drop => drop.DeepCopy()),
                SecondaryDrops = SecondaryDrops?.ConvertAll(drop => drop.DeepCopy()),
                Pictures = pictureCollection
            };
        }
    }
}
