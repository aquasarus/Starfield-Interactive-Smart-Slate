namespace Starfield_Interactive_Smart_Slate.Models
{
    public class Resource
    {
        private int resourceID;
        private ResourceType resourceType;
        private string resourceName;
        private string? resourceShortName;
        private Rarity resourceRarity;

        public int ResourceID { get { return resourceID; } }
        public string FullName { get { return GetFullName(); } }
        public string PrettifiedName { get { return $"{GetFullName()} {resourceRarity.GetDiamonds()}"; } }
        public string RarityString { get { return $"{resourceRarity.ToString()} {resourceRarity.GetDiamonds()}"; } }
        public string ShortName { get { return resourceShortName; } }

        public Resource(int resourceID, ResourceType resourceType, string resourceName, string? resourceShortName, Rarity resourceRarity)
        {
            this.resourceID = resourceID;
            this.resourceType = resourceType;
            this.resourceName = resourceName;
            this.resourceShortName = resourceShortName;
            this.resourceRarity = resourceRarity;
        }

        public Resource DeepCopy()
        {
            return new Resource(resourceID, resourceType, resourceName, resourceShortName, resourceRarity);
        }

        public ResourceType GetType()
        {
            return resourceType;
        }

        public Rarity GetResourceRarity()
        {
            return resourceRarity;
        }

        public override string ToString()
        {
            return PrettifiedName;
        }

        public override bool Equals(object? obj)
        {
            if (obj is Resource)
            {
                return resourceID == ((Resource)obj).resourceID;
            }
            else
            {
                return false;
            }
        }

        private string GetFullName()
        {
            if (resourceShortName != null)
            {
                return $"{resourceName} ({resourceShortName})";
            }
            else
            {
                return resourceName;
            }
        }
    }
}
