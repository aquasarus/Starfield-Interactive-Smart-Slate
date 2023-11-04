using System;
using System.Data.SQLite;
using System.IO;
using System.Reflection;

namespace Starfield_Interactive_Smart_Slate
{
    public static class DatabaseInitializer
    {
        public static readonly string DefaultDatabasePath = "Database/DataSlate.db";
        public static int TargetDatabaseVersion = 4;

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
    }
}
