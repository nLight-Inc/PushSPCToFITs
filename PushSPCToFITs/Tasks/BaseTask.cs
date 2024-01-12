using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using PushSPCToFITs.Context;
using PushSPCToFITs.Helpers;
using PushSPCToFITs.Models;
using Serilog;
using System.Text;
using FITSDLL;


namespace PushSPCToFITs.Tasks
{
    public class BaseTask<Tclass> where Tclass :  class
    {
        #region Singleton

        /// <summary>
        /// Static instance. Needs to use lambda expression
        /// to construct an instance (since constructor is private).
        /// </summary>
        private static readonly Lazy<Tclass> sInstance = new Lazy<Tclass>(() => CreateInstanceOfT());

        /// <summary>
        /// Creates an instance of T via reflection since T's constructor is expected to be private.
        /// </summary>
        /// <returns></returns>
        private static Tclass CreateInstanceOfT()
        {
            return Activator.CreateInstance(typeof(Tclass), true) as Tclass;
        }

        /// <summary>
        /// Gets the instance of this singleton.
        /// </summary>
        public static Tclass Instance { get { return sInstance.Value; } }

        #endregion

        #region Properties
        public bool _stopService = false;
        protected int _backTracking = 30;
        protected int _sleepSeconds = 15000; //Run pushing task every 15 seconds
        protected int _SPCHeaderID_topN_ToProcess = 10;
        private string userName = "";
        private System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        private System.Diagnostics.Stopwatch swd = new System.Diagnostics.Stopwatch();
        private TimeSpan timespan = new TimeSpan();
        private TimeSpan timespand = new TimeSpan();
        private double _timespanSec = 0;
        private double _timespanSecd = 0;
        private bool _updateDataSuccessFlag = false;
        #endregion

        #region Methods
        public void RunTask()
        {
            this.SetTaskSpecificDefaults();

           

            System.Security.Principal.WindowsIdentity currentUser = System.Security.Principal.WindowsIdentity.GetCurrent();
            userName = currentUser.Name.ToString().Split('\\')[1] + "@nlight.net";
            
            
            do
            {
                StartLogging.StartLogger();
                try
                {
                    //  Stop for at least 1 second to prevent continuous error retries
                    

                    SetTaskSpecificDefaults();

                    ICollection<SPCHeader> SPCHeader =  GetSPCHeader();

                    if (SPCHeader.Count >0 )
                    {
                        foreach (SPCHeader s in SPCHeader)
                        {
                            sw.Start();
                            ICollection<GetPendingData> PendingData = GetPendingData(s.ID);

                            foreach (GetPendingData p in PendingData)
                            {
                                swd.Start();
                                ProcessSPCDataToFITs(p);
                                _updateDataSuccessFlag = UpdateSPCData(p);
                                swd.Stop();
                            }

                            if (_updateDataSuccessFlag)
                            {
                                UpdateSPCHeader(s);
                            }
                            sw.Stop();
                        }

                        Log.Information($"Completed pushing SPC header ID. Qty = {SPCHeader.Count} ");
                    }
                    System.Threading.Thread.Sleep(_sleepSeconds);
                }
                catch (Exception e)
                {
                    _stopService = true;
                    Log.Error($"Class-- >{this.GetType().Name} Method-->{System.Reflection.MethodBase.GetCurrentMethod().Name}   Error-->{e.Message}");
                }

            } while (!_stopService);
        }

        protected void SetTaskSpecificDefaults()
        {
            _backTracking = Common.ReadAppSetting("BackTracking", _backTracking);
            _sleepSeconds = Common.ReadAppSetting("SleepSeconds", _sleepSeconds);
        }

        protected void ProcessSPCDataToFITs(GetPendingData pendingData)
        {

        }

