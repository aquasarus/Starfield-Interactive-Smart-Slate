using System.Collections.Generic;

namespace Starfield_Interactive_Smart_Slate.Models
{
    public class Fauna
    {
        public int FaunaID { get; set; }
        public string FaunaName { get; set; }
        public string FaunaNotes { get; set; }
        public List<Resource>? PrimaryDrops { get; set; }
        public List<Resource>? SecondaryDrops { get; set; } // TODO: not yet hooked up with UI
        public bool IsSurveyed
        {
            get
            {
                return (PrimaryDrops?.Count ?? 0) > 0 ? true : false;
            }
        }
        public string SurveyedString
        {
            get
            {
                return IsSurveyed ? "✓" : null;
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
        public string NotesString
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FaunaNotes))
                {
                    return "None";
                }
                else
                {
                    return FaunaNotes;
                }
            }
        }

        public Fauna DeepCopy()
        {
            return new Fauna
            {
                FaunaID = FaunaID,
                FaunaName = FaunaName,
                FaunaNotes = FaunaNotes,
                PrimaryDrops = PrimaryDrops?.ConvertAll(drop => drop.DeepCopy()),
                SecondaryDrops = SecondaryDrops?.ConvertAll(drop => drop.DeepCopy())
            };
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
    }
}
