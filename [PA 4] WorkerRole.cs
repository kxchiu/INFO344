using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Share;
using System.Web;
using System.Configuration;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        WorkerInfo.State currentState;
        private Crawler webCrawler;

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole::Run(): started");

            webCrawler = new Crawler(
                Azure.GetInstance().getQueueReference(ConfigurationManager.AppSettings["urlQ"]),
                Azure.GetInstance().getTableReference(ConfigurationManager.AppSettings["pageInfo"]),
                Azure.GetInstance().getTableReference(ConfigurationManager.AppSettings["errorInfo"])
            );

            updateState(WorkerInfo.State.Stopped);
            while (true)
            {
                CMsg commandMsg = CommandQueue.GetInstance().getMessage();
                if (commandMsg != null)
                {
                    switch (commandMsg.command)
                    {
                        case Command.AddDomain:
                            webCrawler.addAuthority((new Urls(commandMsg.str).getAuthority()));
                            break;
                        case Command.Start:
                            updateState(WorkerInfo.State.Loading, true);
                            webCrawler.start();
                            break;
                        case Command.Stop:
                            updateState(WorkerInfo.State.Stopping, true);
                            webCrawler.clearAuthorities();
                            webCrawler.stop(true);
                            updateState(WorkerInfo.State.Stopped, true);
                            break;
                        case Command.Reset:
                            updateState(WorkerInfo.State.Stopping, true);
                            webCrawler.stop(true);
                            updateState(WorkerInfo.State.Resetting, true);
                            webCrawler.resetDatabase();
                            updateState(WorkerInfo.State.Reset, true);
                            break;
                    }
                    CommandQueue.GetInstance().removeMessage(commandMsg);
                }

                if (!webCrawler.stopped())
                {
                    updateState(WorkerInfo.State.Crawling);
                }
                WorkerMonitor.GetInstance().updateProfile(currentState);
            }
        }

        private void updateState(WorkerInfo.State state, bool instantRefresh = false)
        {
            currentState = state;
            if (instantRefresh)
            {
                WorkerMonitor.GetInstance().updateProfile(state);
            }
        }

        public override bool OnStart()
        {
            ServicePointManager.DefaultConnectionLimit = 20;
            return base.OnStart();
        }
    }
}
