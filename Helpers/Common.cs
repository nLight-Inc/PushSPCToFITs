using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Core;

namespace PushSPCToFITs.Helpers
{
    public static class Common
    {
        public static string TryGetElementValue(XElement parentElement, string elementName, string defaultValue = null)
        {
            var foundElement = parentElement.Element(elementName);

            if (foundElement != null)
            {
                return foundElement.Value;
            }

            return defaultValue;
        }

        public static string TryGetAttributeValue(XElement parentElement, string attributeName, string defaultValue = null)
        {
            var foundAttribute = parentElement.Attribute(attributeName);

            if (foundAttribute != null)
            {
                return foundAttribute.Value;
            }

            //           Log.Debug("Attribute {0} was not found", attributeName);
            return defaultValue;
        }

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

        /// <summary>
        ///   Gets the neqdb server connection string for the main server based on the current domain.
        ///   SITE_SPECIFIC
        /// </summary>
        /// <returns></returns>
        public static string GetNeqServerConnString()
        {
            var domainName = Domain.GetComputerDomain();
            string localDomainName = domainName.Name;
            string firstChars = localDomainName.Substring(0, 2);

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
                    neqConnString = "server=SHA-SQL04-SVR;database=NEQDB;trusted_connection=true; ConnectRetryCount=5; ConnectRetryInterval=15";
                    break;
                case "fb":
                    neqConnString = "server=FBR-SQL04-SVR;database=NEQDB;trusted_connection=true; ConnectRetryCount=5; ConnectRetryInterval=15";
                    break;
                case "to":
                    neqConnString = "server=TOR-SQL04-SVR;database=NEQDB;trusted_connection=true; ConnectRetryCount=5; ConnectRetryInterval=15";
                    break;
                default:
                    neqConnString = "server=NLI-SQL04-SVR;database=NEQDB;trusted_connection=true; ConnectRetryCount=5; ConnectRetryInterval=15";
                    break;
            }

            return neqConnString;
        }

        /// <summary>
        ///  Get Neqdb connection string from NLI-SQL04-SVR - Vancouver
        ///  SITE_SPECIFIC
        /// </summary>
        /// <returns></returns>
        public static string GetVancouverNeqServerConnString()
        {
            string neqConnString = "server=NLI-SQL04-SVR;database=NEQDB;trusted_connection=true; ConnectRetryCount=5; ConnectRetryInterval=15";

            return neqConnString;
        }

        public static string GetNeqConnString()
        {
            string server = ReadAppSetting("neqServer", @".\SQLEXPRESS");
            string database = ReadAppSetting("neqDb", "TestData1");
            string conn = "server=" + server + ";database=" + database + ";trusted_connection=true; ConnectRetryCount=5; ConnectRetryInterval=15";

            Log.Verbose($"NEQdbiAPI.Helpers.Common   GetNeqConnString--> {conn}");

            return conn;
        }

        public static string GetLeqConnString()
        {
            string server = ReadAppSetting("LEQdbServer", @".\SQLEXPRESS");
            string database = ReadAppSetting("LEQdbDatabase", "TestData1");

            string conn = "server=" + server + ";database=" + database + ";trusted_connection=true; ConnectRetryCount=5; ConnectRetryInterval=15";

            Log.Verbose($"NEQdbiAPI.Helpers.Common   GetLeqConnString--> {conn}");

            return conn;
        }

        //SITE_SPECIFIC
        public static string GetMetaDataConnString()
        {
            var domainName = Domain.GetComputerDomain();
            string localDomainName = domainName.Name;
            string firstChars = localDomainName.Substring(0, 2);

            string mdConnString = string.Empty;

            switch (firstChars)
            {
                case "nl":
                    mdConnString = "server=NLI-SQL04-SVR;database=AX_Item_Meta_Data;trusted_connection=true; ConnectRetryCount=5; ConnectRetryInterval=15";
                    break;
                case "hi":     //  Use Vancouver sql04 for meta data when in Hillsboro
                    mdConnString = "server=NLI-SQL04-SVR;database=AX_Item_Meta_Data;trusted_connection=true; ConnectRetryCount=5; ConnectRetryInterval=15";
                    break;
                case "sh":
                    mdConnString = "server=SHA-SQL04-SVR;database=AX_Item_Meta_Data;trusted_connection=true; ConnectRetryCount=5; ConnectRetryInterval=15";
                    break;
                case "fb":
                    mdConnString = "server=FBR-SQL04-SVR;database=AX_Item_Meta_Data;trusted_connection=true; ConnectRetryCount=5; ConnectRetryInterval=15";
                    break;
                case "to":
                    mdConnString = "server=TOR-SQL04-SVR;database=AX_Item_Meta_Data;trusted_connection=true; ConnectRetryCount=5; ConnectRetryInterval=15";
                    break;
                default:
                    mdConnString = "server=NLI-SQL04-SVR;database=AX_Item_Meta_Data;trusted_connection=true; ConnectRetryCount=5; ConnectRetryInterval=15";
                    break;
            }

            Log.Verbose($"NEQdbiAPI.Helpers.Common   GetMetaDataConnString--> {mdConnString}");

            return mdConnString;
        }

        public static T TryGetAttributeValueByValueType<T>(XElement parentElement, string attributeName, T defaultValue)
        {
            var foundAttribute = parentElement.Attribute(attributeName);

            if (foundAttribute != null)
            {
                try
                { // see if it can be converted.
                    var converter = TypeDescriptor.GetConverter(typeof(T));
                    if (converter != null) defaultValue = (T)converter.ConvertFromString(foundAttribute.Value);
                }
                catch { }
            }

            return defaultValue;
        }
    }
}

