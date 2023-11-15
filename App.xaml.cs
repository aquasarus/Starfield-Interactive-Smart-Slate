using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Starfield_Interactive_Smart_Slate.Database;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace Starfield_Interactive_Smart_Slate
{
    public partial class App : Application
    {
        public UserSettings UserSettings;

        private MediaPlayer scrollSoundPlayer = new MediaPlayer();
        private MediaPlayer clickSoundPlayer = new MediaPlayer();
        private MediaPlayer cancelSoundPlayer = new MediaPlayer();
        private static double SoundVolume = 0.8;

        public void PlayScrollSound()
        {
            if (!UserSettings.EnableSounds) { return; }

            if (scrollSoundPlayer.Volume == 0)
            {
                scrollSoundPlayer.Volume = SoundVolume;
            }
            scrollSoundPlayer.Stop();
            scrollSoundPlayer.Play();
        }

        public void PlayClickSound()
        {
            if (!UserSettings.EnableSounds) { return; }

            if (clickSoundPlayer.Volume == 0)
            {
                clickSoundPlayer.Volume = SoundVolume;
            }
            clickSoundPlayer.Stop();
            clickSoundPlayer.Play();
        }

        public void PlayCancelSound()
        {
            if (!UserSettings.EnableSounds) { return; }

            if (cancelSoundPlayer.Volume == 0)
            {
                cancelSoundPlayer.Volume = SoundVolume;
            }
            cancelSoundPlayer.Stop();
            cancelSoundPlayer.Play();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // add local crash logging
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

                if (currentDatabaseVersion == 5)
                {
                    DatabaseInitializer.MigrateV5ToV6();
                    currentDatabaseVersion++;
                }

                if (currentDatabaseVersion == 6)
                {
                    DatabaseInitializer.MigrateV6ToV7();
                    currentDatabaseVersion++;
                }
            }

            DatabaseInitializer.SetVersionToLatest();

            // load in user settings from DB
            UserSettings = new UserSettings();
            UserSettings.LoadSettings();

            // preload sound files
            // set volume to 0 because for some reason it auto-plays the sound for just a little bit
            scrollSoundPlayer.Open(new Uri("Sounds/Scroll_Sound.mp3", UriKind.Relative));
            scrollSoundPlayer.Volume = 0;
            clickSoundPlayer.Open(new Uri("Sounds/Click_Sound.mp3", UriKind.Relative));
            clickSoundPlayer.Volume = 0;
            cancelSoundPlayer.Open(new Uri("Sounds/Cancel_Sound.mp3", UriKind.Relative));
            cancelSoundPlayer.Volume = 0;

            // preload User ID
            DataRepository.InitializeUserID();

            // initialize analytics
            AppCenter.SetUserId(DataRepository.UserID);
            AppCenter.LogLevel = LogLevel.Verbose;
            AppCenter.Start("", typeof(Analytics), typeof(Crashes));
            var appCenterConfigured = AppCenter.Configured; // TODO: add popup if this isn't configured

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
