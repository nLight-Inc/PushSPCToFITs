using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PushSPCToFITs.Models
{
    public class FITsResultParams
    {
        public string trackingNumber { get; set; }
        public string cosPartNumber { get; set; }
        public string goldenSampleSN { get; set; }
        public string goldenCountTime { get; set; }
        public string scPartNumber { get; set; }
        public string facStationNumber { get; set; }
        public double facBeamWidth { get; set; }
        public double facPointing { get; set; }
        public string comment { get; set; }
        public string result { get; set; }
        public string failureCode { get; set; }
        public string fbnWO { get; set; }
        public string supercarrierSN { get; set; }
        public string sacStationNumber { get; set; }
        public double sacPointing { get; set; }
        public double sacBeamWidth { get; set; }
        public double sacPower { get; set; }
        public string seStationNumber { get; set; }
        public double power { get; set; }
        public string modulePartNumber { get; set; }
        public string mtStationNumber { get; set; }
        public string mirrorStationNumber { get; set; }
        public double voltage { get; set; }
        public double waveCentroid { get; set; }
        public double snoutTemperature { get; set; }
        public string chipletPartNumber { get; set; }
        public double wavelength { get; set; }
    }
}
