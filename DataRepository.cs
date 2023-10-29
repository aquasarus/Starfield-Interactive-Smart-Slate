using Starfield_Interactive_Smart_Slate.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Starfield_Interactive_Smart_Slate
{
    public class DataRepository
    {
        public static readonly string connectionString = $"Data Source={DatabaseInitializer.UserDatabasePath()};Version=3;";

        public static SQLiteConnection CreateConnection()
        {
            return new SQLiteConnection(connectionString);
        }

        // Fetch data from SQLite database and convert ResourceRarity to text using the enum
        public List<Resource> GetResources()
        {
            List<Resource> resources = new List<Resource>();

            // Fetch data from the database and convert ResourceRarity to text using the enum
            // Sort the data alphabetically
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    SELECT 
                        ResourceID,
                        ResourceType,
                        ResourceName,
                        ResourceShortName,
                        ResourceRarity
                    FROM
                        Resources
                    ORDER BY
                        ResourceRarity, ResourceName", conn))
                {
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        resources.Add(new Resource(
                            reader.GetInt32(0),
                            (ResourceType)int.Parse(reader["ResourceType"].ToString()),
                            reader["ResourceName"].ToString(),
                            (reader["ResourceShortName"] as string) ?? null,
                            (Rarity)int.Parse(reader["ResourceRarity"].ToString())
                        ));
                    }
                }
            }

            return resources;
        }

        public List<SolarSystem> GetSolarSystems()
        {
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();

                // get all resources and attach them to celestial bodies
                var resourceMap = new Dictionary<int, Resource>();
                var celestialBodyResourcesMap = new Dictionary<int, List<Resource>>();
                foreach (var resource in GetResources())
                {
                    resourceMap.Add(resource.ResourceID, resource);
                }

                var query = @"
                    SELECT
                        cb.BodyID,
                        br.ResourceID
                    FROM
                        CelestialBodies cb
                    LEFT JOIN
                        BodyResources br ON br.BodyID = cb.BodyID
                    LEFT JOIN
                        Resources r ON br.ResourceID = r.ResourceID
                    ORDER BY
                        r.ResourceRarity, r.ResourceName
                ";

                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int bodyID = reader.GetInt32(0);

                            if (reader.IsDBNull(1))
                            {
                                celestialBodyResourcesMap.Add(bodyID, null);
                                continue; // skip celestial bodies with no resources
                            }

                            int resourceID = reader.GetInt32(1);

                            if (!celestialBodyResourcesMap.ContainsKey(bodyID))
                            {
                                celestialBodyResourcesMap.Add(bodyID, new List<Resource>());
                            }

                            celestialBodyResourcesMap[bodyID].Add(resourceMap[resourceID]);
                        }
                    }
                }

                // get all fauna resources for attachment
                var faunaResourcesMap = new Dictionary<int, List<ResourceDrop>>();

                query = @"
                    SELECT
                        FaunaID,
                        ResourceID,
                        DropRate
                    FROM
                        FaunaResources
                    ORDER BY
                        DropRate ASC
                "; // TODO: instead of using ORDER BY, add precise logic if critters ever drop more than 1 primary resource

                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int faunaID = reader.GetInt32(0);
                            int resourceID = reader.GetInt32(1);
                            DropRate dropRate = (DropRate)int.Parse(reader["DropRate"].ToString());

                            if (!faunaResourcesMap.ContainsKey(faunaID))
                            {
                                faunaResourcesMap.Add(faunaID, new List<ResourceDrop>());
                            }

                            faunaResourcesMap[faunaID].Add(new ResourceDrop
                            {
                                Drop = resourceMap[resourceID],
                                DropRate = dropRate
                            });
                        }
                    }
                }

                // get all flora resources for attachment
                var floraResourcesMap = new Dictionary<int, List<ResourceDrop>>();

                query = @"
                    SELECT
                        FloraID,
                        ResourceID,
                        DropRate
                    FROM
                        FloraResources
                    ORDER BY
                        DropRate ASC
                "; // TODO: instead of using ORDER BY, add precise logic if critters ever drop more than 1 primary resource

                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int floraID = reader.GetInt32(0);
                            int resourceID = reader.GetInt32(1);
                            DropRate dropRate = (DropRate)int.Parse(reader["DropRate"].ToString());

                            if (!floraResourcesMap.ContainsKey(floraID))
                            {
                                floraResourcesMap.Add(floraID, new List<ResourceDrop>());
                            }

                            floraResourcesMap[floraID].Add(new ResourceDrop
                            {
                                Drop = resourceMap[resourceID],
                                DropRate = dropRate
                            });
                        }
                    }
                }

                // get all fauna data and attach them to celestial bodies
                var celestialBodyFaunasMap = new Dictionary<int, ObservableCollection<Fauna>>();

                query = @"
                    SELECT
                        FaunaID,
                        FaunaName,
                        ParentBodyID,
                        FaunaNotes
                    FROM
                        Faunas
                ";

                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int faunaID = reader.GetInt32(0);
                            string faunaName = reader.GetString(1);
                            int parentBodyID = reader.GetInt32(2);
                            string faunaNotes = reader.IsDBNull(3) ? null : reader.GetString(3);

                            if (!celestialBodyFaunasMap.ContainsKey(parentBodyID))
                            {
                                celestialBodyFaunasMap[parentBodyID] = new ObservableCollection<Fauna>();
                            }

                            var fauna = new Fauna
                            {
                                FaunaID = faunaID,
                                FaunaName = faunaName,
                                FaunaNotes = faunaNotes
                            };

                            if (faunaResourcesMap.ContainsKey(faunaID))
                            {
                                var faunaResources = faunaResourcesMap[faunaID];
                                foreach (var faunaResource in faunaResources)
                                {
                                    if (faunaResource.DropRate == DropRate.Primary)
                                    {
                                        fauna.AddPrimaryDrop(faunaResource.Drop);
                                    }
                                    else
                                    {
                                        fauna.AddSecondaryDrop(faunaResource.Drop);
                                    }
                                }
                            }

                            celestialBodyFaunasMap[parentBodyID].Add(fauna);
                        }
                    }
                }

                // get all flora data and attach them to celestial bodies
                var celestialBodyFlorasMap = new Dictionary<int, ObservableCollection<Flora>>();

                query = @"
                    SELECT
                        FloraID,
                        FloraName,
                        ParentBodyID,
                        FloraNotes
                    FROM
                        Floras
                ";

                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int floraID = reader.GetInt32(0);
                            string floraName = reader.GetString(1);
                            int parentBodyID = reader.GetInt32(2);
                            string floraNotes = reader.IsDBNull(3) ? null : reader.GetString(3);

                            if (!celestialBodyFlorasMap.ContainsKey(parentBodyID))
                            {
                                celestialBodyFlorasMap[parentBodyID] = new ObservableCollection<Flora>();
                            }

                            var flora = new Flora
                            {
                                FloraID = floraID,
                                FloraName = floraName,
                                FloraNotes = floraNotes
                            };

                            if (floraResourcesMap.ContainsKey(floraID))
                            {
                                var floraResources = floraResourcesMap[floraID];
                                foreach (var floraResource in floraResources)
                                {
                                    if (floraResource.DropRate == DropRate.Primary)
                                    {
                                        flora.AddPrimaryDrop(floraResource.Drop);
                                    }
                                    else
                                    {
                                        flora.AddSecondaryDrop(floraResource.Drop);
                                    }
                                }
                            }

                            celestialBodyFlorasMap[parentBodyID].Add(flora);
                        }
                    }
                }

                // query to get systems and their celestial bodies
                query = @"
                    SELECT
                        s.SystemID,
                        s.SystemName,
                        s.SystemLevel,
                        cb.BodyID,
                        cb.BodyName,
                        cb.IsMoon,
                        cb.BodyType,
                        cb.Gravity, 
                        cb.Temperature,
                        cb.Atmosphere,
                        cb.Magnetosphere,
                        cb.Water,
                        cb.TotalFauna,
                        cb.TotalFlora,
                        s.Discovered
                    FROM
                        Systems s
                    LEFT JOIN
                        SystemBodies sb ON s.SystemID = sb.SystemID
                    LEFT JOIN
                        CelestialBodies cb ON sb.BodyID = cb.BodyID
                    ORDER BY
                        s.SystemLevel, s.SystemName, cb.BodyID
                ";

                OrderedDictionary systemsWithBodiesMap = new OrderedDictionary();

                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int systemID = reader.GetInt32(0);
                            string systemName = reader.GetString(1);
                            int systemLevel = reader.GetInt32(2);

                            // check if the system already exists in the map
                            SolarSystem system;
                            if (systemsWithBodiesMap.Contains(systemID))
                            {
                                system = systemsWithBodiesMap[(object)systemID] as SolarSystem;
                            }
                            else
                            {
                                system = new SolarSystem
                                {
                                    SystemID = systemID,
                                    SystemName = systemName,
                                    SystemLevel = systemLevel,
                                    Discovered = reader.GetInt32(14) == 1,
                                    CelestialBodiesBuilder = new OrderedDictionary()
                                };

                                systemsWithBodiesMap.Add(systemID, system);
                            }

                            // read celestial body data
                            int bodyID = reader.GetInt32(3);
                            string bodyName = reader.GetString(4);
                            bool isMoon = reader.GetInt32(5) == 1;
                            string bodyType = reader.GetString(6);
                            double gravity = reader.GetDouble(7);
                            string temperature = reader.GetString(8);
                            string atmosphere = reader.GetString(9);
                            string magnetosphere = reader.GetString(10);
                            string water = reader.GetString(11);
                            int totalFauna = reader.GetInt32(12);
                            int totalFlora = reader.GetInt32(13);
                            var faunas = celestialBodyFaunasMap.ContainsKey(bodyID) ? celestialBodyFaunasMap[bodyID] : null;
                            var floras = celestialBodyFlorasMap.ContainsKey(bodyID) ? celestialBodyFlorasMap[bodyID] : null;

                            // Create and add CelestialBody object
                            CelestialBody celestialBody = new CelestialBody
                            {
                                BodyID = bodyID,
                                BodyName = bodyName,
                                SystemName = systemName,
                                IsMoon = isMoon,
                                BodyType = bodyType,
                                Gravity = gravity,
                                Temperature = temperature,
                                Atmosphere = atmosphere,
                                Magnetosphere = magnetosphere,
                                Water = water,
                                TotalFauna = totalFauna,
                                TotalFlora = totalFlora,
                                Resources = celestialBodyResourcesMap[bodyID],
                                Faunas = faunas,
                                Floras = floras
                            };

                            system.CelestialBodiesBuilder.Add(bodyID, celestialBody);
                        }
                    }
                }

                foreach (var system in systemsWithBodiesMap.Values.OfType<SolarSystem>())
                {
                    system.BuildList();
                }

                return systemsWithBodiesMap.Values.OfType<SolarSystem>().ToList();
            }
        }

        public void DiscoverSolarSystem(SolarSystem solarSystem)
        {
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    UPDATE
                        Systems
                    SET
                        Discovered = 1
                    WHERE
                        SystemID = @SystemID", conn))
                {
                    cmd.Parameters.AddWithValue("@SystemID", solarSystem.SystemID);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public Dictionary<LifeformType, HashSet<string>> GetLifeformNames()
        {
            var lifeformNames = new Dictionary<LifeformType, HashSet<string>>();
            lifeformNames.Add(LifeformType.Fauna, new HashSet<string>());
            lifeformNames.Add(LifeformType.Flora, new HashSet<string>());

            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    SELECT
                        LifeformName,
                        LifeformType
                    FROM
                        LifeformNames
                    ", conn))
                {
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string lifeformName = reader.GetString(0);
                            LifeformType lifeformType = (LifeformType)int.Parse(reader["LifeformType"].ToString());
                            lifeformNames[lifeformType].Add(lifeformName);
                        }

                        return lifeformNames;
                    }
                }
            }
        }

        public Fauna AddFauna(string faunaName, int parentBodyID)
        {
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    INSERT INTO
                        Faunas
                        (FaunaName, ParentBodyID)
                    VALUES
                        (@FaunaName, @ParentBodyID)", conn))
                {
                    cmd.Parameters.AddWithValue("@FaunaName", faunaName);
                    cmd.Parameters.AddWithValue("@ParentBodyID", parentBodyID);
                    var result = cmd.ExecuteScalar();

                    var insertedID = (int)conn.LastInsertRowId;
                    return new Fauna
                    {
                        FaunaID = insertedID,
                        FaunaName = faunaName
                    };
                }
            }
        }

        public Flora AddFlora(string floraName, int parentBodyID)
        {
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    INSERT INTO
                        Floras
                        (FloraName, ParentBodyID)
                    VALUES
                        (@FloraName, @ParentBodyID)", conn))
                {
                    cmd.Parameters.AddWithValue("@FloraName", floraName);
                    cmd.Parameters.AddWithValue("@ParentBodyID", parentBodyID);
                    var result = cmd.ExecuteScalar();

                    var insertedID = (int)conn.LastInsertRowId;
                    return new Flora
                    {
                        FloraID = insertedID,
                        FloraName = floraName
                    };
                }
            }
        }

        public void EditFauna(Fauna originalFauna, Fauna newFauna)
        {
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    UPDATE
                        Faunas
                    SET
                        FaunaName = @FaunaName,
                        FaunaNotes = @FaunaNotes
                    WHERE
                        FaunaID = @FaunaID", conn))
                {
                    cmd.Parameters.AddWithValue("@FaunaID", newFauna.FaunaID);
                    cmd.Parameters.AddWithValue("@FaunaName", newFauna.FaunaName);
                    cmd.Parameters.AddWithValue("@FaunaNotes", newFauna.FaunaNotes);

                    cmd.ExecuteNonQuery();
                }

                if (newFauna.IsSurveyed)
                {
                    var query = originalFauna.IsSurveyed ? @"
                        UPDATE
                            FaunaResources
                        SET
                            ResourceID = @ResourceID
                        WHERE
                            FaunaID = @FaunaID"
                    : @"
                        INSERT INTO
                            FaunaResources
                            (FaunaID, ResourceID)
                        VALUES
                            (@FaunaID, @ResourceID)";

                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@FaunaID", newFauna.FaunaID);
                        cmd.Parameters.AddWithValue("@ResourceID", newFauna.PrimaryDrops[0].ResourceID);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public void EditFlora(Flora originalFlora, Flora newFlora)
        {
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    UPDATE
                        Floras
                    SET
                        FloraName = @FloraName,
                        FloraNotes = @FloraNotes
                    WHERE
                        FloraID = @FloraID", conn))
                {
                    cmd.Parameters.AddWithValue("@FloraID", newFlora.FloraID);
                    cmd.Parameters.AddWithValue("@FloraName", newFlora.FloraName);
                    cmd.Parameters.AddWithValue("@FloraNotes", newFlora.FloraNotes);

                    cmd.ExecuteNonQuery();
                }

                if (newFlora.IsSurveyed)
                {
                    var query = originalFlora.IsSurveyed ? @"
                        UPDATE
                            FloraResources
                        SET
                            ResourceID = @ResourceID
                        WHERE
                            FloraID = @FloraID"
                    : @"
                        INSERT INTO
                            FloraResources
                            (FloraID, ResourceID)
                        VALUES
                            (@FloraID, @ResourceID)";

                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@FloraID", newFlora.FloraID);
                        cmd.Parameters.AddWithValue("@ResourceID", newFlora.PrimaryDrops[0].ResourceID);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
