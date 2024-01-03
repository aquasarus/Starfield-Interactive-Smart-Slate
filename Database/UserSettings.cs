﻿using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.AppCenter.Analytics;
using System.ComponentModel;

namespace Starfield_Interactive_Smart_Slate.Database
{
    public class UserSettings : ObservableObject
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
                SetProperty(ref enableSounds, value);
            }
        }

        public bool EnableAnalytics
        {
            get { return enableAnalytics; }
            set
            {
                DataRepository.SetUserSettingBool(EnableAnalyticsKey, value);
                Analytics.SetEnabledAsync(value);
                SetProperty(ref enableAnalytics, value);
            }
        }

        public bool EnableUpdateNotification
        {
            get { return enableUpdateNotification; }
            set
            {
                DataRepository.SetUserSettingBool(EnableUpdateNotificationKey, value);
                SetProperty(ref enableUpdateNotification, value);
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
                SetProperty(ref hasShownAnalyticsPopup, value);
            }
        }

        private bool enableSounds;
        private bool enableAnalytics;
        private bool enableUpdateNotification;
        private bool hasShownAnalyticsPopup;

        public void LoadSettings()
        {
            // avoid directly setting public properties here, since those also write to DB
            enableSounds = DataRepository.GetUserSettingBool(EnableSoundsKey);
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(EnableSounds)));

            enableAnalytics = DataRepository.GetUserSettingBool(EnableAnalyticsKey);
            Analytics.SetEnabledAsync(enableAnalytics);
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(EnableAnalytics)));

            enableUpdateNotification = DataRepository.GetUserSettingBool(EnableUpdateNotificationKey);
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(EnableUpdateNotification)));

            hasShownAnalyticsPopup = DataRepository.GetUserSettingBool(HasShownAnalyticsPopupKey);
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasShownAnalyticsPopup)));
        }
    }
}
