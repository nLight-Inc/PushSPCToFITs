using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PushSPCToFITs.Helpers
{
    /// <summary>
    ///   Static class for queries
    /// </summary>
    public static class TableHelpers
    {
        /// <summary>
        ///   Retrieves component serial number
        /// </summary>
        /// <param name="serialNumber"></param>
        /// <param name="verticalPosition">There are 3 values to this parameter being "Bottom", "Middle", and "Top"</param>
        /// <returns></returns>
        public static string GetComponentSerialNumberForElementSupercarrier(string serialNumber, string verticalPosition)
        {
            string neqConnString = Common.GetNeqServerConnString();

            using (NeqdbContext context = new NeqdbContext(neqConnString))
            {
                string componentSerialNum = context.ElementSupercarrier
                    .Where(s => s.SerialNumber == serialNumber
                    && s.VerticalPosition == verticalPosition)
                    .Select(s => s.ComponentSN)
                    .FirstOrDefault();

                return componentSerialNum;
            }
        }

        /// <summary>
        /// Retrieve an item Number for a given serial number in LD.SPC_TEGoldStandards
        /// </summary>
        /// <param name="serialNumber"> The serial number to search LD.SPC_TEGoldStandards </param>
        /// <returns></returns>
        public static string GetGoldStandardItemNumber(string serialNumber)
        {
            string neqConnString = Common.GetNeqServerConnString();

            using (NeqdbContext context = new NeqdbContext(neqConnString))
            {
                return context.SPC_TEGoldStandards.Where(x => x.SerialNumber == serialNumber).Select(x => x.ItemID).FirstOrDefault();
            }
        }

        /// <summary>
        /// Returns a list of all COS's that are currently used for the requested package serial number
        /// </summary>
        /// <param name="packageSN">Package Serial Number</param>
        /// <returns>A List of </returns>
        public static List<ElementFullComponentInfo> GetAllCosForModuleSerialNumber(string packageSN)
        {
            string neqConnString = Common.GetNeqServerConnString();

            using (NeqdbContext context = new NeqdbContext(neqConnString))
            {
                List<ElementFullComponentInfo> moduleCOSs = context.ElementFullComponentInfo
                    .Where(x => x.PackageSN == packageSN)
                    .ToList();

                return moduleCOSs;
            }
        }

        /// <summary>
        ///   Returns an array of strings.  
        /// </summary>
        /// <param name="serialNumber"></param>
        /// <returns></returns>
        public static string[] GroundBondVerification(string serialNumber)
        {
            try
            {
                string neqConnString = Common.GetNeqServerConnString();

                Log.Verbose($"TableHelpers   GroundBondVerification   Connection String-->{neqConnString}");

                using (NeqdbContext context = new NeqdbContext(neqConnString))
                {
                    int latestTestNumber = 0;
                    try
                    {
                        latestTestNumber = (int)context.Tier2Data
                            .Where(w => w.SerialNumber == serialNumber)
                            .Max(m => m.TestNumber);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"TableHelpers    GroundBondVerification   Error:  Latest Test Number not Found");

                        string[] error = new string[1];
                        error[0] = "Latest Test Number not Found";

                        return error;
                    }

                    ICollection<Tier2SubData> t2SubData = context.Tier2SubData
                        .Where(w => w.SerialNumber == serialNumber && w.TestNumber == latestTestNumber)
                        .ToList();

                    ICollection<Tier3SubData> t3SubData = context.Tier3SubData
                        .Where(w => w.SerialNumber == serialNumber && w.TestNumber == latestTestNumber)
                        .ToList();

                    ICollection<Tier4SubData> t4SubData = context.Tier4SubData
                        .Where(w => w.SerialNumber == serialNumber && w.TestNumber == latestTestNumber)
                        .ToList();

                    ICollection<string> results = new List<string>();

                    string dateOfTest = t3SubData.Where(w => w.Enum == 48).Select(s => s.Value).FirstOrDefault();
                    dateOfTest = dateOfTest == null ? string.Empty : dateOfTest;
                    results.Add(dateOfTest);       //  Date of test (from Tier3, BPP controls Tier 2 start date)

                    string stationNumber = t2SubData.Where(w => w.Enum == 61).Select(s => s.Value).FirstOrDefault();
                    stationNumber = stationNumber == null ? string.Empty : stationNumber;
                    results.Add(stationNumber);      //   Station number

                    string serNum = t3SubData.Where(w => w.Enum == 19).Select(s => s.Value).FirstOrDefault();
                    serNum = serNum == null ? string.Empty : serNum;
                    results.Add(serNum);      //   Source serial number

                    var testOutcome = t4SubData.Where(w => w.Enum == 798).Select(s => s.Value).ToList();        //  Test outcome

                    foreach (string to in testOutcome)
                    {
                        results.Add(to);
                    }

                    string[] resultsArray = results.ToArray();

                    return resultsArray;
                }
            }
            catch (Exception e)
            {
                Log.Error($"TableHelpers    GroundBondVerification   Error:  {e.Message}");
                if (e.InnerException != null)
                {
                    Log.Error($"\t\t\tInner exception {e.InnerException.Message}");
                }

                string[] error = new string[1];
                error[0] = e.Message;

                return error;
            }
        }
    }
}
