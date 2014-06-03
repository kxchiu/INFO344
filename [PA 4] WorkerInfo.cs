using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;

namespace Share
{
    public class WorkerInfo : TableEntity
    {
        public enum State
        {
            Loading,
            Crawling,
            Idle,
            Stopped,
            Stopping,
            Resetting,
            Reset
        };

        public string state { set; get; }
        public int roleInstanceId { set; get; }
        public double cpuUsage { set; get; }
        public double memoryUsage { set; get; }

        public WorkerInfo() { }

        public WorkerInfo(int roleInstanceId, State state, double cpuUsage, double memoryUsage)
        {
            this.PartitionKey = "0000";

            this.RowKey = "" + roleInstanceId;

            this.roleInstanceId = roleInstanceId;
            this.cpuUsage = cpuUsage;
            this.memoryUsage = memoryUsage;

            switch (state)
            {
                case State.Idle:
                    this.state = "Idle";
                    break;
                case State.Loading:
                    this.state = "Loading";
                    break;
                case State.Crawling:
                    this.state = "Crawling";
                    break;
                case State.Stopping:
                    this.state = "Stopping";
                    break;
                case State.Stopped:
                    this.state = "Stopped";
                    break;
                case State.Resetting:
                    this.state = "Resetting";
                    break;
                case State.Reset:
                    this.state = "Reset";
                    break;
            }
        }
    }
}