        protected ICollection<SPCHeader> GetSPCHeader()
        {

            ICollection<SPCHeader> toProcess = new List<SPCHeader>();

            using (NEQdbContext nEQdbContext = new NEQdbContext())
            {
                toProcess = nEQdbContext.SPCHeader
                    .Where(sh => sh.ProcessedSuccessToFITs_flag != true || sh.ProcessedSuccessToFITs_flag == null)
                    .OrderBy(sh => sh.ID)
                    .Take(_SPCHeaderID_topN_ToProcess)
                    .ToList();
            }

            Log.Information($"Completed GetSPCHeader() to get pending top SPCHeaderID. Qty = {toProcess.Count}");
            return toProcess;

            
        }
        protected ICollection<GetPendingData> GetPendingData(int SPCHeaderID)
        {
            ICollection<GetPendingData> toProcess = new List<GetPendingData>();
            try
            {
                

                using (NEQdbContext nEQdbContext = new NEQdbContext())
                {
                    toProcess = nEQdbContext.GetPendingData
                        .FromSqlInterpolated($"SELECT * FROM [dbo].[GetPendingSPCData] ({SPCHeaderID})")
                        .ToList();
                }

                Log.Information($"Completed GetPendingData(int SPCHeaderID) to get pending SPC data. Qty = {toProcess.Count}");
                
            }
            catch (Exception e)
            {
                _stopService = true;
                Log.Error($"Class-- >{this.GetType().Name} Method-->{System.Reflection.MethodBase.GetCurrentMethod().Name}   Error-->{e.Message}");
            }
            return toProcess;
        }

        protected void UpdateSPCHeader(SPCHeader sh)
        {
            try
            {
                using (NEQdbContext nEQdbContext = new NEQdbContext())
                {
                    
                    SPCHeader sph = nEQdbContext.SPCHeader
                        .Where(spch => spch.ID == sh.ID)
                        .FirstOrDefault();


                    sph.ProcessedSuccessToFITs_flag = true;
                    sph.ProcessToFITs_date = DateTime.Now;
                    sph.ProcessToFITs_user = userName;

                    timespan = sw.Elapsed;
                    _timespanSec = timespan.TotalSeconds;
                    sph.Process_time = _timespanSec;
                    
                    nEQdbContext.SaveChanges();

                }
            }
            catch (Exception e)
            {
                Log.Error($"Class-- >{this.GetType().Name} Method-->{System.Reflection.MethodBase.GetCurrentMethod().Name}   Error-->{e.Message}");
            }
        }

        protected bool UpdateSPCData(GetPendingData p)
        {
            try
            {
                using (NEQdbContext nEQdbContext = new NEQdbContext())
                {
                    SPCData spd = nEQdbContext.SPCData
                        .Where(sd => sd.ID == p.ID && (sd.ProcessedSuccessToFITs_flag != true || sd.ProcessedSuccessToFITs_flag == null))
                        .FirstOrDefault();

                    if (spd != null)
                    {
                        spd.ProcessedSuccessToFITs_flag = true;
                        spd.ProcessToFITs_date = DateTime.Now;
                        spd.ProcessToFITs_user = userName;

                        timespand = swd.Elapsed;
                        _timespanSecd = timespand.TotalSeconds;
                        spd.Process_time = _timespanSecd;

                        nEQdbContext.SaveChanges();
                    }
                    
                }

                Log.Information($"Completed UpdateSPCData(GetPendingData p) to update specific SPCData row. ID = {p.ID}");
                return true;
            }
            catch (Exception e)
            {
                Log.Error($"Class-- >{this.GetType().Name} Method-->{System.Reflection.MethodBase.GetCurrentMethod().Name}   Error-->{e.Message}");
                return false;
            }


        }

        protected clsDB FITsConnection()
        {
            FITSDLL.clsDB objFITS = new clsDB();

            string dbFITSName = "dbNlight";
            string userName = "nLight";
            string password = "n@AAz87ber";

            try
            { 
                ServiceResult LogonResult = objFITS.Logon(dbFITSName, userName, password); 
            }
            catch (Exception e)
            {
                Log.Error($"Class-- >{this.GetType().Name} Method-->{System.Reflection.MethodBase.GetCurrentMethod().Name}   Error-->{e.Message}");
            }

            return objFITS ;

        }

        protected void InsertFITs(SPCData SPCData)
        {

        }
        #endregion

    }
}
