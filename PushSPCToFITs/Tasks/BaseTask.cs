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
    public class BaseTask
    {
        

        #region Properties
        public bool _stopService = false;
        protected int _backTracking = 30;
        protected int _sleepSeconds = 15000; //If not defined in app.config file, then use this value to run pushing task every 15 seconds
        protected int _SPCHeaderID_topN_ToProcess = 10; //If not defined in app.config file, then use this value to get top 10 SPCHeaderIDs everytime
        protected int _StartSPCHeaderID = 1429374; //It's the SPCHeaderID tested in 2024/3/27 10:22AM Fabrinet time
        private string userName = "";
        private System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        private System.Diagnostics.Stopwatch swd = new System.Diagnostics.Stopwatch();
        private TimeSpan timespan = new TimeSpan();
        private TimeSpan timespand = new TimeSpan();
        private double _timespanSec = 0;
        private double _timespanSecd = 0;
        #endregion

        #region Methods
        /// <summary>
        /// Execute the overall data flow to insert SPC data into FITs, then update insert result in NEQdb 
        /// </summary>
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
                    ICollection<SPCHeader> spcHeaders = GetSPCHeader(); //Get top _SPCHeaderID_topN_ToProcess pending SPC Header using LIFO method
                    FITsClient fitsClient = new FITsClient();
                    FITSDLL.clsDB objFITs = fitsClient.GetFITsConnection();
                    FITsRequestParams requestParams = new FITsRequestParams();
                    
                    if (spcHeaders.Count > 0)
                    {
                        //Go through each SPCHeaderID for FITs data insert
                        foreach (SPCHeader spcHeader in spcHeaders)
                        {
                            sw.Restart();

                            ICollection<GetPendingData> pendingData = GetPendingData(spcHeader.ID); //Get SPC content from dbo.vwSPCData by feedbing SPC Header ID

                            bool FITsNeed_flag = CheckFITsNeed(pendingData);

                            //Check if the data is FITs needed, if yes, continue the process; if not, skip it and update FITsNeed_flag = false 
                            if (FITsNeed_flag)
                            {                                
                                //Check if the data exist in FITs already, if yes, update processedSuccessToFITs_flag = true; if not, continue the process
                                if (!fitsClient.FITsDataExists(objFITs, spcHeader, pendingData))
                                {
                                    string[] splitResultParams;
                                
                                    requestParams = fitsClient.GetSPCRequestParams(objFITs, pendingData);
                                    splitResultParams = requestParams.resultParams.ToString().Split(',');

                                    if (fitsClient.InsertSPCToFITs(objFITs, requestParams))
                                    {
                                        UpdateSPCHeader(spcHeader, true, splitResultParams[0], true);
                                        Log.Information($"FITs insert succeeded for SPCHeaderID {spcHeader.ID} , SerialNumber {spcHeader.SerialNumber} ");
                                    }
                                    else
                                    {
                                        UpdateSPCHeader(spcHeader, false, splitResultParams[0], true);
                                        Log.Information($"FITs insert failed for SPCHeaderID {spcHeader.ID} , SerialNumber {spcHeader.SerialNumber} ");
                                    }
                                }
                                else
                                {
                                    UpdateSPCHeader(spcHeader, true, spcHeader.Tracking_number, true);
                                    Log.Information($"FITs corrected processedSuccessToFITs_flag to be true for SPCHeaderID {spcHeader.ID} , SerialNumber {spcHeader.SerialNumber} ");
                                   
                                }
                            }
                            else
                            {
                                UpdateSPCHeader(spcHeader, false, null, false);
                                Log.Information($"FITs does not need for SPCHeaderID {spcHeader.ID} , SerialNumber {spcHeader.SerialNumber} ");
                            }
                        }
                        Log.Information($"Completed pushing SPC header ID with Qty = {spcHeaders.Count} ");
                    }
                    System.Threading.Thread.Sleep(_sleepSeconds); //  Stop for at least 1 second to prevent continuous error retries
                }
                catch (Exception e)
                {
                    _stopService = true;  //Stop the service if the exception is not processed by existed catch block

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
            _SPCHeaderID_topN_ToProcess = Common.ReadAppSetting("SPCHeaderID_topN_ToProcess", _SPCHeaderID_topN_ToProcess);
            _StartSPCHeaderID = Common.ReadAppSetting("StartSPCHeaderID", _StartSPCHeaderID);
        }

        /// <summary>
        /// Get top _SPCHeaderID_topN_ToProcess pending SPCHeader list
        /// </summary>
        /// <returns>Pending SPCHeaders to be pushed into FITs</returns>
        protected ICollection<SPCHeader> GetSPCHeader()
        {

            ICollection<SPCHeader> toProcess = new List<SPCHeader>();

            using (NEQdbContext nEQdbContext = new NEQdbContext())
            {
                toProcess = nEQdbContext.SPCHeader
                    .Where(sh => (sh.FITsNeed_flag == true || sh.FITsNeed_flag == null) && (sh.ProcessedSuccessToFITs_flag != true || sh.ProcessedSuccessToFITs_flag == null) && sh.ID > _StartSPCHeaderID)
                    .OrderBy(sh => sh.ID)
                    .Take(_SPCHeaderID_topN_ToProcess)
                    .ToList();
            }

            Log.Information($"Completed GetSPCHeader() to get pending top {toProcess.Count} SPCHeaderIDs");
            return toProcess;


        }

        /// <summary>
        /// Get SPC Data details for specific SPCHeaderID
        /// </summary>
        /// <param name="SPCHeaderID"></param>
        /// <returns>Pending SPCData details to be pushed into FITs</returns>
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

                Log.Information($"Completed GetPendingData to get pending SPCHeaderID = {SPCHeaderID}, data Qty = {toProcess.Count}");

            }
            catch (Exception e)
            {
                _stopService = true;
                Log.Error($"Class-- >{this.GetType().Name} Method-->{System.Reflection.MethodBase.GetCurrentMethod().Name}   Error-->{e.Message}");
            }
            return toProcess;
        }

        /// <summary>
        /// Update FITs insert result in NEQdb SPCHeader table
        /// </summary>
        /// <param name="sh">Position SPCHeader row</param>
        /// <param name="processedSuccessToFITs_flag">FITs fn_Insert success/failure</param>
        /// <param name="trackingNumber">FITs tracking number to position the data in FITs</param>
        /// <param name="fitsNeed_flag">Identify the SPC data is needed by FITs or not</param>
        protected void UpdateSPCHeader(SPCHeader spcHeader, bool processedSuccessToFITs_flag, string trackingNumber, bool fitsNeed_flag)
        {
            try
            {
                using (NEQdbContext nEQdbContext = new NEQdbContext())
                {
                    SPCHeader sph = nEQdbContext.SPCHeader
                        .Where(spch => spch.ID == spcHeader.ID)
                        .FirstOrDefault();

                    if (fitsNeed_flag)
                    {
                        timespan = sw.Elapsed;
                        _timespanSec = timespan.TotalSeconds;
                        sph.Process_time = _timespanSec;

                        sph.ProcessedSuccessToFITs_flag = processedSuccessToFITs_flag;
                        sph.ProcessToFITs_date = DateTime.Now;
                        sph.ProcessToFITs_user = userName;
                        sph.Tracking_number = trackingNumber;
                    }

                    sph.FITsNeed_flag = fitsNeed_flag;

                    nEQdbContext.SaveChanges();

                }
            }
            catch (Exception e)
            {
                Log.Error($"Class-- >{this.GetType().Name} Method-->{System.Reflection.MethodBase.GetCurrentMethod().Name}   Error-->{e.Message}");
            }
        }

        /// <summary>
        /// This function is used for FITs testing with hard coded data, developer can follow to feed similiar data to test if inserting FITs can be successful
        /// </summary>
        /// TODO: use security jason to avoid plain text storing credentials
        public void TestFITs()
        {
            Log.Information($"The FITsConnection function starts. ");
            FITSDLL.clsDB objFITS = new clsDB();

            string dbFITSName = "dbNlight";
            string userName = "nLight";
            string password = "n@AAz87ber";

            //string modelTypeQuery = "SPC for Element";
            //string operationQuery = "SPC15";
            //string serialNumberQuery = "SPC152435_006";
            //string revisionQuery = "";
            //string labelParamsQuery = "Tracking number,CoS Part number,Golden Sample SN,SE Station#,Power,Wavelength";
            //string fspQuery = ",";

            string modelType = "SPC for Element";
            string operation = "SPC12";
            string serialNumber = "T4XVMN";
            int operationType = 0;
            string labelParams = "Tracking number,FBN W/O,Supercarrier SN,SC Part number,FAC Station#,FAC Beam Width,FAC Pointing";
            string resultParams = "SPC122437_050,P406031802,T4XVMN,1098828,element FAC 10,0.3,450";
            string revision = "";
            string fsp = ",";
            string employeeNo = "Auto upload";
            string shift = "";
            string machine = "element FAC 10";


            //DateTime timestamp2 = Convert.ToDateTime("2024-02-27 19:37:30.303");
            DateTime timestamp2 = DateTime.Now;
            string timestampStr = timestamp2.ToString("yyyy-MM-dd HH:mm:ss");
            DateTime timestamp = DateTime.ParseExact(timestampStr, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

            //Log.Information($"timestamp: {timestamp}");
            try
            {
                ServiceResult LogonResult = objFITS.Logon(dbFITSName, userName, password);
                Log.Information($"The FITs Logon result is {LogonResult.result}, message is {LogonResult.message}, outputValue is {LogonResult.outputValue.ToString()} ");

                //ServiceResultQuery objResultQuery = objFITS.fn_Query(modelTypeQuery, operationQuery, revisionQuery, serialNumberQuery, labelParamsQuery, fspQuery);
                //Log.Information($"The FITs fn_Query result is {objResultQuery.result}, messge is {objResultQuery.message}, outputValue is {objResultQuery.outputValue.ToString()} ");

                ServiceResult objResult = objFITS.fn_Handshake(modelType, operation, serialNumber);
                Log.Information($"The FITs fn_Handshake result is {objResult.result}, messge is {objResult.message}, outputValue is {objResult.outputValue.ToString()} ");

                string[] splitHandshakeMessage = objResult.message.ToString().Split(' ');
                string trackingNumber = splitHandshakeMessage[splitHandshakeMessage.Length - 1];
                resultParams = trackingNumber + "," + resultParams;

                Log.Information($"labelParams: {labelParams}");
                Log.Information($"labelResults: {resultParams}");
                objResult = objFITS.fn_Insert(operationType, modelType, operation, labelParams, resultParams, fsp, employeeNo, shift, machine, timestamp, revision);
                Log.Information($"The FITs fn_Insert with Tracking number result is {objResult.result}, messge is {objResult.message}, outputValue is {objResult.outputValue.ToString()} ");


            }
            catch (Exception e)
            {
                Log.Error($"Class-- >{this.GetType().Name} Method-->{System.Reflection.MethodBase.GetCurrentMethod().Name}   Error-->{e.Message}");
            }


        }

        /// <summary>
        /// Check if the SPC data is needed to push to FITs since charts "element fac" and "element sac dev from target" do not exist in FITs/SPC 
        /// </summary>
        /// <param name="pendingData"></param>
        /// <returns>It's true if it's needed by FITs, false if it's not needed by FITs</returns>
        protected bool CheckFITsNeed(ICollection<GetPendingData> pendingData)
        {
            bool FITsNeed = true;
            GetPendingData gd1 = pendingData.First();
            switch (gd1.ChartName)
            {
                case "element fac": FITsNeed = false; break;
                case "element sac dev from target": FITsNeed = false; break;
                default: FITsNeed = true; break;
            }
            return FITsNeed;
        }

        #endregion

    }
}
