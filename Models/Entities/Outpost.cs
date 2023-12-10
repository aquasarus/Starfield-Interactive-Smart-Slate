using System.Collections.ObjectModel;

namespace Starfield_Interactive_Smart_Slate.Models.Entities
{
    public class Outpost : Entity
    {
        public override string SubtitleLabel => "· Outpost";
        public override bool SupportsResources => false;

        public Outpost()
        {
            Pictures = new ObservableCollection<Picture> { new Picture() };
        }

        public Outpost DeepCopy()
        {
            return new Outpost
            {
                ID = ID,
                Name = Name,
                Notes = Notes,
                Pictures = Pictures
            };
        }
    }
}
