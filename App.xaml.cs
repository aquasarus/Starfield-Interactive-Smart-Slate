using System.Windows;

namespace Starfield_Interactive_Smart_Slate
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
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
            }

            DatabaseInitializer.SetVersionToLatest();

            base.OnStartup(e);
        }
    }
}
