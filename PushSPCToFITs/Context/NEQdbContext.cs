using Microsoft.EntityFrameworkCore;
using PushSPCToFITs.Models;
using Serilog;
using System;

namespace PushSPCToFITs.Context
{
    public class NEQdbContext : DbContext
    {
        public DbSet<SPCHeader> SPCHeader { get; set; }

        public DbSet<SPCData> SPCData { get; set; }

        public DbSet<GetPendingData> GetPendingData { get; set; }

        public string NEQdbServer;

        public NEQdbContext()
        {
        }

        public void NeqdbContext(bool isGetServer)
        {
            if (isGetServer && string.IsNullOrEmpty(NEQdbServer))
            {
                string connString = Helpers.Common.GetNeqServerConnString();
                var builder = new System.Data.SqlClient.SqlConnectionStringBuilder(connString);
                NEQdbServer = builder.DataSource;
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connString = Helpers.Common.GetNeqServerConnString();

            optionsBuilder
                   .UseSqlServer(connString, options => options.EnableRetryOnFailure());

            LogFirstRunThroughClassNeqdb.LogFirstRun(connString);
        }
        
        static class LogFirstRunThroughClassNeqdb
        {
            private static volatile bool _firstRunThrough = true;

            private static object lockObject = new object();

            internal static void LogFirstRun(string connString)
            {
                lock (lockObject)
                {
                    if (_firstRunThrough)
                    {
                        Log.Information($"LogFirstRunThroughClassNeqdb, connString:  {connString}");
                        _firstRunThrough = false;
                    }
                }
            }
        }
    }
}
