using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;
using WebRole;
using WorkerRole;

namespace WebRole
{
    /// <summary>
    /// Summary description for WebService
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class WebService1 : System.Web.Services.WebService
    {
        private CloudTable table;
        private CloudTable dash;
        private CloudQueue commandq;

        private void cloudInitialization()
        {
            CloudStorageAccount storageAccount =
                CloudStorageAccount.Parse(ConfigurationManager.AppSettings["kcinfo344"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference("pa3Table");
            table.CreateIfNotExists();
            dash = tableClient.GetTableReference("dashboard");
            dash.CreateIfNotExists();
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            commandq = queueClient.GetQueueReference("command");
            commandq.CreateIfNotExists();
        }


        [WebMethod]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public void start(string web)
        {
            cloudInitialization();
            if (web.Contains("www.cnn"))
            {
                CloudQueueMessage message = new CloudQueueMessage("http://www.cnn.com/robots.txt");
                commandq.AddMessage(message);
            }
            else if (web.Contains("sports"))
            {
                CloudQueueMessage message2 = new CloudQueueMessage("http://sportsillustrated.cnn.com/robots.txt");
                commandq.AddMessage(message2);
            }
        }

        [WebMethod]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public List<string> getDashboard()
        {
            List<string> result = new List<string>();
            cloudInitialization();
            TableQuery<Dashboard> query = new TableQuery<Dashboard>()
                    .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "worker1"));
            
            foreach (Dashboard entity in dash.ExecuteQuery(query))
            {
                result.Add(entity.CPU.ToString());
                result.Add(entity.RAM.ToString());

                if (entity.LastTen == null)
                {
                    List<string> empty = new List<string>();
                    result.Add(empty.ToString());
                } else {
                    result.Add(entity.LastTen.ToString());
                }
                result.Add(entity.TotalCrawled.ToString());
                result.Add(entity.qCnt.ToString());
                result.Add(entity.iCnt.ToString());
                if (entity.ErrorUrl == null)
                {
                    List<string> empty = new List<string>();
                    result.Add(empty.ToString());
                } else {
                    result.Add(entity.ErrorUrl.ToString());
                }
            }
            return result;
        }

        [WebMethod]
        public void stop()
        {
            cloudInitialization();
            CloudQueueMessage message = new CloudQueueMessage("stop");
            commandq.AddMessage(message);
        }

        // pass in url and find the title
        [WebMethod]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public string find(string website)
        {
            try
            {
                string title = "";
                TableQuery<UrlInfo> query = new TableQuery<UrlInfo>()
                    .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, website));

                foreach (UrlInfo entity in table.ExecuteQuery(query))
                {
                    title = entity.Title;
                }
                return title;
            }
            catch (Exception EX)
            {
                return "website not found: " + EX;
            }
        }

        // clear the table
        [WebMethod]
        public void clear()
        {
            stop();
            CloudQueueMessage message = new CloudQueueMessage("clear");
            commandq.AddMessage(message);
        }


    }
}
