using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
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

namespace WebService2
{
    /// <summary>
    /// Summary description for WebService1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class WebService2 : System.Web.Services.WebService
    {
        static Trie tree;
        const string WIKITITLE_FILENAME = "wikititles.txt";
        private PerformanceCounter memProcess = new PerformanceCounter("Memory", "Available MBytes");

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
                if (counter % 10 == 0)
                {
                    memLeft = GetAvailableMBytes();
                }
                if (memLeft > 20)
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
                TrieNode poop = tree.root;
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
    }
}
