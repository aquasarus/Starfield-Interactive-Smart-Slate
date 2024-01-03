using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Starfield_Interactive_Smart_Slate.Models;
using System;
using System.Collections.Generic;

namespace Starfield_Interactive_Smart_Slate
{
    public class AnalyticsUtil
    {
        public static void TrackEvent(string eventName)
        {
            TrackEventWithProperties(eventName);
        }

        public static void TrackEventWithProperties(string eventName, Dictionary<string, string>? properties = null)
        {
            if (properties == null) properties = new Dictionary<string, string>();

            properties.TryAdd("UserID", DataRepository.UserID);
            Analytics.TrackEvent(eventName, properties);
        }

        public static void TrackResourceEvent(string eventName, Resource resource)
        {
            TrackEventWithProperties(eventName, new Dictionary<string, string>
            {
                { "ResourceName", resource.FullName }
            });
        }

        public static void TrackError(Exception e)
        {
            Crashes.TrackError(e);
        }
    }
}
