using System;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.DirectoryServices.ActiveDirectory;
using Serilog;

namespace PushSPCToFITs.Helpers
{
    public static class Common
    {
        public static T ReadAppSetting<T>(string searchKey, T defaultValue, StringComparison compare = StringComparison.Ordinal)
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

        public static decimal RoundUp(decimal number, int digits)
        {
            decimal multiplier = (decimal)Math.Pow(10, digits);
            return Math.Ceiling(number * multiplier) / multiplier;
        }

        /// <summary>
        ///   Gets the neqdb server connection string for the main server based on the current domain.
        /// </summary>
        /// <returns></returns>
        public static string GetNeqServerConnString()
        {
            var domainName = Domain.GetComputerDomain();
            string localDomainName = domainName.Name;
            string firstChars = localDomainName.ToLower().Substring(0, 2);

            string neqConnString = string.Empty;

            switch (firstChars)
            {
                case "nl":
                    neqConnString = "server=NLI-SQL04-SVR;database=NEQDB;trusted_connection=true; ConnectRetryCount=5; ConnectRetryInterval=15";
                    break;
                case "hi":
                    neqConnString = "server=HIL-SQL04-SVR;database=NEQDB;trusted_connection=true; ConnectRetryCount=5; ConnectRetryInterval=15";
                    break;
                case "sh":
                    //neqConnString = "server=SHA-SQL04-SVR;database=NEQDB;trusted_connection=true; ConnectRetryCount=5; ConnectRetryInterval=15";
                    neqConnString = "server=FBR-SQL04-DEV;database=NEQDB;trusted_connection=true; ConnectRetryCount=5; ConnectRetryInterval=15";
                    break;
                case "fa":
                    neqConnString = "server=FBR-SQL12-SVR;database=NEQDBTEST;trusted_connection=true; ConnectRetryCount=5; ConnectRetryInterval=15";
                    //neqConnString = "server=FBR-SQL04-SVR;database=NEQDB;trusted_connection=true; ConnectRetryCount=5; ConnectRetryInterval=15"; 
                    break;
                default:
                    neqConnString = "server=NLI-SQL04-SVR;database=NEQDB;trusted_connection=true; ConnectRetryCount=5; ConnectRetryInterval=15";
                    break;
            }

            return neqConnString;
        }

    }
}
