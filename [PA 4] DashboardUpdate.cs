using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace Share
{
    public class DashboardUpdate
    {
        public static DashboardUpdate instance;

        private CloudTable dashboardTable;
        private CloudTable pageTable;
        private CloudTable errorTable;
        private CloudTable mappingTable;
        private CloudQueue urlQ;

        public static DashboardUpdate GetInstance()
        {
            if (instance == null)
            {
                instance = new DashboardUpdate(
                    Azure.GetInstance().getTableReference(ConfigurationManager.AppSettings["dashboardInfo"]),
                    Azure.GetInstance().getTableReference(ConfigurationManager.AppSettings["pageInfo"]),
                    Azure.GetInstance().getTableReference(ConfigurationManager.AppSettings["errorInfo"]),
                    Azure.GetInstance().getTableReference(ConfigurationManager.AppSettings["mappingInfo"]),
                    Azure.GetInstance().getQueueReference(ConfigurationManager.AppSettings["urlQ"])
                );
            }
            return instance;
        }

        public DashboardUpdate(CloudTable generalInfoTable, CloudTable pageTable, CloudTable errorTable, CloudTable mappingTable, CloudQueue urlQ)
        {
            this.dashboardTable = generalInfoTable;
            this.pageTable = pageTable;
            this.errorTable = errorTable;
            this.mappingTable = mappingTable;
            this.urlQ = urlQ;

            IEnumerator<Dashboard> results = generalInfoTable.ExecuteQuery(new TableQuery<Dashboard>()).GetEnumerator();
            if (!results.MoveNext())
            {
                generalInfoTable.Execute(TableOperation.InsertOrReplace(new Dashboard(0, 0, 0, new List<string>(), new List<string>())));
            }
        }

        public void clear()
        {
            Dashboard dInfo = new Dashboard(0, 0, 0, new List<string>(), new List<string>());
            dInfo.ETag = "*";
            dashboardTable.Execute(TableOperation.Replace(dInfo));
        }

        public Dashboard getProfile()
        {
            Dashboard dProfile = new Dashboard(0, 0, 0, new List<string>(), new List<string>());
            try
            {
                IEnumerator<Dashboard> results = dashboardTable.ExecuteQuery(new TableQuery<Dashboard>()).GetEnumerator();
                if (results.MoveNext())
                {
                    Dashboard result = results.Current;
                    dProfile.numUrlsCrawled = result.numUrlsCrawled;
                    dProfile.numUrlsIndexed = result.numUrlsIndexed;
                }

                foreach (PageInfo webinfo in pageTable.ExecuteQuery((new TableQuery<PageInfo>()).Take(10)))
                {
                    dProfile.urlsCrawled.Add(webinfo.url);
                }

                foreach (ErrorUrl crawlError in errorTable.ExecuteQuery((new TableQuery<ErrorUrl>()).Take(100)))
                {
                    dProfile.crawlErrors.Add(crawlError.message + ": " + crawlError.url);
                }

                urlQ.FetchAttributes();
                dProfile.numUrlsQueued = urlQ.ApproximateMessageCount.GetValueOrDefault(0);
            }
            catch (StorageException) {  }
            return dProfile;
        }

        public void increaseStats(int urlsCrawled, int urlsIndexed)
        {
            int retryCount = 10;
            while (retryCount > 0)
            {
                try
                {
                    IEnumerator<Dashboard> results = dashboardTable.ExecuteQuery(new TableQuery<Dashboard>()).GetEnumerator();
                    if (!results.MoveNext())
                    {
                        return;
                    }

                    Dashboard dInfo = results.Current;
                    dInfo.numUrlsCrawled += urlsCrawled;
                    dInfo.numUrlsIndexed += urlsIndexed;
                    dashboardTable.Execute(TableOperation.Replace(dInfo));
                }
                catch (StorageException)
                {
                    retryCount--;
                    continue;
                }
                break;
            }
        }
    }
}
