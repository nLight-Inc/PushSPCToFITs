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
    public class RunFITsDLL
    {
        public clsDB _objFITs;

        public RunFITsDLL()
        {
            _objFITs = GetFITsConnection();
        }

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
                body.AppendLine().Append("\n\nThe PushSPCToFITs service cannot logon FITSDLL");
                body.AppendLine().Append("\n\nRefer to error message: " + e.Message);
                string subject = "[SW Alert]: PushSPCToFITs service cannot logon FITSDLL";
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
                body.AppendLine().Append("\n\nThe PushSPCToFITs service cannot fn_Handshake FITSDLL");
                body.AppendLine().Append("\n\nRefer to error message: " + e.Message);
                string subject = "[SW Alert]: PushSPCToFITs service cannot fn_Handshake FITSDLL";
                SendEmail.SendNotification(body.ToString(), subject);

                Log.Error($"Class-- >{this.GetType().Name} Method-->{System.Reflection.MethodBase.GetCurrentMethod().Name}   Error-->{e.Message}");
            }

            return trackingNumber;
        }

        public FITsRequestParams GetRequestParams (ICollection<GetPendingData> pendingData, string trackingNumber)
        {
            FITsRequestParams requestParams  = new FITsRequestParams();

            GetPendingData gdf = pendingData.First();

            string modelType = string.Empty;
            string operation = string.Empty;
            string serialNumber = gdf.SerialNumber;
            string labelParams = string.Empty;
            string resultParams = string.Empty;
            string revision = "";
            string fsp = ",";
            string employeeNo = "000001"; //To be replaced by real 6 digital number recognized by FITs
            string shift = "";
            string machine = gdf.Process;
            DateTime timeTest = Convert.ToDateTime(gdf.tmTest);
            string timestampStr = timeTest.ToString("yyyy-MM-dd hh:mm:ss");
            DateTime timeTestFITs = DateTime.ParseExact(timestampStr, "yyyy-MM-dd hh:mm:ss", System.Globalization.CultureInfo.InvariantCulture);



            //For SPC11: element fac golden sample
            string goldenSampleSN = gdf.SerialNumber;
            string facStationNo = gdf.Process;
            double facBeamWidth;
            double facPointing;

            //For SPC12: element fac dev from target
            string FBNWO = gdf.WorkOrder;
            string supercarrierSN = gdf.SerialNumber;
            string scPartNumber = gdf.Part;

            //For SPC13: element sac golden sample
            string sacStationNo = gdf.Process;
            double sacBeamWidth;
            double sacPower;

            //For SPC14: element sac
            double sacPointing;

            //For SPC15: se golden sample for COS
            string seStationNo = gdf.Process;
            double power;
            double wavelength;

            //For SPC16: element mirror golden sample
            string mirrorStationNo = gdf.Process;

            //For SPC17: element mt golden sample
            string mtStationNo = gdf.Process;
            double voltage;
            double waveCentroid;
            double snoutTemperature;

            //For SPC04: se golden sample for Chiplet

            foreach (GetPendingData gd in pendingData)
            {
                switch (gdf.ChartName + ", " + gd.ParameterName)
                {
                    case "element fac golden sample, BW":
                        facBeamWidth = Convert.ToDouble(gd.ParameterValue);
                        break;
                    case "element fac golden sample, Pointing":
                        facPointing = Convert.ToDouble(gd.ParameterValue);
                        break;
                    case "element fac dev from target, FA 1/e2 dev from target":
                        facBeamWidth = Convert.ToDouble(gd.ParameterValue);
                        break;
                    case "element fac dev from target, FA centroid dev from target":
                        facPointing = Convert.ToDouble(gd.ParameterValue);
                        break;
                    case "element sac golden sample, FA BW":
                        facBeamWidth = Convert.ToDouble(gd.ParameterValue);
                        break;
                    case "element sac golden sample, FA Pointing":
                        facPointing = Convert.ToDouble(gd.ParameterValue);
                        break;
                    case "element sac golden sample, SA BW":
                        sacBeamWidth = Convert.ToDouble(gd.ParameterValue);
                        break;
                    case "element sac golden sample, SAC Power":
                        sacPower = Convert.ToDouble(gd.ParameterValue);
                        break;
                    case "element sac, element SA 1/e2":
                        sacBeamWidth = Convert.ToDouble(gd.ParameterValue);
                        break;
                    case "element sac, element SA centroid":
                        sacPointing = Convert.ToDouble(gd.ParameterValue);
                        break;
                    case "element sac, element SAC Power":
                        sacPower = Convert.ToDouble(gd.ParameterValue);
                        break;
                    case "se golden sample, GS Power":
                        power = Convert.ToDouble(gd.ParameterValue);
                        break;
                    case "se golden sample, GS Wave centroid":
                        wavelength = Convert.ToDouble(gd.ParameterValue);
                        break;
                    case "element mirror golden sample, GS Power":
                        power = Convert.ToDouble(gd.ParameterValue);
                        break;
                    case "element mt golden sample, GS voltage":
                        voltage = Convert.ToDouble(gd.ParameterValue);
                        break;
                    case "element mt golden sample, GS Power":
                        power = Convert.ToDouble(gd.ParameterValue);
                        break;
                    case "element mt golden sample, GS Wave centroid":
                        waveCentroid = Convert.ToDouble(gd.ParameterValue);
                        break;
                    case "element mt golden sample, GS Snout temperature":
                        snoutTemperature = Convert.ToDouble(gd.ParameterValue);
                        break;

                }
            }
            switch (gdf.ChartName)
            {                
                case "element fac golden sample":
                    labelParams = "Tracking number,Golden Sample SN,FAC Station#,FAC Beam Width,FAC Pointing";
                    modelType = "SPC for Element";
                    operation = "SPC11";
                    break;
                case "element fac dev from target":
                    labelParams = "Tracking number,FBN W/O,Supercarrier SN,SC Part number,FAC Station#,FAC Beam Width,FAC Pointing";
                    modelType = "SPC for Element";
                    operation = "SPC12";
                    break;
                case "element sac golden sample":
                    labelParams = "Tracking number,Golden Sample SN,SAC station#,FAC Beam Width,FAC Pointing,SAC Beam Width,SAC Power";
                    modelType = "SPC for Element";
                    operation = "SPC13";
                    break;
                case "element sac":
                    labelParams = "Tracking number,FBN W/O,Supercarrier SN,SC Part number,SAC Station#,SAC Beam Width,SAC Pointing,SAC Power";
                    modelType = "SPC for Element";
                    operation = "SPC14";
                    break;
                case "element mirror golden sample":
                    labelParams = "Tracking number,Golden Sample SN,Mirror Station#,Power";
                    modelType = "SPC for Element";
                    operation = "SPC16";
                    break;
                case "element mt golden sample":
                    labelParams = "Tracking number,Golden Sample SN,MT Station#,Voltage,Power,Wave Centroid,Snout Temperature";
                    modelType = "SPC for Element";
                    operation = "SPC17";
                    break;                                
                case "se golden sample":
                    if (gdf.PartGroup == "GS CS") //for PartGroup=GS CS
                    {
                        labelParams = "Tracking number,Golden Sample SN,SE Station#,Power,Wavelength";
                        modelType = "SPC for Element";
                        operation = "SPC15";
                    }
                    else //for PartGroup=GS Pearl Chiplet
                    {
                        labelParams = "Tracking number,Golden Sample SN,SE Station#,Power,Wavelength";
                        modelType = "SPC for Pearl";
                        operation = "SPCP04";
                    }
                    break;
            }

            //resultParams = trackingNumber + "," + goldenSampleSN + "," + facStationNo + "," + 
            
                
            

            return requestParams;
        }

        public FITsResultParams GetResultParams(ICollection<GetPendingData> pendingData)
        {
            FITsResultParams resultParams = new FITsResultParams();
            return resultParams;
        }


        public bool InsertFITs (clsDB objFITs, ICollection<GetPendingData> pendingData, FITsRequestParams requestParams, FITsResultParams resultParams)
        {
            try
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
                        if (gdf.PartGroup == "GS CS") //for PartGroup=GS CS
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

                string labelResults2 = resultParams.trackingNumber + ",T59CHU,1,GS element FAC,FAC No.5,1.640064,272.04,Test,PASS,0";
                Log.Information($"labelParams: {labelParams}");
                Log.Information($"labelResults: {labelResults2}");
                ServiceResult objResult = objFITs.fn_Insert(operationType, modelType, operation, labelParams, labelResults2, fsp, employeeNo, shift, machine, timeTestFITs, revision);
                Log.Information($"The FITs fn_Insert with Tracking number result is {objResult.result}, messge is {objResult.message}, outputValue is {objResult.outputValue.ToString()} ");
            }
            catch (Exception e)
            {
                StringBuilder body = new StringBuilder();
                body.AppendLine().Append("\n\nThe PushSPCToFITs service cannot logon FITSDLL");
                body.AppendLine().Append("\n\nRefer to error message: " + e.Message);
                string subject = "[SW Alert]: PushSPCToFITs service cannot logon FITSDLL";
                SendEmail.SendNotification(body.ToString(), subject);

                Log.Error($"Class-- >{this.GetType().Name} Method-->{System.Reflection.MethodBase.GetCurrentMethod().Name}   Error-->{e.Message}");
            }
            return true;
        }
    }
}
