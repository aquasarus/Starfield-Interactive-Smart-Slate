namespace Starfield_Interactive_Smart_Slate.Models.Entities
{
    public class Flora : LifeformEntity
    {
        public override LifeformType LifeformType => LifeformType.Flora;

        public override string SubtitleLabel => "· Flora";

        public Flora DeepCopy()
        {
            return new Flora
            {
                ID = ID,
                Name = Name,
                Notes = Notes,
                IsFarmable = IsFarmable,
                PrimaryDrops = PrimaryDrops?.ConvertAll(drop => drop.DeepCopy()),
                SecondaryDrops = SecondaryDrops?.ConvertAll(drop => drop.DeepCopy()),
                Pictures = Pictures
            };
        }
    }
}
