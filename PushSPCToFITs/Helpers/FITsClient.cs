using System;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.DirectoryServices.ActiveDirectory;
using Serilog;
using System.Collections.Generic;
using PushSPCToFITs.Models;
using System.Text;
using FITSDLL;
using PushSPCToFITs.Helpers;

namespace PushSPCToFITs.Helpers
{
    public class FITsClient
    {
        /// <summary>
        /// Get FITs object through FITSDLL fn_Logon function
        /// </summary>
        /// <returns></returns>
        public clsDB GetFITsConnection()
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
                StringBuilder body = new StringBuilder();
                body.AppendLine().Append("\n\nThe PushSPCToFITs service cannot run logon using FITSDLL");
                body.AppendLine().Append("\n\nRefer to error message: " + e.Message);
                string subject = "[SW Alert]: PushSPCToFITs service cannot run logon using FITSDLL";
                SendEmail.SendNotification(body.ToString(), subject);

                Log.Error($"Class-- >{this.GetType().Name} Method-->{System.Reflection.MethodBase.GetCurrentMethod().Name}   Error-->{e.Message}");
            }

            return objFITS;
        }

        /// <summary>
        /// Get Tracking number for FITSDLL fn_Insert execution
        /// </summary>
        /// <param name="objFITs">FITs object</param>
        /// <param name="requestParams">FITs request parameters</param>
        /// <param name="resultParams">FITs result parameters of requst parameters to be pushed into FITs</param>
        /// <returns></returns>
        public string GetTrackingNumber(clsDB objFITs, FITsRequestParams requestParams, FITsResultParams resultParams)
        {
            string serialNumber = string.Empty;
            string trackingNumber = string.Empty;
            try
            {
                if (resultParams.goldenSampleSN != string.Empty)
                {
                    serialNumber = resultParams.goldenSampleSN;
                }
                else
                {
                    serialNumber = resultParams.supercarrierSN;
                }
                ServiceResult objResult = objFITs.fn_Handshake(requestParams.modelType, requestParams.operation, serialNumber);
                Log.Information($"The FITs fn_Handshake result is {objResult.result}, messge is {objResult.message}, outputValue is {objResult.outputValue.ToString()} ");

                string[] splitHandshakeMessage = objResult.message.ToString().Split(' ');
                trackingNumber = splitHandshakeMessage[splitHandshakeMessage.Length - 1];
                if (trackingNumber.Substring(0, 3) != "SPC")
                {
                    trackingNumber = string.Empty;
                }
            }
            catch (Exception e)
            {
                StringBuilder body = new StringBuilder();
                body.AppendLine().Append("\n\nThe PushSPCToFITs service cannot run fn_Handshake using FITSDLL");
                body.AppendLine().Append("\n\nRefer to error message: " + e.Message);
                string subject = "[SW Alert]: PushSPCToFITs service cannot run fn_Handshake using FITSDLL";
                SendEmail.SendNotification(body.ToString(), subject);

                Log.Error($"Class-- >{this.GetType().Name} Method-->{System.Reflection.MethodBase.GetCurrentMethod().Name}   Error-->{e.Message}");
            }

            return trackingNumber;
        }

        /// <summary>
        /// Compose FITs requestParams using FITs object and pending SPC data of a specific test
        /// </summary>
        /// <param name="objFITs">FITs object</param>
        /// <param name="pendingData">pending SPC data of a specific test</param>
        /// <returns></returns>
        public FITsRequestParams GetSPCRequestParams(clsDB objFITs, ICollection<GetPendingData> pendingData)
        {
            FITsRequestParams requestParams = new FITsRequestParams();
            try
            {
                FITsResultParams resultParams = new FITsResultParams();

                GetPendingData gdf = pendingData.First();

                requestParams.operationType = 0;
                requestParams.revision = "";
                requestParams.fsp = ",";
                requestParams.employeeNo = gdf.Employee; // "000001"; //To be replaced by real 6 digital number recognized by FITs
                requestParams.shift = "";
                //Remove "_TE" characters from station name
                string stationName = DecryptStationName(gdf.Process);
                requestParams.machine = stationName;

                //Format test date into FITs standardard yyyy-MM-dd hh:mm:ss
                DateTime timeTest = Convert.ToDateTime(gdf.tmTest);
                string timestampStr = timeTest.ToString("yyyy-MM-dd hh:mm:ss");
                requestParams.timeTestFITs = DateTime.ParseExact(timestampStr, "yyyy-MM-dd hh:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

                foreach (GetPendingData gd in pendingData)
                {
                    switch (gdf.ChartName + ", " + gd.ParameterName)
                    {
                        case "element fac golden sample, BW":
                            resultParams.facBeamWidth = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element fac golden sample, Pointing":
                            resultParams.facPointing = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element fac dev from target, FA 1/e2 dev from target":
                            resultParams.facBeamWidth = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element fac dev from target, FA centroid dev from target":
                            resultParams.facPointing = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element sac golden sample, FA BW":
                            resultParams.facBeamWidth = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element sac golden sample, FA Pointing":
                            resultParams.facPointing = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element sac golden sample, SA BW":
                            resultParams.sacBeamWidth = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element sac golden sample, SAC Power":
                            resultParams.sacPower = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element sac, element SA 1/e2":
                            resultParams.sacBeamWidth = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element sac, element SA centroid":
                            resultParams.sacPointing = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element sac, element SAC Power":
                            resultParams.sacPower = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "se golden sample, GS Power":
                            resultParams.power = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "se golden sample, GS Wave centroid":
                            resultParams.wavelength = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element mirror golden sample, GS Power":
                            resultParams.power = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element mt golden sample, GS voltage":
                            resultParams.voltage = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element mt golden sample, GS Power":
                            resultParams.power = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element mt golden sample, GS Wave centroid":
                            resultParams.waveCentroid = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element mt golden sample, GS Snout temperature":
                            resultParams.snoutTemperature = Convert.ToDouble(gd.ParameterValue);
                            break;
                    }
                }
                switch (gdf.ChartName)
                {
                    case "element fac golden sample":
                        requestParams.labelParams = "Tracking number,Golden Sample SN,FAC Station#,FAC Beam Width,FAC Pointing";
                        requestParams.modelType = "SPC for Element";
                        requestParams.operation = "SPC11";
                        resultParams.goldenSampleSN = gdf.SerialNumber;
                        resultParams.facStationNumber = stationName;

                        requestParams.resultParams = resultParams.goldenSampleSN + "," + resultParams.facStationNumber + "," + Convert.ToString(resultParams.facBeamWidth) + "," + Convert.ToString(resultParams.facPointing);
                        break;
                    case "element fac dev from target":
                        requestParams.labelParams = "Tracking number,FBN W/O,Supercarrier SN,SC Part number,FAC Station#,FAC Beam Width,FAC Pointing";
                        requestParams.modelType = "SPC for Element";
                        requestParams.operation = "SPC12";
                        resultParams.fbnWO = (gdf.WorkOrder != null) ? gdf.WorkOrder.Trim() : "-";
                        resultParams.supercarrierSN = gdf.SerialNumber;
                        resultParams.scPartNumber = gdf.Part;
                        resultParams.facStationNumber = stationName;

                        requestParams.resultParams = resultParams.fbnWO + "," + resultParams.supercarrierSN + "," + resultParams.scPartNumber + "," + resultParams.facStationNumber + "," + Convert.ToString(resultParams.facBeamWidth) + "," + Convert.ToString(resultParams.facPointing);
                        break;
                    case "element sac golden sample":
                        requestParams.labelParams = "Tracking number,Golden Sample SN,SAC station#,FAC Beam Width,FAC Pointing,SAC Beam Width,SAC Power";
                        requestParams.modelType = "SPC for Element";
                        requestParams.operation = "SPC13";
                        resultParams.goldenSampleSN = gdf.SerialNumber;
                        resultParams.sacStationNumber = stationName;

                        requestParams.resultParams = resultParams.goldenSampleSN + "," + resultParams.sacStationNumber + "," + Convert.ToString(resultParams.facBeamWidth) + "," + Convert.ToString(resultParams.facPointing) + "," + Convert.ToString(resultParams.sacBeamWidth) + "," + Convert.ToString(resultParams.sacPower);
                        break;
                    case "element sac":
                        requestParams.labelParams = "Tracking number,FBN W/O,Supercarrier SN,SC Part number,SAC Station#,SAC Beam Width,SAC Pointing,SAC Power";
                        requestParams.modelType = "SPC for Element";
                        requestParams.operation = "SPC14";
                        resultParams.fbnWO = (gdf.WorkOrder != null) ? gdf.WorkOrder.Trim() : "-";
                        resultParams.supercarrierSN = gdf.SerialNumber;
                        resultParams.scPartNumber = gdf.Part;
                        resultParams.sacStationNumber = stationName;

                        requestParams.resultParams = resultParams.fbnWO + "," + resultParams.supercarrierSN + "," + resultParams.scPartNumber + "," + resultParams.sacStationNumber + "," + Convert.ToString(resultParams.sacBeamWidth) + "," + Convert.ToString(resultParams.sacPointing) + "," + Convert.ToString(resultParams.sacPower);
                        break;
                    case "element mirror golden sample":
                        requestParams.labelParams = "Tracking number,Golden Sample SN,Mirror Station#,Power";
                        requestParams.modelType = "SPC for Element";
                        requestParams.operation = "SPC16";
                        resultParams.goldenSampleSN = gdf.SerialNumber;
                        resultParams.mirrorStationNumber = stationName;

                        requestParams.resultParams = resultParams.goldenSampleSN + "," + resultParams.mirrorStationNumber + "," + Convert.ToString(resultParams.power);
                        break;
                    case "element mt golden sample":
                        requestParams.labelParams = "Tracking number,Golden Sample SN,MT Station#,Voltage,Power,Wave Centroid,Snout Temperature";
                        requestParams.modelType = "SPC for Element";
                        requestParams.operation = "SPC17";
                        resultParams.goldenSampleSN = gdf.SerialNumber;
                        resultParams.mtStationNumber = stationName;

                        requestParams.resultParams = resultParams.goldenSampleSN + "," + resultParams.mtStationNumber + "," + Convert.ToString(resultParams.voltage) + "," + Convert.ToString(resultParams.power) + "," + Convert.ToString(resultParams.waveCentroid) + "," + Convert.ToString(resultParams.snoutTemperature);
                        break;
                    case "se golden sample":
                        requestParams.labelParams = "Tracking number,Golden Sample SN,SE Station#,Power,Wavelength";

                        resultParams.goldenSampleSN = gdf.SerialNumber;
                        resultParams.seStationNumber = stationName;
                        if (gdf.PartGroup == "GS CS") //for PartGroup=GS CS
                        {
                            requestParams.modelType = "SPC for Element";
                            requestParams.operation = "SPC15";
                        }
                        else //for PartGroup=GS Pearl Chiplet
                        {
                            requestParams.modelType = "SPC for Pearl";
                            requestParams.operation = "SPCP04";
                        }
                        requestParams.resultParams = resultParams.goldenSampleSN + "," + resultParams.seStationNumber + "," + Convert.ToString(resultParams.power) + "," + Convert.ToString(resultParams.wavelength);
                        break;
                }

                resultParams.trackingNumber = GetTrackingNumber(objFITs, requestParams, resultParams);
                requestParams.resultParams = resultParams.trackingNumber + "," + requestParams.resultParams;
            }
            catch (Exception e)
            {
                StringBuilder body = new StringBuilder();
                body.AppendLine().Append("\n\nThe PushSPCToFITs service cannot run GetSPCRequestParams");
                body.AppendLine().Append("\n\nRefer to error message: " + e.Message);
                string subject = "[SW Alert]: PushSPCToFITs service cannot run GetSPCRequestParams";
                SendEmail.SendNotification(body.ToString(), subject);

                Log.Error($"Class-- >{this.GetType().Name} Method-->{System.Reflection.MethodBase.GetCurrentMethod().Name}   Error-->{e.Message}");
            }
            return requestParams;
        }

        public FITsRequestParams GetSPCRequestParamsForQuery(SPCHeader sh, ICollection<GetPendingData> pendingData)
        {
            FITsRequestParams requestParams = new FITsRequestParams();
            try
            {
                FITsResultParams resultParams = new FITsResultParams();

                GetPendingData gdf = pendingData.First();

                requestParams.revision = "";
                requestParams.serialNumber = sh.Tracking_number;
                requestParams.fsp = ",";

                //Remove "_TE" characters from station name
                string stationName = DecryptStationName(gdf.Process);
                requestParams.machine = stationName;

                foreach (GetPendingData gd in pendingData)
                {
                    switch (gdf.ChartName + ", " + gd.ParameterName)
                    {
                        case "element fac golden sample, BW":
                            resultParams.facBeamWidth = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element fac golden sample, Pointing":
                            resultParams.facPointing = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element fac dev from target, FA 1/e2 dev from target":
                            resultParams.facBeamWidth = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element fac dev from target, FA centroid dev from target":
                            resultParams.facPointing = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element sac golden sample, FA BW":
                            resultParams.facBeamWidth = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element sac golden sample, FA Pointing":
                            resultParams.facPointing = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element sac golden sample, SA BW":
                            resultParams.sacBeamWidth = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element sac golden sample, SAC Power":
                            resultParams.sacPower = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element sac, element SA 1/e2":
                            resultParams.sacBeamWidth = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element sac, element SA centroid":
                            resultParams.sacPointing = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element sac, element SAC Power":
                            resultParams.sacPower = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "se golden sample, GS Power":
                            resultParams.power = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "se golden sample, GS Wave centroid":
                            resultParams.wavelength = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element mirror golden sample, GS Power":
                            resultParams.power = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element mt golden sample, GS voltage":
                            resultParams.voltage = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element mt golden sample, GS Power":
                            resultParams.power = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element mt golden sample, GS Wave centroid":
                            resultParams.waveCentroid = Convert.ToDouble(gd.ParameterValue);
                            break;
                        case "element mt golden sample, GS Snout temperature":
                            resultParams.snoutTemperature = Convert.ToDouble(gd.ParameterValue);
                            break;
                    }
                }
                switch (gdf.ChartName)
                {
                    case "element fac golden sample":
                        requestParams.labelParams = "Tracking number,Golden Sample SN,FAC Station#,FAC Beam Width,FAC Pointing";
                        requestParams.modelType = "SPC for Element";
                        requestParams.operation = "SPC11";
                        resultParams.goldenSampleSN = (gdf.SerialNumber != null) ? gdf.SerialNumber : "-";
                        resultParams.facStationNumber = stationName;

                        requestParams.resultParams = resultParams.goldenSampleSN + "," + resultParams.facStationNumber + "," + Convert.ToString(resultParams.facBeamWidth) + "," + Convert.ToString(resultParams.facPointing);
                        break;
                    case "element fac dev from target":
                        requestParams.labelParams = "Tracking number,FBN W/O,Supercarrier SN,SC Part number,FAC Station#,FAC Beam Width,FAC Pointing";
                        requestParams.modelType = "SPC for Element";
                        requestParams.operation = "SPC12";
                        resultParams.fbnWO = (gdf.WorkOrder != null) ? gdf.WorkOrder.Trim() : "-";
                        resultParams.supercarrierSN = gdf.SerialNumber;
                        resultParams.scPartNumber = gdf.Part;
                        resultParams.facStationNumber = stationName;

                        requestParams.resultParams = resultParams.fbnWO + "," + resultParams.supercarrierSN + "," + resultParams.scPartNumber + "," + resultParams.facStationNumber + "," + Convert.ToString(resultParams.facBeamWidth) + "," + Convert.ToString(resultParams.facPointing);
                        break;
                    case "element sac golden sample":
                        requestParams.labelParams = "Tracking number,Golden Sample SN,SAC station#,FAC Beam Width,FAC Pointing,SAC Beam Width,SAC Power";
                        requestParams.modelType = "SPC for Element";
                        requestParams.operation = "SPC13";
                        resultParams.goldenSampleSN = gdf.SerialNumber;
                        resultParams.sacStationNumber = stationName;

                        requestParams.resultParams = resultParams.goldenSampleSN + "," + resultParams.sacStationNumber + "," + Convert.ToString(resultParams.facBeamWidth) + "," + Convert.ToString(resultParams.facPointing) + "," + Convert.ToString(resultParams.sacBeamWidth) + "," + Convert.ToString(resultParams.sacPower);
                        break;
                    case "element sac":
                        requestParams.labelParams = "Tracking number,FBN W/O,Supercarrier SN,SC Part number,SAC Station#,SAC Beam Width,SAC Pointing,SAC Power";
                        requestParams.modelType = "SPC for Element";
                        requestParams.operation = "SPC14";
                        resultParams.fbnWO = (gdf.WorkOrder != null) ? gdf.WorkOrder.Trim() : "-";
                        resultParams.supercarrierSN = gdf.SerialNumber;
                        resultParams.scPartNumber = gdf.Part;
                        resultParams.sacStationNumber = stationName;

                        requestParams.resultParams = resultParams.fbnWO + "," + resultParams.supercarrierSN + "," + resultParams.scPartNumber + "," + resultParams.sacStationNumber + "," + Convert.ToString(resultParams.sacBeamWidth) + "," + Convert.ToString(resultParams.sacPointing) + "," + Convert.ToString(resultParams.sacPower);
                        break;
                    case "element mirror golden sample":
                        requestParams.labelParams = "Tracking number,Golden Sample SN,Mirror Station#,Power";
                        requestParams.modelType = "SPC for Element";
                        requestParams.operation = "SPC16";
                        resultParams.goldenSampleSN = gdf.SerialNumber;
                        resultParams.mirrorStationNumber = stationName;

                        requestParams.resultParams = resultParams.goldenSampleSN + "," + resultParams.mirrorStationNumber + "," + Convert.ToString(resultParams.power);
                        break;
                    case "element mt golden sample":
                        requestParams.labelParams = "Tracking number,Golden Sample SN,MT Station#,Voltage,Power,Wave Centroid,Snout Temperature";
                        requestParams.modelType = "SPC for Element";
                        requestParams.operation = "SPC17";
                        resultParams.goldenSampleSN = gdf.SerialNumber;
                        resultParams.mtStationNumber = stationName;

                        requestParams.resultParams = resultParams.goldenSampleSN + "," + resultParams.mtStationNumber + "," + Convert.ToString(resultParams.voltage) + "," + Convert.ToString(resultParams.power) + "," + Convert.ToString(resultParams.waveCentroid) + "," + Convert.ToString(resultParams.snoutTemperature);
                        break;
                    case "se golden sample":
                        requestParams.labelParams = "Tracking number,Golden Sample SN,SE Station#,Power,Wavelength";

                        resultParams.goldenSampleSN = gdf.SerialNumber;
                        resultParams.seStationNumber = stationName;
                        if (gdf.PartGroup == "GS CS") //for PartGroup=GS CS
                        {
                            requestParams.modelType = "SPC for Element";
                            requestParams.operation = "SPC15";
                        }
                        else //for PartGroup=GS Pearl Chiplet
                        {
                            requestParams.modelType = "SPC for Pearl";
                            requestParams.operation = "SPCP04";
                        }
                        requestParams.resultParams = resultParams.goldenSampleSN + "," + resultParams.seStationNumber + "," + Convert.ToString(resultParams.power) + "," + Convert.ToString(resultParams.wavelength);
                        break;
                }

                requestParams.resultParams = sh.Tracking_number + "," + requestParams.resultParams;
            }
            catch (Exception e)
            {
                StringBuilder body = new StringBuilder();
                body.AppendLine().Append("\n\nThe PushSPCToFITs service cannot run GetSPCRequestParamsForQuery");
                body.AppendLine().Append("\n\nRefer to error message: " + e.Message);
                string subject = "[SW Alert]: PushSPCToFITs service cannot run GetSPCRequestParams";
                SendEmail.SendNotification(body.ToString(), subject);

                Log.Error($"Class-- >{this.GetType().Name} Method-->{System.Reflection.MethodBase.GetCurrentMethod().Name}   Error-->{e.Message}");
            }
            return requestParams;
        }

        /// <summary>
        /// Insert the SPC data into FITs
        /// </summary>
        /// <param name="objFITs">FITs object</param>
        /// <param name="requestParams">Input requstParams to be pushed into FITs</param>
        /// <returns></returns>
        public bool InsertSPCToFITs(clsDB objFITs, FITsRequestParams requestParams)
        {
            bool result = false;
            try
            {
                Log.Information($"labelParams: {requestParams.labelParams}");
                Log.Information($"labelResults: {requestParams.resultParams}");
                ServiceResult objResult = objFITs.fn_Insert(requestParams.operationType, requestParams.modelType, requestParams.operation, requestParams.labelParams, requestParams.resultParams, requestParams.fsp, requestParams.employeeNo, requestParams.shift, requestParams.machine, requestParams.timeTestFITs, requestParams.revision);
                result = objResult.outputValue;
                Log.Information($"The FITs fn_Insert result is {objResult.result}, messge is {objResult.message}, outputValue is {objResult.outputValue.ToString()} ");

            }
            catch (Exception e)
            {
                StringBuilder body = new StringBuilder();
                body.AppendLine().Append("\n\nThe PushSPCToFITs service cannot run fn_Insert using FITSDLL");
                body.AppendLine().Append("\n\nRefer to error message: " + e.Message);
                string subject = "[SW Alert]: PushSPCToFITs service cannot run fn_Insert using FITSDLL";
                SendEmail.SendNotification(body.ToString(), subject);

                Log.Error($"Class-- >{this.GetType().Name} Method-->{System.Reflection.MethodBase.GetCurrentMethod().Name}   Error-->{e.Message}");
            }
            return result;
        }

        /// <summary>
        /// Remove "_TE" characters from station name as business does not needed it, Rebecca Jin confirmed it
        /// </summary>
        /// <param name="spcStationName"></param>
        /// <returns></returns>
        public string DecryptStationName(string spcStationName)
        {
            string stationName = spcStationName;

            if (spcStationName.Substring(spcStationName.Length - 3) == "_TE")
            {
                stationName = spcStationName.Substring(0, spcStationName.Length - 3);
            }

            return stationName;
        }

        /// <summary>
        /// Check if the SPC data exists in FITs already
        /// </summary>
        /// <param name="objFITs">FITs object</param>
        /// <param name="sh">SPCHeader data for 1 SPCHeaderID</param>
        /// <param name="pendingData">SPC detail data for 1 SPCHeaderID</param>
        /// <returns>true: the SPC data already exist in FITs; false: the SPC data does not exist in FITs</returns>
        public bool FITsDataExists(clsDB objFITs, SPCHeader sh, ICollection<GetPendingData> pendingData)
        {
            bool result = false;
            try
            {
                if (sh.FITsNeed_flag != false && sh.Tracking_number != null && sh.ProcessedSuccessToFITs_flag == false)
                {
                    FITsRequestParams requestParams = GetSPCRequestParamsForQuery(sh, pendingData);
                    Log.Information($"Query labelParams: {requestParams.labelParams}");
                    ServiceResultQuery objResult = objFITs.fn_Query(requestParams.modelType, requestParams.operation, requestParams.revision, requestParams.serialNumber, requestParams.labelParams, requestParams.fsp);
                    if (objResult.outputValue != requestParams.resultParams)
                    {
                        Log.Information($"Query outputValue: {objResult.outputValue}");
                        Log.Information($"Existed composed resultParams: {requestParams.resultParams}");
                        result = false;
                    }
                    else
                    {
                        Log.Information($"Query outputValue: {objResult.outputValue}");
                        Log.Information($"Existed composed resultParams: {requestParams.resultParams}");
                        result = true;
                    }
                }
            }
            catch (Exception e)
            {
                StringBuilder body = new StringBuilder();
                body.AppendLine().Append("\n\nThe PushSPCToFITs service cannot run fn_Query using FITSDLL");
                body.AppendLine().Append("\n\nRefer to error message: " + e.Message);
                string subject = "[SW Alert]: PushSPCToFITs service cannot run fn_Query using FITSDLL";
                SendEmail.SendNotification(body.ToString(), subject);

                Log.Error($"Class-- >{this.GetType().Name} Method-->{System.Reflection.MethodBase.GetCurrentMethod().Name}   Error-->{e.Message}");
            }

            return result;

        }


    }
}
