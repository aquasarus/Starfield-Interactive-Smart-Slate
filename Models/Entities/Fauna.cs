namespace Starfield_Interactive_Smart_Slate.Models.Entities
{
    public class Fauna : LifeformEntity
    {
        public override LifeformType LifeformType
        {
            get { return LifeformType.Fauna; }
        }

        public Fauna DeepCopy()
        {
            return new Fauna
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
