using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace Starfield_Interactive_Smart_Slate
{
    public partial class App : Application
    {
        private MediaPlayer scrollSoundPlayer = new MediaPlayer();
        private MediaPlayer clickSoundPlayer = new MediaPlayer();
        private MediaPlayer cancelSoundPlayer = new MediaPlayer();
        private static double SoundVolume = 0.8;

        public void PlayScrollSound()
        {
            if (scrollSoundPlayer.Volume == 0)
            {
                scrollSoundPlayer.Volume = SoundVolume;
            }
            scrollSoundPlayer.Stop();
            scrollSoundPlayer.Play();
        }

        public void PlayClickSound()
        {
            if (clickSoundPlayer.Volume == 0)
            {
                clickSoundPlayer.Volume = SoundVolume;
            }
            clickSoundPlayer.Stop();
            clickSoundPlayer.Play();
        }

        public void PlayCancelSound()
        {
            if (cancelSoundPlayer.Volume == 0)
            {
                cancelSoundPlayer.Volume = SoundVolume;
            }
            cancelSoundPlayer.Stop();
            cancelSoundPlayer.Play();
        }

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

                if (currentDatabaseVersion == 4)
                {
                    DatabaseInitializer.MigrateV4ToV5();
                    currentDatabaseVersion++;
                }
            }

            DatabaseInitializer.SetVersionToLatest();

            // preload sound files
            // set volume to 0 because for some reason it auto-plays the sound for just a little bit
            scrollSoundPlayer.Open(new Uri("Sounds/Scroll_Sound.mp3", UriKind.Relative));
            scrollSoundPlayer.Volume = 0;
            clickSoundPlayer.Open(new Uri("Sounds/Click_Sound.mp3", UriKind.Relative));
            clickSoundPlayer.Volume = 0;
            cancelSoundPlayer.Open(new Uri("Sounds/Cancel_Sound.mp3", UriKind.Relative));
            cancelSoundPlayer.Volume = 0;

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
