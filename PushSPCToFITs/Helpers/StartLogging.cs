using Serilog;
using Serilog.Core;
using System;
using System.Diagnostics;
using System.Reflection;

namespace PushSPCToFITs.Helpers
{
    public static class StartLogging
    {
        private static volatile bool _firstRunThrough = true;

        private static object lockObject = new object();

        public static void StartLogger()
        {
            try
            {
                string logPath = Common.ReadAppSetting("LogFilePath", @"C:\nLIGHT_Logs\nLIGHT_PushSPCToFITs\");
                string logName = Common.ReadAppSetting("LogName", @"PushSPCToFITs{Date}.txt");
                logName = logName.Replace("{Date}", DateTime.Now.ToString("yyyyMMdd"));

                var levelSwitch = new LoggingLevelSwitch();

                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.ControlledBy(levelSwitch)
                    .WriteTo.Console()
                    .WriteTo.File(logPath + logName, shared: true, retainedFileCountLimit: 45)
                    .CreateLogger();

                string loglevel = Common.ReadAppSetting("LogLevel", "Information");

                Log.Information($"Log level:  {loglevel}");

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
                        levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Verbose;
                        break;
                }

                Serilog.Debugging.SelfLog.Enable(Console.Error);
                Log.Information("PushSPCToFITs Service: {0} has successfully started.", Assembly.GetEntryAssembly().GetName().Version.ToString());
                Log.Information("PushSPCToFITs: {0}", Assembly.GetExecutingAssembly().GetName().Version.ToString());

            }
            catch (Exception e)
            {
                using (EventLog eventLog = new EventLog("Application"))
                {
                    eventLog.Source = "Application";
                    eventLog.WriteEntry("PushSPCToFITs Service failed to start Serilog Logging " + e.ToString(), EventLogEntryType.Error, 101, 1);
                    if (e.InnerException != null)
                        eventLog.WriteEntry("Inner Exception: " + e.InnerException.Message.ToString(), EventLogEntryType.Error, 101, 1);
                }
            }
        }
    }
}
