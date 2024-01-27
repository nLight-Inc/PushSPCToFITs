﻿using System;
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
    public class RunFITsDLL
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

        public string GetTrackingNumber(clsDB objFITs, FITsRequestParams requestParams, FITsResultParams resultParams )
        {
            string serialNumber =  string.Empty;
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

        public FITsRequestParams GetSPCRequestParams (clsDB objFITs, ICollection<GetPendingData> pendingData)
        {
            FITsRequestParams requestParams = new FITsRequestParams();
            try
            {                
                FITsResultParams resultParams = new FITsResultParams();

                GetPendingData gdf = pendingData.First();

                requestParams.operationType = 0;
                requestParams.revision = "";
                requestParams.fsp = ",";
                requestParams.employeeNo = "000001"; //To be replaced by real 6 digital number recognized by FITs
                requestParams.shift = "";
                requestParams.machine = gdf.Process;
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
                        resultParams.facStationNumber = gdf.Process;

                        requestParams.resultParams = resultParams.goldenSampleSN + "," + resultParams.facStationNumber + "," + Convert.ToString(resultParams.facBeamWidth) + "," + Convert.ToString(resultParams.facPointing);
                        break;
                    case "element fac dev from target":
                        requestParams.labelParams = "Tracking number,FBN W/O,Supercarrier SN,SC Part number,FAC Station#,FAC Beam Width,FAC Pointing";
                        requestParams.modelType = "SPC for Element";
                        requestParams.operation = "SPC12";
                        resultParams.fbnWO = gdf.WorkOrder;
                        resultParams.supercarrierSN = gdf.SerialNumber;
                        resultParams.scPartNumber = gdf.Part;
                        resultParams.facStationNumber = gdf.Process;

                        requestParams.resultParams = resultParams.fbnWO + "," + resultParams.supercarrierSN + "," + resultParams.scPartNumber + Convert.ToString(resultParams.facBeamWidth) + "," + Convert.ToString(resultParams.facPointing);
                        break;
                    case "element sac golden sample":
                        requestParams.labelParams = "Tracking number,Golden Sample SN,SAC station#,FAC Beam Width,FAC Pointing,SAC Beam Width,SAC Power";
                        requestParams.modelType = "SPC for Element";
                        requestParams.operation = "SPC13";
                        resultParams.goldenSampleSN = gdf.SerialNumber;
                        resultParams.sacStationNumber = gdf.Process;

                        requestParams.resultParams = resultParams.goldenSampleSN + "," + resultParams.sacStationNumber + "," + Convert.ToString(resultParams.facBeamWidth) + Convert.ToString(resultParams.facPointing) + "," + Convert.ToString(resultParams.sacBeamWidth) + "," + Convert.ToString(resultParams.sacPower);
                        break;
                    case "element sac":
                        requestParams.labelParams = "Tracking number,FBN W/O,Supercarrier SN,SC Part number,SAC Station#,SAC Beam Width,SAC Pointing,SAC Power";
                        requestParams.modelType = "SPC for Element";
                        requestParams.operation = "SPC14";
                        resultParams.supercarrierSN = gdf.SerialNumber;
                        resultParams.scPartNumber = gdf.Part;
                        resultParams.sacStationNumber = gdf.Process;

                        requestParams.resultParams = resultParams.fbnWO + "," + resultParams.supercarrierSN + "," + resultParams.scPartNumber + Convert.ToString(resultParams.sacBeamWidth) + "," + Convert.ToString(resultParams.sacPointing) + "," + Convert.ToString(resultParams.sacPower);
                        break;
                    case "element mirror golden sample":
                        requestParams.labelParams = "Tracking number,Golden Sample SN,Mirror Station#,Power";
                        requestParams.modelType = "SPC for Element";
                        requestParams.operation = "SPC16";
                        resultParams.goldenSampleSN = gdf.SerialNumber;
                        resultParams.mirrorStationNumber = gdf.Process;

                        requestParams.resultParams = resultParams.goldenSampleSN + "," + resultParams.mirrorStationNumber + "," + Convert.ToString(resultParams.power);
                        break;
                    case "element mt golden sample":
                        requestParams.labelParams = "Tracking number,Golden Sample SN,MT Station#,Voltage,Power,Wave Centroid,Snout Temperature";
                        requestParams.modelType = "SPC for Element";
                        requestParams.operation = "SPC17";
                        resultParams.goldenSampleSN = gdf.SerialNumber;
                        resultParams.mtStationNumber = gdf.Process;

                        requestParams.resultParams = resultParams.goldenSampleSN + "," + resultParams.mtStationNumber + "," + Convert.ToString(resultParams.voltage) + "," + Convert.ToString(resultParams.power) + "," + Convert.ToString(resultParams.waveCentroid) + "," + Convert.ToString(resultParams.snoutTemperature);
                        break;
                    case "se golden sample":
                        requestParams.labelParams = "Tracking number,Golden Sample SN,SE Station#,Power,Wavelength";

                        resultParams.goldenSampleSN = gdf.SerialNumber;
                        resultParams.seStationNumber = gdf.Process;
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

        public bool InsertSPCToFITs (clsDB objFITs, FITsRequestParams requestParams)
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
    }
}
