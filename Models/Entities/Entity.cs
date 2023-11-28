using System.Collections.ObjectModel;

namespace Starfield_Interactive_Smart_Slate.Models.Entities
{
    public abstract class Entity
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Notes { get; set; }
        public ObservableCollection<Picture>? Pictures { get; set; }

        public string NotesString
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Notes))
                {
                    return "None";
                }
                else
                {
                    return Notes;
                }
            }
        }

        public virtual void AddPicture(Picture picture)
        {
            Pictures.Insert(Pictures.Count - 1, picture);
        }

        public virtual void RemovePicture(Picture picture)
        {
            Pictures.Remove(picture);
        }
    }
}
