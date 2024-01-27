using PushSPCToFITs.Helpers;
using PushSPCToFITs.Models;
using PushSPCToFITs.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using FITSDLL;


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
            //SPCTask.TestFITs();
            SPCTask.RunTask();
        }
    }
}
