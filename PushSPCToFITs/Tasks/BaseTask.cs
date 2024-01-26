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
                    SetTaskSpecificDefaults();

                    ICollection<SPCHeader> SPCHeader =  GetSPCHeader(); //Get top _SPCHeaderID_topN_ToProcess pending SPC Header using LIFO method

                    if (SPCHeader.Count > 0 )
                    {
                        foreach (SPCHeader s in SPCHeader)
                        {
                            sw.Start();
                            ICollection<GetPendingData> pendingData = GetPendingData(s.ID); //Get SPC content from dbo.vwSPCData by feedbing SPC Header ID

                            clsDB objFITs = GetFITsConnection();
                            bool FITsNeed_flag = CheckFITsNeed(pendingData);
                            if (FITsNeed_flag)
                            {
                                InsertFITs(pendingData, objFITs);
                            }
                            
                            //foreach (GetPendingData p in PendingData)
                            //{
                            //    swd.Start();
                            //    ProcessSPCDataToFITs(p);
                            //    //_updateDataSuccessFlag = UpdateSPCData(p);
                            //    swd.Stop();
                            //}

                            if (_updateDataSuccessFlag)
                            {
                                UpdateSPCHeader(s);
                            }
                            sw.Stop();
                        }

                        Log.Information($"Completed pushing SPC header ID with Qty = {SPCHeader.Count} ");
                    }
                    System.Threading.Thread.Sleep(_sleepSeconds); //  Stop for at least 1 second to prevent continuous error retries
                }
                catch (Exception e)
                {
                    _stopService = true;

                    StringBuilder body = new StringBuilder();
                    body.AppendLine().Append("\n\nThe PushSPCToFITs service has been stopped");
                    body.AppendLine().Append("\n\nRefer to error message: " + e.Message);                    
                    string subject = "PushSPCToFITs service stopped";
                    SendEmail.SendNotification(body.ToString(), subject);

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
                    .OrderByDescending(sh => sh.ID)
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

        /// <summary>
        /// This function is used for FITs testing with hard coded data
        /// </summary>
        /// <returns></returns>
        public void TestFITs()
        {
            Log.Information($"The FITsConnection function starts. ");
            FITSDLL.clsDB objFITS = new clsDB();

            string dbFITSName = "dbNlight";
            string userName = "nLight";
            string password = "n@AAz87ber";

            string modelTypeQuery = "SPC for Element";
            string operationQuery = "SPC11";
            string serialNumberQuery = "T59CHU";
            string revisionQuery = "";
            string labelParamsQuery = "Tracking number,Golden Sample SN";
            string fspQuery = ",";

            string modelType = "SPC for Element";
            string operation = "SPC11";
            string serialNumber = "T59CHU";
            int operationType = 0;
            //string labelParams2 = "Tracking number,Golden Sample SN,Golden Count time,SC Part number,FAC Station#,FAC Beam Width,FAC Pointing,Comment,Result,Failure code";
            string labelParams2 = "Tracking number,Golden Sample SN,FAC Station#,FAC Beam Width,FAC Pointing";
            string revision = "";
            string fsp = ",";
            string employeeNo = "000001";
            string shift = "";
            string machine = "FAC No.5";


            DateTime timestamp2 = Convert.ToDateTime("12/21/2023  6:58:00 AM");
            string timestampStr = timestamp2.ToString("yyyy-MM-dd hh:mm:ss");
            DateTime timestamp = DateTime.ParseExact(timestampStr, "yyyy-MM-dd hh:mm:ss", System.Globalization.CultureInfo.InvariantCulture );


            try
            { 
                ServiceResult LogonResult = objFITS.Logon(dbFITSName, userName, password);
                Log.Information ($"The FITs Logon result is {LogonResult.result}, message is {LogonResult.message}, outputValue is {LogonResult.outputValue.ToString()} ");

                ServiceResultQuery objResultQuery = objFITS.fn_Query(modelTypeQuery, operationQuery, revisionQuery, serialNumberQuery, labelParamsQuery, fspQuery);
                Log.Information($"The FITs fn_Query result is {objResultQuery.result}, messge is {objResultQuery.message}, outputValue is {objResultQuery.outputValue.ToString()} ");

                ServiceResult objResult = objFITS.fn_Handshake(modelType, operation, serialNumber);
                Log.Information($"The FITs fn_Handshake result is {objResult.result}, messge is {objResult.message}, outputValue is {objResult.outputValue.ToString()} ");

                string[] splitHandshakeMessage = objResult.message.ToString().Split(' ');
                string trackingNumber = splitHandshakeMessage[splitHandshakeMessage.Length - 1];
                string labelResults2 = trackingNumber + ",T59CHU,FAC No.5,1.640064,286.7";

                Log.Information($"labelParams: {labelParams2}");
                Log.Information($"labelResults: {labelResults2}");
                objResult = objFITS.fn_Insert(operationType, modelType, operation, labelParams2, labelResults2, fsp, employeeNo, shift, machine, timestamp, revision);
                Log.Information($"The FITs fn_Insert with Tracking number result is {objResult.result}, messge is {objResult.message}, outputValue is {objResult.outputValue.ToString()} ");

               
            }
            catch (Exception e)
            {
                Log.Error($"Class-- >{this.GetType().Name} Method-->{System.Reflection.MethodBase.GetCurrentMethod().Name}   Error-->{e.Message}");
            }


        }

        /// <summary>
        /// Get FITs object through FITSDLL fn_Logon function
        /// </summary>
        /// <returns></returns>
        protected clsDB GetFITsConnection()
        {
            Log.Information($"The FITsConnection function starts. ");
            FITSDLL.clsDB objFITS = new clsDB();

            string dbFITSName = "dbNlight";
            string userName = "nLight";
            string password = "n@AAz87ber";

            ServiceResult LogonResult = objFITS.Logon(dbFITSName, userName, password);

            try
            {
                if (LogonResult.result == 1)
                {
                    Log.Information($"The FITs Logon succeeded, result is {LogonResult.result}, message is {LogonResult.message}, outputValue is {LogonResult.outputValue.ToString()} ");
                }
                else
                {
                    Log.Error($"The FITs Logon Logon failed, result is {LogonResult.result}, message is {LogonResult.message}, outputValue is {LogonResult.outputValue.ToString()} ");
                }
            }
            catch (Exception e)
            {
                _stopService = true;

                StringBuilder body = new StringBuilder();
                body.AppendLine().Append("\n\nThe PushSPCToFITs service has been stopped");
                body.AppendLine().Append("\n\nRefer to error message: " + e.Message);
                string subject = "PushSPCToFITs service stopped";
                SendEmail.SendNotification(body.ToString(), subject);

                Log.Error($"Class-- >{this.GetType().Name} Method-->{System.Reflection.MethodBase.GetCurrentMethod().Name}   Error-->{e.Message}");
            }

            return objFITS;
        }

        /// <summary>
        /// Check if the SPC data is needed to push to FITs since charts "element fac" and "element sac dev from target" do not exist in FITs 
        /// </summary>
        /// <param name="pendingData"></param>
        /// <returns></returns>
        protected bool CheckFITsNeed(ICollection<GetPendingData> pendingData)
        {
            bool FITsNeed = true;
            GetPendingData gd1 = pendingData.First();
            switch (gd1.ChartName)
            {
                case "element fac": FITsNeed = false; break;
                case "element sac dev from target": FITsNeed = false; break;
                default: FITsNeed = true;  break;
            }

            return FITsNeed;
        }
       
        protected void InsertFITs(ICollection<GetPendingData> pendingData, clsDB objFITS)
        {
            int operationType = 0;
            string modelType = string.Empty;
            string operation = string.Empty;
            GetPendingData gdf = pendingData.First();
            string serialNumber = gdf.SerialNumber;
            string labelParams = "Tracking number,Golden Sample SN,Golden Count time,SC Part number,FAC Station#,FAC Beam Width,FAC Pointing,Comment,Result,Failure code";
            string SCPartNumber = gdf.ChartName;
            string revision = "";
            string fsp = ",";
            string employeeNo = "000001";
            string shift = "";
            string machine = gdf.Process;
            DateTime timeTest = Convert.ToDateTime(gdf.tmTest);
            string timestampStr = timeTest.ToString("yyyy-MM-dd hh:mm:ss");
            DateTime timeTestFITs = DateTime.ParseExact(timestampStr, "yyyy-MM-dd hh:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            string comment = "Test";
            string result = "NA";
            string failureCode = "0";

            //For SPC12:
            string FBNWO = gdf.WorkOrder;
            string supercarrier = string.Empty;

            //For SPC11:
            string GoldenCountTime = "1";            
            float FACBeanWidth;
            float FACPointing;

            //For SPC16
            
            switch (gdf.ChartName)
            {
                case "element fac dev from target": 
                    modelType = "SPC for Element"; 
                    operation = "SPC12"; 
                    break;
                case "element fac golden sample":
                    modelType = "SPC for Element";
                    operation = "SPC11";
                    
                    
                    break;
                case "element mirror golden sample":
                    modelType = "SPC for Element";
                    operation = "SPC16";
                    break;
                case "element mt golden sample":
                    modelType = "SPC for Element";
                    operation = "SPC17";
                    break;
                case "element sac":
                    modelType = "SPC for Element";
                    operation = "SPC14";
                    break;
                case "element sac golden sample":
                    modelType = "SPC for Element";
                    operation = "SPC13";
                    break;
                case "se golden sample":                    
                    if(gdf.PartGroup == "GS CS") //for PartGroup=GS CS
                    {
                        modelType = "SPC for Element";
                        operation = "SPC15"; 
                    }
                    else //for PartGroup=GS Pearl Chiplet
                    {
                        modelType = "SPC for Pearl";
                        operation = "SPCP04"; 
                    }                    
                    break;
            }

            foreach (GetPendingData gd in pendingData)
            {
                
            }
            


            try
            {
                
                ServiceResult objResult = objFITS.fn_Handshake(modelType, operation, serialNumber);
                Log.Information($"The FITs fn_Handshake result is {objResult.result}, messge is {objResult.message}, outputValue is {objResult.outputValue.ToString()} ");

                string[] splitHandshakeMessage = objResult.message.ToString().Split(' ');
                string trackingNumber = splitHandshakeMessage[splitHandshakeMessage.Length - 1];
                string labelResults2 = trackingNumber + ",T59CHU,1,GS element FAC,FAC No.5,1.640064,272.04,Test,PASS,0";

                Log.Information($"labelParams: {labelParams}");
                Log.Information($"labelResults: {labelResults2}");
                objResult = objFITS.fn_Insert(operationType, modelType, operation, labelParams, labelResults2, fsp, employeeNo, shift, machine, timeTestFITs, revision);
                Log.Information($"The FITs fn_Insert with Tracking number result is {objResult.result}, messge is {objResult.message}, outputValue is {objResult.outputValue.ToString()} ");


            }
            catch (Exception e)
            {
                Log.Error($"Class-- >{this.GetType().Name} Method-->{System.Reflection.MethodBase.GetCurrentMethod().Name}   Error-->{e.Message}");
            }

        }
        #endregion

    }
}
