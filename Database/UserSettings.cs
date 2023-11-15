using System.ComponentModel;

namespace Starfield_Interactive_Smart_Slate.Database
{
    public class UserSettings : INotifyPropertyChanged
    {
        public static readonly string EnableSoundsKey = "EnableSounds";
        public static readonly string EnableAnalyticsKey = "EnableAnalytics";
        public static readonly string EnableUpdateNotificationKey = "EnableUpdateNotification";

        public bool EnableSounds
        {
            get { return enableSounds; }
            set
            {
                DataRepository.SetUserSettingBool(EnableSoundsKey, value);
                enableSounds = value;
            }
        }
        public bool EnableAnalytics
        {
            get { return enableAnalytics; }
            set
            {
                DataRepository.SetUserSettingBool(EnableAnalyticsKey, value);
                enableAnalytics = value;
            }
        }
        public bool EnableUpdateNotification
        {
            get { return enableUpdateNotification; }
            set
            {
                DataRepository.SetUserSettingBool(EnableUpdateNotificationKey, value);
                enableUpdateNotification = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private bool enableSounds;
        private bool enableAnalytics;
        private bool enableUpdateNotification;


        public void LoadSettings()
        {
            enableSounds = DataRepository.GetUserSettingBool(EnableSoundsKey);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnableSounds)));

            enableAnalytics = DataRepository.GetUserSettingBool(EnableAnalyticsKey);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnableAnalytics)));

            enableUpdateNotification = DataRepository.GetUserSettingBool(EnableUpdateNotificationKey);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnableUpdateNotification)));
        }
    }
}
