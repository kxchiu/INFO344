using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Share;

namespace WebRole1
{
    public class DashboardInfo
    {
        public List<WorkerInfo> workers { set; get; }
        public Dashboard info { set; get; }

        public DashboardInfo() { }

        public DashboardInfo(List<WorkerInfo> workers, Dashboard info)
        {
            this.workers = workers;
            this.info = info;
        }
    }
}
