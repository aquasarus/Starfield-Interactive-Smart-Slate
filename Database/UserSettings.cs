using Microsoft.AppCenter.Analytics;
using System.ComponentModel;

namespace Starfield_Interactive_Smart_Slate.Database
{
    public class UserSettings : INotifyPropertyChanged
    {
        public static readonly string EnableSoundsKey = "EnableSounds";
        public static readonly string EnableAnalyticsKey = "EnableAnalytics";
        public static readonly string EnableUpdateNotificationKey = "EnableUpdateNotification";
        public static readonly string HasShownAnalyticsPopupKey = "HasShownAnalyticsPopup";

        public bool EnableSounds
        {
            get { return enableSounds; }
            set
            {
                DataRepository.SetUserSettingBool(EnableSoundsKey, value);
                enableSounds = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnableSounds)));
            }
        }

        public bool EnableAnalytics
        {
            get { return enableAnalytics; }
            set
            {
                DataRepository.SetUserSettingBool(EnableAnalyticsKey, value);
                Analytics.SetEnabledAsync(value);
                enableAnalytics = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnableAnalytics)));
            }
        }

        public bool EnableUpdateNotification
        {
            get { return enableUpdateNotification; }
            set
            {
                DataRepository.SetUserSettingBool(EnableUpdateNotificationKey, value);
                enableUpdateNotification = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnableUpdateNotification)));
            }
        }

        public bool HasShownAnalyticsPopup
        {
            get
            {
                return hasShownAnalyticsPopup;
            }
            set
            {
                DataRepository.SetUserSettingBool(HasShownAnalyticsPopupKey, value);
                hasShownAnalyticsPopup = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private bool enableSounds;
        private bool enableAnalytics;
        private bool enableUpdateNotification;
        private bool hasShownAnalyticsPopup;

        public void LoadSettings()
        {
            enableSounds = DataRepository.GetUserSettingBool(EnableSoundsKey);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnableSounds)));

            enableAnalytics = DataRepository.GetUserSettingBool(EnableAnalyticsKey);
            Analytics.SetEnabledAsync(enableAnalytics);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnableAnalytics)));

            enableUpdateNotification = DataRepository.GetUserSettingBool(EnableUpdateNotificationKey);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnableUpdateNotification)));

            hasShownAnalyticsPopup = DataRepository.GetUserSettingBool(HasShownAnalyticsPopupKey);
        }
    }
}
