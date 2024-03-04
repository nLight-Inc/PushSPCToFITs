using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PushSPCToFITs.Models
{
    public class GetPendingData
    {
        public int ID { get; set; }
        public string ChartName { get; set; }
        public int TIER2ID { get; set; }
        public string Part { get; set; }
        public string PartGroup { get; set; }
        public string Process { get; set; }
        public string? ProcessGroup { get; set; }
        public int Size { get; set; }
        public string Employee { get; set; }
        public string SerialNumber { get; set; }
        public DateTime? tmTest { get; set; }
        public int? TestNumber { get; set; }
        public string? TestStation { get; set; }
        public string? WorkOrder { get; set; }
        public string? TestResult { get; set; }
        public string? TestOperator { get; set; }
        public string ParameterName { get; set; }
        public double? ParameterValue { get; set; }
        public DateTime Created_date { get; set; }
        public DateTime? ProcessToFITs_date { get; set; }
        public bool? ProcessedSuccessToFITs_flag { get; set; }
        public double? Process_time { get; set; }

        public string? ProcessToFITs_user { get; set; }

    }
}
