using System.Windows;

namespace Starfield_Interactive_Smart_Slate
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static int TargetDatabaseVersion = 2;

        protected override void OnStartup(StartupEventArgs e)
        {
            DatabaseInitializer.InitializeDatabaseFile();
            var currentDatabaseVersion = DatabaseInitializer.CheckVersion();
            while (currentDatabaseVersion < TargetDatabaseVersion)
            {
                if (currentDatabaseVersion == 1)
                {
                    DatabaseInitializer.MigrateV1ToV2();
                    currentDatabaseVersion++;
                }
            }

            DatabaseInitializer.SetVersion(TargetDatabaseVersion);

            base.OnStartup(e);
        }
    }
}
