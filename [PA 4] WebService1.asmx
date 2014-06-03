using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Share;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Web.Script.Services;
using System.Web.Services;
using WorkerRole1.Share;

namespace WebRole1
{
    /// <summary>
    /// Summary description for WebService1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class WebService1 : System.Web.Services.WebService
    {
        static Trie tree;
        const string WIKITITLE_FILENAME = "wikititles.txt";
        private PerformanceCounter memProcess = new PerformanceCounter("Memory", "Available MBytes");
        static string last;
        static int total;

        //---------------------------------------------------
        //----------------------PA 2-------------------------
        //---------------------------------------------------

        [WebMethod]
        public float GetAvailableMBytes()
        {
            float memUsage = memProcess.NextValue();
            return memUsage;
        }

        private void DownloadFileFromBlobToDisk()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["kcinfo344"]);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("lecture8");

            BlobRequestOptions options = new BlobRequestOptions();
            options.MaximumExecutionTime = new TimeSpan(0, 100, 0);
            options.ServerTimeout = new TimeSpan(0, 100, 0);


            if (container.Exists())
            {
                foreach (IListBlobItem item in container.ListBlobs(null, false))
                {
                    if (item.GetType() == typeof(CloudBlockBlob))
                    {
                        CloudBlockBlob blob = (CloudBlockBlob)item;
                        LocalResource myStorage = RoleEnvironment.GetLocalResource("lessonStorage");
                        string filePath = Path.Combine(myStorage.RootPath, "wiki.txt");
                        using (FileStream fs = new FileStream(filePath, FileMode.Create))
                        {
                            blob.DownloadToStream(fs, null, options);
                        }
                    }
                }
            }
        }

        private void ReadFromDisk()
        {
            string line;
            int counter = 0;
            float memLeft = 0;
            LocalResource myStorage = RoleEnvironment.GetLocalResource("lessonStorage");
            System.IO.StreamReader file = new System.IO.StreamReader(Path.Combine(myStorage.RootPath, "wiki.txt"));
            while ((line = file.ReadLine()) != null)
            {
                counter++;
                last = line;
                total++;
                if (counter % 1000 == 0)
                {
                    memLeft = GetAvailableMBytes();
                }
                if (memLeft > 50)
                {
                    tree.AddTitle(line);
                }

            }
            file.Close();
        }

        private void LoadFromBlob()
        {
            try
            {
                TrieNode exist = tree.root;
            }
            catch (NullReferenceException)
            {
                tree = new Trie();
                DownloadFileFromBlobToDisk();
                ReadFromDisk();
            }
        }

        [WebMethod]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public List<string> Search(string word)
        {
            LoadFromBlob();
            return tree.GetTenResults(word.ToLower().Trim());
        }

        //---------------------------------------------------
        //----------------------PA 3-------------------------
        //---------------------------------------------------

        [WebMethod]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public string getTitle(string url)
        {
            return findTitle(url);
        }

        private string findTitle(string url)
        {
            CloudTable webpageInfoTable = Azure.GetInstance().getTableReference(ConfigurationManager.AppSettings["pageInfo"]);
            IEnumerator<PageInfo> results = webpageInfoTable.ExecuteQuery(new TableQuery<PageInfo>().Where(
                TableQuery.GenerateFilterCondition("url", QueryComparisons.Equal, url)
            )).GetEnumerator();
            if (results.MoveNext())
            {
                return results.Current.title;
            }
            return "Not Page was Found";
        }

        /// <summary>
        /// to start crawling
        /// </summary>
        /// <param name="workerId"></param>
        [WebMethod]
        public void startCrawl(int workerId)
        {
            performAction(new CMsg(Command.AddDomain, "http://sportsillustrated.cnn.com/"));
            performAction(new CMsg(Command.AddDomain, "http://cnn.com"));
            performAction(new CMsg(Command.Start, workerId));
        }

        /// <summary>
        /// to stop crawling
        /// </summary>
        /// <param name="workerId"></param>
        [WebMethod]
        public void stopCrawl(int workerId)
        {
            performAction(new CMsg(Command.Stop, workerId));
        }

        /// <summary>
        /// to reset the database
        /// </summary>
        [WebMethod]
        public void resetDatabase()
        {
            performAction(new CMsg(Command.Reset));
        }

        private void performAction(CMsg command)
        {
            CommandQueue.GetInstance().addMessage(command);
        }

        [WebMethod]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public DashboardInfo getUpdates()
        {
            List<WorkerInfo> workers = Worker.GetInstance().getProfiles();
            return new DashboardInfo (workers, DashboardUpdate.GetInstance().getProfile());
        }

        //---------------------------------------------------
        //----------------------PA 4-------------------------
        //---------------------------------------------------

        private string filterBuilder(string columnName, string[] keywords)
        {
            if (keywords.Length == 1)
            {
                return TableQuery.GenerateFilterCondition(columnName, QueryComparisons.Equal, keywords[0]);
            }
            else if (keywords.Length == 0) {
                return null;
            } 
            else
            {

                string[] filter = new string[keywords.Length];
                string or = TableOperators.Or;
                string filterCombine = "";

                for (var i = 0; i < keywords.Length; i++)
                {
                    filter[i] = TableQuery.GenerateFilterCondition(columnName, QueryComparisons.Equal, keywords[i]);
                }
                filterCombine = TableQuery.CombineFilters(filter[0], or, filter[1]);
                if (keywords.Length > 20)
                {
                    for (var i = 0; i < 20; i++)
                    {
                        filterCombine = TableQuery.CombineFilters(filterCombine, or, filter[i + 2]);
                    }
                }
                else
                {
                    for (var i = 0; i < keywords.Length - 2; i++)
                    {
                        filterCombine = TableQuery.CombineFilters(filterCombine, or, filter[i + 2]);
                    }
                }
                return filterCombine;
            }
        }

        [WebMethod]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public List<List<string>> searchTitle(string word)
        {
            string[] keywords = word.Split(' ');
            List<List<string>> searchResult = new List<List<string>>();
            Dictionary<string, int> dict = new Dictionary<string, int>();

            CloudTable mappingTable = Azure.GetInstance().getTableReference(ConfigurationManager.AppSettings["mappingInfo"]);
            CloudTable pageTable = Azure.GetInstance().getTableReference(ConfigurationManager.AppSettings["pageInfo"]);
                
            var wordResults = mappingTable.ExecuteQuery(new TableQuery<Mapping>().Where(filterBuilder("keyword", keywords)));

            if (wordResults.ToArray().Length > 0)
            {

                Mapping[] wResults = wordResults.ToArray();

                // if dictionary contains the keyword,
                //      add 1 to the value
                // else,
                //      add the keyword and 1

                string[] searchUrl = new string[wResults.Length];

                for (var i = 0; i < wResults.Length; i++)
                {
                    searchUrl[i] = wResults[i].url;
                    if (!dict.ContainsKey(wResults[i].url))
                    {
                        dict.Add(wResults[i].url, 1);
                    }
                    else
                    {
                        dict[wResults[i].url]++;
                    }
                }
                var sortedDictionary =
                    (from item
                         in dict
                         orderby item.Value
                         descending
                         select item);

                var pageResults = pageTable.ExecuteQuery(new TableQuery<PageInfo>().Where(filterBuilder("url", searchUrl)));

                if (pageResults.ToArray().Length != 0)
                {
                    List<string> data = new List<string>();
                    foreach (KeyValuePair<string, int> key in sortedDictionary)
                    {

                        foreach (PageInfo page in pageResults)
                        {
                            if (key.Key == page.url)
                            {
                                data.Add(page.title);
                                data.Add(page.url);
                                searchResult.Add(data);
                            }
                        }
                    }
                }
            }
            return searchResult;
        }

        [WebMethod]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public string lastTitle()
        {
            return last;
        }

        [WebMethod]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public int totalAdd()
        {
            return total;
        }

    }
}
