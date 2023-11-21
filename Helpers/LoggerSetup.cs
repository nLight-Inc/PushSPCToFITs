using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices.ActiveDirectory;
using Serilog;
using Serilog.Core;
using System.Diagnostics;
using System.Reflection;

namespace PushSPCToFITs.Helpers
{
    static class LogFirstRunThroughClass
    {
        private static volatile bool _firstRunThrough = true;

        private static object lockObject = new object();

        internal static void StartLoggingOnFirstRunThrough()
        {
            lock (lockObject)
            {
                if (_firstRunThrough)
                {
                    StartLogging();
                    try
                    {
                        Log.Information("NeqdbApi Version {0}", Assembly.GetExecutingAssembly().GetName().Version.ToString());
                    }
                    catch (Exception e)
                    {
                        Log.Warning("Unable to get version information");
                    }
                    _firstRunThrough = false;
                }
            }
        }

        private static void StartLogging()
        {
            string logPath = AppSettings.ReadAppSetting("LogFilePath", @"C:\Logs\NeqdbApi\");
            string logName = AppSettings.ReadAppSetting("LogName", @"NeqdbApi_{Date}.txt");

            Serilog.Debugging.SelfLog.Enable(msg => Debug.WriteLine(msg));

            var levelSwitch = new LoggingLevelSwitch();

            try
            {
                Log.Logger = new LoggerConfiguration()
                    //.MinimumLevel.Information()
                    .MinimumLevel.ControlledBy(levelSwitch)
                    .WriteTo.RollingFile(logPath + logName, shared: true, retainedFileCountLimit: 45)
                    .WriteTo.ColoredConsole()
                    .WriteTo.Debug()
                    .CreateLogger();

                string loglevel = AppSettings.ReadAppSetting("LogLevel", "Debug");

                switch (loglevel)
                {
                    case "Verbose":
                        levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Verbose;
                        break;
                    case "Debug":
                        levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Debug;
                        break;
                    case "Information":
                        levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Information;
                        break;
                    case "Warning":
                        levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Warning;
                        break;
                    case "Error":
                        levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Error;
                        break;
                    case "Fatal":
                        levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Fatal;
                        break;
                    default:
                        levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Information;
                        break;
                }

                //Serilog.Debugging.SelfLog.Enable(Console.Error);

                Log.Information("Console app starting in NeqdbApi");
            }
            catch (Exception e)
            {
                Log.Error($"Error starting logging:  {e.Message}");
                if (e.InnerException != null)
                {
                    Log.Error($"Inner Exception:  {e.InnerException.Message}");
                }
            }
        }
    }
    /// <summary>
    ///   Class to start logging
    /// </summary>
    public static class LoggerSetup
    {
        /// <summary>
        ///  Starts Serilog
        /// </summary>
        public static void StartLogging()
        {
            LogFirstRunThroughClass.StartLoggingOnFirstRunThrough();
        }
    }
}
