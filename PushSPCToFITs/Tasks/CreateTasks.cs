using Serilog;
using System.Threading.Tasks;

namespace PushSPCToFITs.Tasks
{
    public class CreateTasks
    {
        public Task[] StartTasks()
        {
            Task[] tasks = new Task[1];
            ProductionTask myTask = ProductionTask.Instance;
            Task tSPC = Task.Run(() =>
            {
                Log.Information("{0} Starting task", this.GetType().Name);
                myTask.RunTask();
            });
            tasks[0] = tSPC;

            return tasks;
        }
    }
}
