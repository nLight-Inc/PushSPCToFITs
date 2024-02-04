using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushSPCToFITs.Models
{
    public class FITsRequestParams
    {
        public string labelParams { get; set; }
        public string resultParams { get; set; }
        public int operationType { get; set; }
        public string modelType { get; set; }
        public string operation { get; set; }
        public string fsp { get; set; }
        public string employeeNo { get; set; }
        public string shift { get; set; }
        public string machine { get; set; }
        public DateTime timeTestFITs { get; set; }

        public string revision { get; set; }

        public string serialNumber { get; set; }

    }
}
