using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Starfield_Interactive_Smart_Slate.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public static void TrackMultipleResourcesEvent(string eventName, IEnumerable<Resource> resources)
        {
            var resourcesStringBuilder = new StringBuilder();
            foreach (var resource in resources.OrderBy(r => r.FullName))
            {
                if (resourcesStringBuilder.Length == 0)
                {
                    resourcesStringBuilder.Append(resource.FullName);

                }
                else
                {
                    resourcesStringBuilder.Append($" + {resource.FullName}");
                }
            }

            TrackEventWithProperties(eventName, new Dictionary<string, string>
            {
                { "ResourceNames", resourcesStringBuilder.ToString() }
            });
        }

        public static void TrackError(Exception e)
        {
            Crashes.TrackError(e);
        }
    }
}
