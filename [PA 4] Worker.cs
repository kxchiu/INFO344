using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Management;
using Share;
using Microsoft.VisualBasic.Devices;

namespace Share
{
    public class WorkerMonitor
    {
        public static WorkerMonitor instance = null;

        private CloudTable cloudTable;
        private PerformanceCounter cpuCounter;
        private PerformanceCounter ramCounter;
        private int roleInstanceId;
        private double totalMemory;

        public static WorkerMonitor GetInstance()
        {
            if (instance == null)
            {
                instance = new WorkerMonitor(Azure.GetInstance().getTableReference(ConfigurationManager.AppSettings["workerInfo"]));
            }
            return instance;
        }

        public WorkerMonitor(CloudTable cloudTable)
        {
            this.cloudTable = cloudTable;

            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "Available Bytes");
            cpuCounter.NextValue();
            ramCounter.NextValue();

            totalMemory = (new ComputerInfo()).TotalPhysicalMemory;

            string instanceId = RoleEnvironment.CurrentRoleInstance.Id;
            if (int.TryParse(instanceId.Substring(instanceId.LastIndexOf(".") + 1), out roleInstanceId)) // On cloud.
            {
                int.TryParse(instanceId.Substring(instanceId.LastIndexOf("_") + 1), out roleInstanceId); // On compute emulator.
            }
        }

        public List<WorkerInfo> getProfiles()
        {
            TableQuery<WorkerInfo> query = (new TableQuery<WorkerInfo>()).Where(
                TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.GreaterThan, DateTime.Now.AddMinutes(-3)
            ));

            List<WorkerInfo> workers = new List<WorkerInfo>(); ;
            foreach (WorkerInfo entity in cloudTable.ExecuteQuery(query))
            {
                workers.Add(entity);
            }
            return workers;
        }

        public void updateProfile(WorkerInfo.State state)
        {
            double ramPercent = (totalMemory - ramCounter.NextValue()) * 100 / totalMemory;
            WorkerInfo info = new WorkerInfo(roleInstanceId, state, cpuCounter.NextValue(), ramPercent);
            cloudTable.Execute(TableOperation.InsertOrReplace(info));
        }
    }
}
