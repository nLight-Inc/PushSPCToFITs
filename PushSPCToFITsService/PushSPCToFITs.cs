using PushSPCToFITs.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using System.Reflection;
using PushSPCToFITs.Tasks;

namespace PushSPCToFITsService
{
    public partial class PushSPCToFITs : ServiceBase
    {
        private Task[] _tasks;
        private string _envLoc;

        public PushSPCToFITs()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                StartLogging.StartLogger();

                CreateTasks createTasks = new CreateTasks();
                Log.Information("{0}: {1} is starting.", ServiceName, Assembly.GetEntryAssembly().GetName().Version.ToString());
                _tasks = createTasks.StartTasks();


            }
            catch (Exception e)
            {
                Log.Error("A critical error occurred during startup of the service, see exception for details. {0}", e.Message.ToString());
            }
        }

        protected override void OnStop()
        {
            Log.Information("OnStop Method {0}", this.GetType().Name);

            try
            {
                ProductionTask productionTask = ProductionTask.Instance;
                productionTask._stopService = true;

                //  if a thread has not finished within 3 minutes, let it die a painful death  --  Note, AX will time out after 1 minute, this gives us time to update the transactions external file with failure data, and end

                TimeSpan maxWaitTime = TimeSpan.FromMinutes(Common.ReadAppSetting("KillAllTasksWaitTime", 3));
                Task.WaitAll(_tasks.ToArray(), maxWaitTime);

                Log.Information("All tasks have been stopped");
            }
            catch (Exception e)
            {
                Log.Error("{0} exception message during OnStop: {1}", this.GetType().Name, e.Message.ToString());
            }
        }
    }
}
