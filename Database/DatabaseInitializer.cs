using Starfield_Interactive_Smart_Slate.Database;
using System;
using System.Data.SQLite;
using System.IO;
using System.Reflection;

namespace Starfield_Interactive_Smart_Slate
{
    public static class DatabaseInitializer
    {
        public static readonly string DefaultDatabasePath = "Database/DataSlate.db";
        public static int TargetDatabaseVersion = 10;

        public static string UserDatabaseFolder()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appName = Assembly.GetEntryAssembly().GetName().Name.Replace(" ", "_");
            return Path.Combine(localAppData, appName);
        }

        public static string UserDatabasePath()
        {
            return Path.Combine(UserDatabaseFolder(), "DataSlate.db");
        }

        public static void InitializeDatabaseFile()
        {
            if (!Directory.Exists(UserDatabaseFolder()))
            {
                Directory.CreateDirectory(UserDatabaseFolder());
            }

            if (!File.Exists(UserDatabasePath()))
            {
                File.Copy(DefaultDatabasePath, UserDatabasePath());
            }
        }

        public static int CheckVersion()
        {
            using (SQLiteConnection conn = DataRepository.CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    SELECT
                        VersionNumber
                    FROM
                        VersionInfo
                    LIMIT 1", conn))
                {
                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        public static void SetVersionToLatest()
        {
            using (SQLiteConnection conn = DataRepository.CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    UPDATE
                        VersionInfo
                    SET
                        VersionNumber = @VersionNumber", conn))
                {
                    cmd.Parameters.AddWithValue("@VersionNumber", TargetDatabaseVersion);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void MigrateV1ToV2()
        {
            using (SQLiteConnection conn = DataRepository.CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    UPDATE CelestialBodies
                    SET TotalFauna = 11
                    WHERE BodyName = 'Bardeen III'
                ", conn))
                {
                    cmd.ExecuteNonQuery();
                }

                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    UPDATE CelestialBodies
                    SET
                        TotalFauna = 3,
                        TotalFlora = 3
                    WHERE BodyName = 'Newton III'
                ", conn))
                {
                    cmd.ExecuteNonQuery();
                }

                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    UPDATE CelestialBodies
                    SET
                        TotalFauna = 3,
                        TotalFlora = 3
                    WHERE BodyName = 'Newton III'
                ", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public static void MigrateV2ToV3()
        {
            using (SQLiteConnection conn = DataRepository.CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    UPDATE LifeformNames
                    SET LifeformName = 'Crimson Gibbet'
                    WHERE LifeformName = 'Chrimson Gibbet'
                ", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void MigrateV3ToV4()
        {
            using (SQLiteConnection conn = DataRepository.CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    UPDATE CelestialBodies
                    SET Atmosphere = 'High CO2'
                    WHERE BodyName = 'Al-Battani I'
                ", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public static void MigrateV4ToV5()
        {
            using (SQLiteConnection conn = DataRepository.CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    INSERT INTO
                        Resources
                        (ResourceName, ResourceType, ResourceRarity)
                    VALUES
                        ('None', 2, 0)
                ", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void MigrateV5ToV6()
        {
            using (SQLiteConnection conn = DataRepository.CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    CREATE TABLE UserInfo (
                        InfoKey TEXT PRIMARY KEY NOT NULL,
                        InfoValue TEXT NOT NULL
                    )", conn))
                {
                    cmd.ExecuteNonQuery();
                }

                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    INSERT INTO UserInfo
                        (InfoKey, InfoValue)
                    VALUES
                        (@InfoKey, @InfoValue)", conn))
                {
                    cmd.Parameters.AddWithValue("@InfoKey", DataRepository.UserIDKey);
                    cmd.Parameters.AddWithValue("@InfoValue", Guid.NewGuid().ToString());
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void MigrateV6ToV7()
        {
            using (SQLiteConnection conn = DataRepository.CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    INSERT INTO UserInfo
                        (InfoKey, InfoValue)
                    VALUES
                        (@EnableSoundsKey, 1),
                        (@EnableAnalyticsKey, 1),
                        (@EnableUpdateNotificationKey, 1),
                        (@HasShownAnalyticsPopupKey, 0)", conn))
                {
                    cmd.Parameters.AddWithValue("@EnableSoundsKey", UserSettings.EnableSoundsKey);
                    cmd.Parameters.AddWithValue("@EnableAnalyticsKey", UserSettings.EnableAnalyticsKey);
                    cmd.Parameters.AddWithValue("@EnableUpdateNotificationKey", UserSettings.EnableUpdateNotificationKey);
                    cmd.Parameters.AddWithValue("@HasShownAnalyticsPopupKey", UserSettings.HasShownAnalyticsPopupKey);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void MigrateV7ToV8()
        {
            using (SQLiteConnection conn = DataRepository.CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    CREATE TABLE IF NOT EXISTS Outposts (
                        OutpostID INTEGER PRIMARY KEY,
                        OutpostName TEXT NOT NULL,
                        ParentBodyID INTEGER NOT NULL,
                        OutpostNotes TEXT,
                        OutpostDeleted INTEGER NOT NULL DEFAULT 0,
                        FOREIGN KEY (ParentBodyID) REFERENCES CelestialBodies(BodyID));

                    CREATE TABLE IF NOT EXISTS OutpostPictures (
                        OutpostPictureID INTEGER PRIMARY KEY,
                        OutpostID INTEGER NOT NULL,
                        OutpostPicturePath TEXT NOT NULL,
                        FOREIGN KEY (OutpostID) REFERENCES Outposts(OutpostID))", conn))
                {
                    cmd.ExecuteNonQuery();
                }

                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    UPDATE CelestialBodies
                    SET TotalFlora = 8
                    WHERE BodyName = 'Guniibuu II'
                ", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void MigrateV8toV9()
        {
            using (SQLiteConnection conn = DataRepository.CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    UPDATE LifeformNames
                    SET LifeformName = 'Herding Rockhound Scavenger'
                    WHERE LifeformName = 'Herding Rockground Scavenger'
                ", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void MigrateV9toV10()
        {
            using (SQLiteConnection conn = DataRepository.CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    INSERT INTO UserInfo
                        (InfoKey, InfoValue)
                    VALUES
                        (@LanguageKey, 'English'),
                        (@UnlockLifeformCountsKey, 0)", conn))
                {
                    cmd.Parameters.AddWithValue("@LanguageKey", UserSettings.LanguageKey);
                    cmd.Parameters.AddWithValue("@UnlockLifeformCountsKey", UserSettings.UnlockLifeformCountsKey);
                    cmd.ExecuteNonQuery();
                }

                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    DELETE FROM LifeformNames
                    WHERE LifeformName IN (
                        'Hunting Nail Tail', 
                        'Trilibite Filterer',
                        'Flocking Scepterer Filterer',
                        'Cassoway Grazer'
                    )", conn))
                {
                    cmd.ExecuteNonQuery();
                }

                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    UPDATE LifeformNames
                    SET LifeformName = 'Swarming Stingback'
                    WHERE LifeformName = 'Swarmking Stingback'
                ", conn))
                {
                    cmd.ExecuteNonQuery();
                }

                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    UPDATE LifeformNames
                    SET LifeformName = 'Hunting Clickbeetle Scavenger'
                    WHERE LifeformName = 'Hunting Clickbeetle Scanvenger'
                ", conn))
                {
                    cmd.ExecuteNonQuery();
                }

                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    UPDATE LifeformNames
                    SET LifeformName = 'Chasmbass'
                    WHERE LifeformName = 'Chambass'
                ", conn))
                {
                    cmd.ExecuteNonQuery();
                }

                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    UPDATE LifeformNames
                    SET LifeformName = 'Wanderer''s Husk'
                    WHERE LifeformName = 'Wanderer''S Husk'
                ", conn))
                {
                    cmd.ExecuteNonQuery();
                }

                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    UPDATE LifeformNames
                    SET LifeformName = 'Wander''s Husk'
                    WHERE LifeformName = 'Wander''S Husk'
                ", conn))
                {
                    cmd.ExecuteNonQuery();
                }

                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    UPDATE LifeformNames
                    SET LifeformName = 'Hunter''s Stalk'
                    WHERE LifeformName = 'Hunter''S Stalk'
                ", conn))
                {
                    cmd.ExecuteNonQuery();
                }

                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    UPDATE LifeformNames
                    SET LifeformName = 'Hecate''s Fireleaf'
                    WHERE LifeformName = 'Hecate''S Fireleaf'
                ", conn))
                {
                    cmd.ExecuteNonQuery();
                }

                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    UPDATE LifeformNames
                    SET LifeformName = 'Explorer''s Coleus'
                    WHERE LifeformName = 'Explorer''S Coleus'
                ", conn))
                {
                    cmd.ExecuteNonQuery();
                }

                using (SQLiteCommand cmd = new SQLiteCommand(@"
                    UPDATE CelestialBodies
                    SET
                        TotalFlora = 3
                    WHERE BodyName = 'Syrma IV'
                ", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
