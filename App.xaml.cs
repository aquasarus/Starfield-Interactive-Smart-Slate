using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Win32;
using Starfield_Interactive_Smart_Slate.Database;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace Starfield_Interactive_Smart_Slate
{
    public partial class App : Application
    {
        public UserSettings UserSettings;

        public new static App Current
        {
            get { return current; }
        }

        private static double SoundVolume = 0.8;
        private static App current;

        private MediaPlayer scrollSoundPlayer;
        private MediaPlayer clickSoundPlayer;
        private MediaPlayer cancelSoundPlayer;

        public App()
        {
            current = this;

            // TODO: maybe refactor all usages to directly use .Instance?
            UserSettings = UserSettings.Instance;
        }

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

            // abort if target version is outdated
            if (currentDatabaseVersion > DatabaseInitializer.TargetDatabaseVersion)
            {
                MessageBox.Show(
                    $"Your data version ({currentDatabaseVersion}) is higher than your app's target ({DatabaseInitializer.TargetDatabaseVersion}). " +
                    "You may be running an outdated version of the app. " +
                    "Please download and use the latest version!",
                    "Error"
                );
                Application.Current.Shutdown();
                return;
            }

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

                if (currentDatabaseVersion == 7)
                {
                    DatabaseInitializer.MigrateV7ToV8();
                    currentDatabaseVersion++;
                }

                if (currentDatabaseVersion == 8)
                {
                    DatabaseInitializer.MigrateV8toV9();
                    currentDatabaseVersion++;
                }

                if (currentDatabaseVersion == 9)
                {
                    DatabaseInitializer.MigrateV9toV10();
                    currentDatabaseVersion++;
                }
            }

            // if the current version is unexpectedly higher (e.g. user downgraded a build), don't overwrite version number
            if (currentDatabaseVersion == DatabaseInitializer.TargetDatabaseVersion)
            {
                DatabaseInitializer.SetVersionToLatest();
            }

            InitializeMediaPlayers();
            SystemEvents.PowerModeChanged += OnPowerModeChanged;

            // preload User ID
            DataRepository.InitializeUserID();

            // initialize analytics
            AppCenter.SetUserId(DataRepository.UserID);
            AppCenter.LogLevel = LogLevel.Verbose;
            AppCenter.SetCountryCode(RegionInfo.CurrentRegion.TwoLetterISORegionName);
            Analytics.EnableManualSessionTracker();
            AppCenter.Start("", typeof(Analytics), typeof(Crashes));
            Analytics.StartSession();
            if (!AppCenter.Configured)
            {
                MessageBox.Show("Analytics not configured!", "Warning");
            }

            // load in user settings from DB
            UserSettings.LoadSettings();

            base.OnStartup(e);
        }

        private void InitializeMediaPlayers()
        {
            scrollSoundPlayer = new MediaPlayer();
            clickSoundPlayer = new MediaPlayer();
            cancelSoundPlayer = new MediaPlayer();

            // preload sound files
            // set volume to 0 because for some reason it auto-plays the sound for just a little bit
            scrollSoundPlayer.Open(new Uri("Sounds/Scroll_Sound.mp3", UriKind.Relative));
            scrollSoundPlayer.Volume = 0;
            clickSoundPlayer.Open(new Uri("Sounds/Click_Sound.mp3", UriKind.Relative));
            clickSoundPlayer.Volume = 0;
            cancelSoundPlayer.Open(new Uri("Sounds/Cancel_Sound.mp3", UriKind.Relative));
            cancelSoundPlayer.Volume = 0;
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            // for some reason, an existing MediaPlayer stops working once the PC goes to sleep
            if (e.Mode == PowerModes.Resume)
            {
                InitializeMediaPlayers();
            }
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
