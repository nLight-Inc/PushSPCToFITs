using PushSPCToFITs.Helpers;
using PushSPCToFITs.Models;
using PushSPCToFITs.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using FITSDLL;
using System.Text;


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

            BaseTask myTask = new BaseTask();
            //myTask.TestFITs();
            myTask.RunTask();
            //Test_SendEmail();
        }

        static void Test_SendEmail()
        {

            StringBuilder body = new StringBuilder();
            body.AppendLine().Append("\n\nTest");            
            string subject = "[SW Alert]: This is test mail, please ignore it";
            SendEmail.SendNotification(body.ToString(), subject);
        }
    }
}
