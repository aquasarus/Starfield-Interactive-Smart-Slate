using Starfield_Interactive_Smart_Slate.Models;
using Starfield_Interactive_Smart_Slate.Models.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data.SQLite;
using System.Linq;

namespace Starfield_Interactive_Smart_Slate
{
    public static class DataRepository
    {
        public static readonly string CnnectionString = $"Data Source={DatabaseInitializer.UserDatabasePath()};Version=3;";
        public static readonly string UserIDKey = "UserID";

        public static string UserID { get; set; }

        public static SQLiteConnection CreateConnection()
        {
            return new SQLiteConnection(CnnectionString);
        }

        public static void InitializeUserID()
        {
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    SELECT
                        InfoValue
                    FROM
                        UserInfo
                    WHERE
                        InfoKey = @InfoKey
                ", conn))
                {
                    cmd.Parameters.AddWithValue("@InfoKey", UserIDKey);
                    var reader = cmd.ExecuteReader();
                    reader.Read();

                    UserID = reader.GetString(0);
                }
            }
        }

        public static bool GetUserSettingBool(string infoKey)
        {
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    SELECT
                        InfoValue
                    FROM
                        UserInfo
                    WHERE
                        InfoKey = @InfoKey
                ", conn))
                {
                    cmd.Parameters.AddWithValue("@InfoKey", infoKey);
                    var reader = cmd.ExecuteReader();
                    reader.Read();

                    return reader.GetString(0) == "1";
                }
            }
        }

        public static string GetUserSettingString(string infoKey)
        {
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    SELECT
                        InfoValue
                    FROM
                        UserInfo
                    WHERE
                        InfoKey = @InfoKey
                ", conn))
                {
                    cmd.Parameters.AddWithValue("@InfoKey", infoKey);
                    var reader = cmd.ExecuteReader();
                    reader.Read();

                    return reader.GetString(0);
                }
            }
        }

        public static void SetUserSettingBool(string infoKey, bool infoValue)
        {
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    UPDATE
                        UserInfo
                    SET
                        InfoValue = @InfoValue
                    WHERE
                        InfoKey = @InfoKey", conn))
                {
                    cmd.Parameters.AddWithValue("@InfoKey", infoKey);
                    cmd.Parameters.AddWithValue("@InfoValue", infoValue);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void SetUserSettingString(string infoKey, string infoValue)
        {
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    UPDATE
                        UserInfo
                    SET
                        InfoValue = @InfoValue
                    WHERE
                        InfoKey = @InfoKey", conn))
                {
                    cmd.Parameters.AddWithValue("@InfoKey", infoKey);
                    cmd.Parameters.AddWithValue("@InfoValue", infoValue);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Fetch data from SQLite database and convert ResourceRarity to text using the enum
        public static List<Resource> GetResources()
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

        public static List<SolarSystem> GetSolarSystems()
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
                        f.FaunaID,
                        f.FaunaName,
                        f.ParentBodyID,
                        f.FaunaNotes,
                        fp.FaunaPicturePath,
                        fp.FaunaPictureID
                    FROM
                        Faunas f
                    LEFT JOIN
                        FaunaPictures fp ON fp.FaunaID = f.FaunaID
                    WHERE
                        f.FaunaDeleted = 0
                    ORDER BY
                        f.FaunaID, fp.FaunaPictureID
                ";

                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        Fauna currentFauna = null;
                        while (reader.Read())
                        {
                            int faunaID = reader.GetInt32(0);
                            string faunaName = reader.GetString(1);
                            int parentBodyID = reader.GetInt32(2);
                            string faunaNotes = reader.IsDBNull(3) ? null : reader.GetString(3);

                            string faunaPicturePath = reader.IsDBNull(4) ? null : reader.GetString(4);
                            int faunaPictureID = reader.IsDBNull(5) ? -1 : reader.GetInt32(5);

                            if (!celestialBodyFaunasMap.ContainsKey(parentBodyID))
                            {
                                celestialBodyFaunasMap[parentBodyID] = new ObservableCollection<Fauna>();
                            }

                            if (currentFauna == null || currentFauna.ID != faunaID)
                            {
                                var fauna = new Fauna
                                {
                                    ID = faunaID,
                                    Name = faunaName,
                                    Notes = faunaNotes
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
                                currentFauna = fauna;
                            }

                            if (faunaPicturePath != null)
                            {
                                currentFauna.AddPicture(new Picture(faunaPictureID, new Uri(faunaPicturePath)));
                            }
                        }
                    }
                }

                // get all flora data and attach them to celestial bodies
                var celestialBodyFlorasMap = new Dictionary<int, ObservableCollection<Flora>>();

                query = @"
                    SELECT
                        f.FloraID,
                        f.FloraName,
                        f.ParentBodyID,
                        f.FloraNotes,
                        fp.FloraPicturePath,
                        fp.FloraPictureID
                    FROM
                        Floras f
                    LEFT JOIN
                        FloraPictures fp ON fp.FloraID = f.FloraID
                    WHERE
                        f.FloraDeleted = 0
                    ORDER BY
                        f.FloraID, fp.FloraPictureID
                ";

                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        Flora currentFlora = null;
                        while (reader.Read())
                        {
                            int floraID = reader.GetInt32(0);
                            string floraName = reader.GetString(1);
                            int parentBodyID = reader.GetInt32(2);
                            string floraNotes = reader.IsDBNull(3) ? null : reader.GetString(3);
                            string floraPicturePath = reader.IsDBNull(4) ? null : reader.GetString(4);
                            int floraPictureID = reader.IsDBNull(5) ? -1 : reader.GetInt32(5);

                            if (!celestialBodyFlorasMap.ContainsKey(parentBodyID))
                            {
                                celestialBodyFlorasMap[parentBodyID] = new ObservableCollection<Flora>();
                            }

                            if (currentFlora == null || currentFlora.ID != floraID)
                            {
                                var flora = new Flora
                                {
                                    ID = floraID,
                                    Name = floraName,
                                    Notes = floraNotes
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
                                currentFlora = flora;
                            }

                            if (floraPicturePath != null)
                            {
                                currentFlora.AddPicture(new Picture(floraPictureID, new Uri(floraPicturePath)));
                            }
                        }
                    }
                }

                // get all outpost data and attach them to celestial bodies
                var celestialBodyOutpostsMap = new Dictionary<int, ObservableCollection<Outpost>>();

                query = @"
                    SELECT
                        f.OutpostID,
                        f.OutpostName,
                        f.ParentBodyID,
                        f.OutpostNotes,
                        fp.OutpostPicturePath,
                        fp.OutpostPictureID
                    FROM
                        Outposts f
                    LEFT JOIN
                        OutpostPictures fp ON fp.OutpostID = f.OutpostID
                    WHERE
                        f.OutpostDeleted = 0
                    ORDER BY
                        f.OutpostID, fp.OutpostPictureID
                ";

                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        Outpost currentOutpost = null;
                        while (reader.Read())
                        {
                            int outpostID = reader.GetInt32(0);
                            string outpostName = reader.GetString(1);
                            int parentBodyID = reader.GetInt32(2);
                            string outpostNotes = reader.IsDBNull(3) ? null : reader.GetString(3);
                            string outpostPicturePath = reader.IsDBNull(4) ? null : reader.GetString(4);
                            int outpostPictureID = reader.IsDBNull(5) ? -1 : reader.GetInt32(5);

                            if (!celestialBodyOutpostsMap.ContainsKey(parentBodyID))
                            {
                                celestialBodyOutpostsMap[parentBodyID] = new ObservableCollection<Outpost>();
                            }

                            if (currentOutpost == null || currentOutpost.ID != outpostID)
                            {
                                var outpost = new Outpost
                                {
                                    ID = outpostID,
                                    Name = outpostName,
                                    Notes = outpostNotes
                                };

                                celestialBodyOutpostsMap[parentBodyID].Add(outpost);
                                currentOutpost = outpost;
                            }

                            if (outpostPicturePath != null)
                            {
                                currentOutpost.AddPicture(new Picture(outpostPictureID, new Uri(outpostPicturePath)));
                            }
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
                        CelestialBody currentPlanet = null;
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
                            var outposts = celestialBodyOutpostsMap.ContainsKey(bodyID) ? celestialBodyOutpostsMap[bodyID] : null;

                            // Create and add CelestialBody object
                            CelestialBody celestialBody = new CelestialBody
                            {
                                BodyID = bodyID,
                                BodyName = bodyName,
                                ParentSystem = system,
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
                                Floras = floras,
                                Outposts = outposts
                            };

                            if (!celestialBody.IsMoon)
                            {
                                currentPlanet = celestialBody;
                            }
                            else
                            {
                                currentPlanet.AddMoon(celestialBody);
                            }

                            system.CelestialBodiesBuilder.Add(bodyID, celestialBody);
                        }
                    }
                }

                foreach (var system in systemsWithBodiesMap.Values.OfType<SolarSystem>())
                {
                    system.BuildList();
                    system.ShowAllCelestialBodies();
                }

                return systemsWithBodiesMap.Values.OfType<SolarSystem>().ToList();
            }
        }

        public static void DiscoverSolarSystem(SolarSystem solarSystem)
        {
            AnalyticsUtil.TrackEvent("Discover solar system");
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

        // gets the full list of lifeform names, mapping a lowercase version to the capitalized version
        // e.g. Dictionary { "pack octomaggot":  "Pack Octomaggot" }
        public static Dictionary<LifeformType, Dictionary<string, string>> GetLifeformNames()
        {
            var lifeformNames = new Dictionary<LifeformType, Dictionary<string, string>>
            {
                { LifeformType.Fauna, new Dictionary<string, string>() },
                { LifeformType.Flora, new Dictionary<string, string>() }
            };

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
                            lifeformNames[lifeformType].Add(lifeformName.ToLower(), lifeformName);
                        }

                        return lifeformNames;
                    }
                }
            }
        }

        public static Fauna AddFauna(string faunaName, int parentBodyID)
        {
            AnalyticsUtil.TrackEvent("Add fauna");
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
                        ID = insertedID,
                        Name = faunaName
                    };
                }
            }
        }

        public static Flora AddFlora(string floraName, int parentBodyID)
        {
            AnalyticsUtil.TrackEvent("Add flora");
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
                        ID = insertedID,
                        Name = floraName
                    };
                }
            }
        }

        public static Outpost AddOutpost(string outpostName, int parentBodyID)
        {
            AnalyticsUtil.TrackEvent("Add outpost");
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    INSERT INTO
                        Outposts
                        (OutpostName, ParentBodyID)
                    VALUES
                        (@OutpostName, @ParentBodyID)", conn))
                {
                    cmd.Parameters.AddWithValue("@OutpostName", outpostName);
                    cmd.Parameters.AddWithValue("@ParentBodyID", parentBodyID);
                    var result = cmd.ExecuteScalar();

                    var insertedID = (int)conn.LastInsertRowId;
                    return new Outpost
                    {
                        ID = insertedID,
                        Name = outpostName
                    };
                }
            }
        }

        public static void DeleteFauna(int faunaID)
        {
            AnalyticsUtil.TrackEvent("Delete fauna");
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    UPDATE
                        Faunas
                    SET
                        FaunaDeleted = 1
                    WHERE
                        FaunaID = @FaunaID", conn))
                {
                    cmd.Parameters.AddWithValue("@FaunaID", faunaID);
                    var result = cmd.ExecuteNonQuery();
                }
            }
        }

        public static void DeleteFlora(int floraID)
        {
            AnalyticsUtil.TrackEvent("Delete flora");
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    UPDATE
                        Floras
                    SET
                        FloraDeleted = 1
                    WHERE
                        FloraID = @FloraID", conn))
                {
                    cmd.Parameters.AddWithValue("@FloraID", floraID);
                    var result = cmd.ExecuteNonQuery();
                }
            }
        }

        public static void DeleteOutpost(int outpostID)
        {
            AnalyticsUtil.TrackEvent("Delete outpost");
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    UPDATE
                        Outposts
                    SET
                        OutpostDeleted = 1
                    WHERE
                        OutpostID = @OutpostID", conn))
                {
                    cmd.Parameters.AddWithValue("@OutpostID", outpostID);
                    var result = cmd.ExecuteNonQuery();
                }
            }
        }

        public static void EditFauna(Fauna originalFauna, Fauna newFauna)
        {
            AnalyticsUtil.TrackEvent("Edit fauna");
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
                    cmd.Parameters.AddWithValue("@FaunaID", newFauna.ID);
                    cmd.Parameters.AddWithValue("@FaunaName", newFauna.Name);
                    cmd.Parameters.AddWithValue("@FaunaNotes", newFauna.Notes);

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
                        cmd.Parameters.AddWithValue("@FaunaID", newFauna.ID);
                        cmd.Parameters.AddWithValue("@ResourceID", newFauna.PrimaryDrops[0].ResourceID);

                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    if (originalFauna.IsSurveyed)
                    {
                        using (SQLiteCommand cmd = new SQLiteCommand(@"
                            DELETE FROM
                                FaunaResources
                            WHERE FaunaID = @FaunaID", conn))
                        {
                            cmd.Parameters.AddWithValue("@FaunaID", newFauna.ID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public static void EditFlora(Flora originalFlora, Flora newFlora)
        {
            AnalyticsUtil.TrackEvent("Edit flora");
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
                    cmd.Parameters.AddWithValue("@FloraID", newFlora.ID);
                    cmd.Parameters.AddWithValue("@FloraName", newFlora.Name);
                    cmd.Parameters.AddWithValue("@FloraNotes", newFlora.Notes);

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
                        cmd.Parameters.AddWithValue("@FloraID", newFlora.ID);
                        cmd.Parameters.AddWithValue("@ResourceID", newFlora.PrimaryDrops[0].ResourceID);

                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    if (originalFlora.IsSurveyed)
                    {
                        using (SQLiteCommand cmd = new SQLiteCommand(@"
                            DELETE FROM
                                FloraResources
                            WHERE FloraID = @FloraID", conn))
                        {
                            cmd.Parameters.AddWithValue("@FloraID", newFlora.ID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public static void EditOutpost(Outpost newOutpost)
        {
            AnalyticsUtil.TrackEvent("Edit outpost");
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    UPDATE
                        Outposts
                    SET
                        OutpostName = @OutpostName,
                        OutpostNotes = @OutpostNotes
                    WHERE
                        OutpostID = @OutpostID", conn))
                {
                    cmd.Parameters.AddWithValue("@OutpostID", newOutpost.ID);
                    cmd.Parameters.AddWithValue("@OutpostName", newOutpost.Name);
                    cmd.Parameters.AddWithValue("@OutpostNotes", newOutpost.Notes);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static int AddFaunaPicture(Fauna fauna, string pictureUri)
        {
            AnalyticsUtil.TrackEvent("Add fauna picture");
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    INSERT INTO
                        FaunaPictures
                        (FaunaID, FaunaPicturePath)
                    VALUES
                        (@FaunaID, @FaunaPicturePath)", conn))
                {
                    cmd.Parameters.AddWithValue("@FaunaID", fauna.ID);
                    cmd.Parameters.AddWithValue("@FaunaPicturePath", pictureUri);

                    cmd.ExecuteNonQuery();
                    return (int)conn.LastInsertRowId;
                }
            }
        }

        public static int AddFloraPicture(Flora flora, string pictureUri)
        {
            AnalyticsUtil.TrackEvent("Add flora picture");
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    INSERT INTO
                        FloraPictures
                        (FloraID, FloraPicturePath)
                    VALUES
                        (@FloraID, @FloraPicturePath)", conn))
                {
                    cmd.Parameters.AddWithValue("@FloraID", flora.ID);
                    cmd.Parameters.AddWithValue("@FloraPicturePath", pictureUri);

                    cmd.ExecuteNonQuery();
                    return (int)conn.LastInsertRowId;
                }
            }
        }

        public static int AddOutpostPicture(Outpost outpost, string pictureUri)
        {
            AnalyticsUtil.TrackEvent("Add outpost picture");
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    INSERT INTO
                        OutpostPictures
                        (OutpostID, OutpostPicturePath)
                    VALUES
                        (@OutpostID, @OutpostPicturePath)", conn))
                {
                    cmd.Parameters.AddWithValue("@OutpostID", outpost.ID);
                    cmd.Parameters.AddWithValue("@OutpostPicturePath", pictureUri);

                    cmd.ExecuteNonQuery();
                    return (int)conn.LastInsertRowId;
                }
            }
        }

        public static void DeleteFaunaPicture(Picture picture)
        {
            AnalyticsUtil.TrackEvent("Delete fauna picture");
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    DELETE FROM
                        FaunaPictures
                    WHERE
                        FaunaPictureID = @FaunaPictureID", conn))
                {
                    cmd.Parameters.AddWithValue("@FaunaPictureID", picture.PictureID);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void DeleteFloraPicture(Picture picture)
        {
            AnalyticsUtil.TrackEvent("Delete flora picture");
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    DELETE FROM
                        FloraPictures
                    WHERE
                        FloraPictureID = @FloraPictureID", conn))
                {
                    cmd.Parameters.AddWithValue("@FloraPictureID", picture.PictureID);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void DeleteOutpostPicture(Picture picture)
        {
            AnalyticsUtil.TrackEvent("Delete outpost picture");
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    DELETE FROM
                        OutpostPictures
                    WHERE
                        OutpostPictureID = @OutpostPictureID", conn))
                {
                    cmd.Parameters.AddWithValue("@OutpostPictureID", picture.PictureID);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
