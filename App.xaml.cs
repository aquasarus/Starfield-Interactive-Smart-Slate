using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace Starfield_Interactive_Smart_Slate
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // add crash logging
            DispatcherUnhandledException += (sender, e) =>
            {
                HandleException(e.Exception);
            };

            // set up database and upgrade if needed
            DatabaseInitializer.InitializeDatabaseFile();
            var currentDatabaseVersion = DatabaseInitializer.CheckVersion();
            while (currentDatabaseVersion < DatabaseInitializer.TargetDatabaseVersion)
            {
                if (currentDatabaseVersion == 1)
                {
                    DatabaseInitializer.MigrateV1ToV2();
                    currentDatabaseVersion++;
                }

                if (currentDatabaseVersion == 2)
                {
                    DatabaseInitializer.MigrateV2ToV3();
                    currentDatabaseVersion++;
                }

                if (currentDatabaseVersion == 3)
                {
                    DatabaseInitializer.MigrateV3ToV4();
                    currentDatabaseVersion++;
                }
            }

            DatabaseInitializer.SetVersionToLatest();

            base.OnStartup(e);
        }
        private static void HandleException(Exception ex)
        {
            string logFileDirectory = DatabaseInitializer.UserDatabaseFolder();

            if (!Directory.Exists(logFileDirectory))
            {
                Directory.CreateDirectory(logFileDirectory);
            }

            Version version = Assembly.GetEntryAssembly().GetName().Version;
            string appVersionString = $"v{version.Major}.{version.Minor}.{version.Build}";

            string logFilePath = Path.Combine(logFileDirectory, $"error_{DateTime.Now:yyyy_MM_dd_HHmmss}.log");
            string errorMessage = $"[{DateTime.Now}] [{appVersionString}] Exception: {ex.Message}\nStack Trace: {ex.StackTrace}\n\n";
            File.AppendAllText(logFilePath, errorMessage);

            MessageBox.Show($"An unexpected error occured!\n\n" +
                $"The application will crash. Please find an error log in {logFileDirectory} and report it to my GitHub repo.",
                "ERROR");
        }
    }
}
