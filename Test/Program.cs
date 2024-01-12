using PushSPCToFITs.Helpers;
using PushSPCToFITs.Models;
using PushSPCToFITs.Tasks;
using System;
using System.Collections.Generic;
using System.IO;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            StartLogging.StartLogger();
            Test_LoadSPCData();
        }

        static void Test_LoadSPCData()
        {
            Console.WriteLine("\nSPC Data Loading Starts:");

            ProductionTask SPCTask = ProductionTask.Instance;
            //SPCTask.FITsConnection();
            SPCTask.RunTask();
        }
    }
}
