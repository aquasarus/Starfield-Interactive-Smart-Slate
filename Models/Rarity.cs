namespace Starfield_Interactive_Smart_Slate.Models
{
    public enum Rarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Exotic = 3,
        Unique = 4
    }

    public static class RarityExtensions
    {
        public static string GetDiamonds(this Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Common: return "";
                case Rarity.Uncommon: return "⬩";
                case Rarity.Rare: return "⬩⬩";
                case Rarity.Exotic: return "⬩⬩⬩";
                case Rarity.Unique: return "⬩⬩⬩⬩";
            }
            return null; // unreachable
        }
    }
}
