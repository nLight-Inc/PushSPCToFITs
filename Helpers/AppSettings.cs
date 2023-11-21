using System;
using System.ComponentModel;
using System.Configuration;
using System.Linq;

namespace PushSPCToFITs.Helpers
{
    public static class AppSettings
    {        public static T ReadAppSetting<T>(string searchKey, T defaultValue, StringComparison compare = StringComparison.Ordinal)
        {
            if (ConfigurationManager.AppSettings.AllKeys.Any(key => string.Compare(key, searchKey, compare) == 0))
            {
                try
                { // see if it can be converted.
                    var converter = TypeDescriptor.GetConverter(typeof(T));
                    if (converter != null) defaultValue = (T)converter.ConvertFromString(ConfigurationManager.AppSettings.GetValues(searchKey).First());
                }
                catch { } // nothing to do just return the defaultValue
            }
            return defaultValue;
        }
    }
}
