using Starfield_Interactive_Smart_Slate.Dialogs;
using Starfield_Interactive_Smart_Slate.Screens.PlanetaryData;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace Starfield_Interactive_Smart_Slate
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer updateTimer;

        public MainWindow()
        {
            InitializeComponent();

            MainViewModel.Instance.ReloadAllData();

            EnableSoundsCheckBox.DataContext = App.Current.UserSettings;
            EnableAnalyticsCheckBox.DataContext = App.Current.UserSettings;
            EnableUpdateNotificationCheckBox.DataContext = App.Current.UserSettings;
            UnlockLifeformCountsCheckBox.DataContext = App.Current.UserSettings;

            // show version number
            Version version = Assembly.GetEntryAssembly().GetName().Version;
            VersionNumberLabel.Content = $"Version {version.Major}.{version.Minor}.{version.Build}";

            CheckForUpdate();

            // check for updates once a day in case the user never closes the app
            updateTimer = new DispatcherTimer();
            updateTimer.Interval = TimeSpan.FromDays(1);
            updateTimer.Tick += Timer_Tick;
            updateTimer.Start();

            // initialize user ID label
            UserIDLabel.Content = $"User ID: {DataRepository.UserID}";
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            CheckForUpdate();
        }

        private void TabClicked(object sender, MouseButtonEventArgs e)
        {
            if (!((TabItem)sender).IsSelected)
            {
                App.Current.PlayClickSound();
            }
        }

        private void TabItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!((TabItem)sender).IsSelected)
            {
                App.Current.PlayScrollSound();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!App.Current.UserSettings.HasShownAnalyticsPopup)
            {
                var analyticsPermissionPopup = new BasicYesNoDialog("Analytics",
                    "To continue improving this app, I'd love to know how many people are actually using it. " +
                        "Will you allow me to collect some anonymous analytics data from this app?\n\n" +
                        $"You will be identified as:\n{DataRepository.UserID}",
                    "Okay",
                    "Opt Out");
                analyticsPermissionPopup.Owner = this;
                analyticsPermissionPopup.ShowDialog();
                if (analyticsPermissionPopup.ExplicitNo)
                {
                    App.Current.UserSettings.EnableAnalytics = false;
                }

                // also show first launch tutorial
                var welcomeDialog = new BasicYesNoDialog("Quick Start",
                    "Welcome to Starfield ISS!\n\n" +
                    "This is an exploration compendium for you to catalog your own survey data. " +
                    "To get started, click Discover New System to reveal your first solar system.",
                    "Got it");
                welcomeDialog.Owner = this;
                welcomeDialog.ShowDialog();

                App.Current.UserSettings.HasShownAnalyticsPopup = true;
            }

            if (ActualHeight >= SystemParameters.PrimaryScreenHeight ||
                ActualWidth >= SystemParameters.PrimaryScreenWidth)
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        // -----------------------------------------------------------------------------------------------
        // SETTINGS PAGE
        // -----------------------------------------------------------------------------------------------
        #region

        private void EnableSoundsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (App.Current.UserSettings.EnableSounds)
            {
                App.Current.PlayClickSound();
            }
            else
            {
                App.Current.PlayCancelSound();
            }
        }

        private void EnableAnalyticsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (App.Current.UserSettings.EnableAnalytics)
            {
                App.Current.PlayClickSound();
            }
            else
            {
                App.Current.PlayCancelSound();
            }
        }

        private void EnableUpdateNotificationCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (App.Current.UserSettings.EnableUpdateNotification)
            {
                App.Current.PlayClickSound();
            }
            else
            {
                App.Current.PlayCancelSound();
            }
        }

        private void UnlockLifeformCountsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (App.Current.UserSettings.UnlockLifeformCounts)
            {
                App.Current.PlayClickSound();
            }
            else
            {
                App.Current.PlayCancelSound();
            }

            PlanetaryDataViewModel.Instance.DisplayedCelestialBody?.NotifyLifeformUnlockChanged();
        }

        #endregion

        // -----------------------------------------------------------------------------------------------
        // ABOUT PAGE
        // -----------------------------------------------------------------------------------------------
        #region

        private void NavigateToHyperlink(object sender, RequestNavigateEventArgs e)
        {
            App.Current.PlayClickSound();
            LaunchHyperlink(e.Uri.ToString());
            e.Handled = true;
        }

        private void DataFolderLinkClick(object sender, RoutedEventArgs e)
        {
            App.Current.PlayClickSound();
            Process.Start("explorer.exe", DatabaseInitializer.UserDatabaseFolder());
        }

        private void CopyUserID(object sender, RoutedEventArgs e)
        {
            App.Current.PlayClickSound();
            Clipboard.SetText(DataRepository.UserID);
            MessageBox.Show($"User ID {DataRepository.UserID} has been copied to clipboard!", "Copied");
        }

        private void LaunchHyperlink(string hyperlink)
        {
            Process.Start(new ProcessStartInfo(hyperlink)
            {
                UseShellExecute = true // need to set this to get web links to work here
            });
        }

        private async void CheckForUpdate()
        {
            try
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage response =
                    await client.GetAsync("https://github.com/aquasarus/Starfield-Interactive-Smart-Slate");
                response.EnsureSuccessStatusCode();
                string htmlContent = await response.Content.ReadAsStringAsync();

                var pattern = "(Releases).*?tag\\/v(\\d+\\.\\d+\\.\\d+).*?(Label: Latest)";
                Match match = Regex.Match(htmlContent, pattern, RegexOptions.Singleline);
                if (match.Success)
                {
                    Version latestVersion = Version.Parse(match.Groups[2].Value);
                    Version currentVersion = Assembly.GetEntryAssembly().GetName().Version;

                    if (latestVersion.CompareTo(currentVersion) > 0)
                    {
                        AnalyticsUtil.TrackEvent("Found app update");
                        NewVersionAvailableHyperlink.Inlines.Clear();
                        NewVersionAvailableHyperlink.Inlines.Add($"(New Version v{latestVersion} Available)");
                        NewVersionAvailableLabel.Visibility = Visibility.Visible;

                        NewVersionAvailableSettingsHyperlink.Inlines.Clear();
                        NewVersionAvailableSettingsHyperlink.Inlines.Add($"> New Version v{latestVersion} Available");
                        NewVersionAvailableSettingsLabel.Visibility = Visibility.Visible;

                        // don't clash with the analytics popup
                        if (App.Current.UserSettings.EnableUpdateNotification
                            && App.Current.UserSettings.HasShownAnalyticsPopup)
                        {
                            var latestVersionString = $"v{latestVersion.Major}.{latestVersion.Minor}.{latestVersion.Build}";
                            var currentVersionString = $"v{currentVersion.Major}.{currentVersion.Minor}.{currentVersion.Build}";
                            var newVersionNotification = new BasicYesNoDialog("New Version Available",
                                $"There is a newer version available on GitHub!\n" +
                                $"Wanna check it out?\n\n" +
                                $"Your current version is: {currentVersionString}\n" +
                                $"The latest version is: {latestVersionString}\n\n" +
                                $"This dialog can be disabled in the Settings tab.",
                                "Yes",
                                "No");
                            newVersionNotification.Owner = this;
                            if (newVersionNotification.ShowDialog() == true)
                            {
                                LaunchHyperlink("https://github.com/aquasarus/Starfield-Interactive-Smart-Slate/releases");
                            }
                        }
                    }
                }
                else
                {
                    AnalyticsUtil.TrackEvent("Did not find latest release on GitHub");
                }
            }
            catch (Exception ex)
            {
                AnalyticsUtil.TrackError(ex);
                if (Debugger.IsAttached) { throw; }
            }
        }

        #endregion
    }
}