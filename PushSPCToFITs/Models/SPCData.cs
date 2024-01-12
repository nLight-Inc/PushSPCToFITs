using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PushSPCToFITs.Models
{
    [Table("SPCData", Schema = "dbo")]
    public class SPCData
    {
        [Key]
        public int ID { get; set; }
        
        public int SPCHeaderID { get; set; }
        public int SPCChartID { get; set; }
        public string ParameterName { get; set; }
        public double? ParameterValue { get; set; }
        public bool? ProcessedSuccessToFITs_flag { get; set; }
        public DateTime? ProcessToFITs_date { get; set; }
        public string? ProcessToFITs_user { get; set; }
        public double? Process_time { get; set; }

    }
}
