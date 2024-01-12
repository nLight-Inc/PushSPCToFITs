using Microsoft.EntityFrameworkCore;
using PushSPCToFITs.Context;
using PushSPCToFITs.Helpers;
using PushSPCToFITs.Models;
using Serilog;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

// https://www.codeproject.com/Articles/572263/A-Reusable-Base-Class-for-the-Singleton-Pattern-in


namespace PushSPCToFITs.Tasks
{
    public class ProductionTask : BaseTask<ProductionTask>
    {

    }
}
