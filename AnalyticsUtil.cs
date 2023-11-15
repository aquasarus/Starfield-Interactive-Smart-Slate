using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System;
using System.Collections.Generic;

namespace Starfield_Interactive_Smart_Slate
{
    public class AnalyticsUtil
    {
        public static void TrackEvent(string eventName)
        {
            Analytics.TrackEvent(eventName, new Dictionary<string, string>
            {
                { "UserID", DataRepository.UserID }
            });
        }

        public static void TrackError(Exception e)
        {
            Crashes.TrackError(e);
        }
    }
}
