namespace Starfield_Interactive_Smart_Slate.Models.Entities
{
    public class Flora : LifeformEntity
    {
        public Flora DeepCopy()
        {
            return new Flora
            {
                ID = ID,
                Name = Name,
                Notes = Notes,
                PrimaryDrops = PrimaryDrops?.ConvertAll(drop => drop.DeepCopy()),
                SecondaryDrops = SecondaryDrops?.ConvertAll(drop => drop.DeepCopy()),
                Pictures = Pictures
            };
        }
    }
}
