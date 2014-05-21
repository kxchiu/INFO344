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
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Queue;
using System.IO;
using System.Xml;
using System.Globalization;
using HtmlAgilityPack;
using System.Web;
using WorkerRole;

namespace WorkerRole1
{

    public class WorkerRole : RoleEntryPoint
    {
        private Boolean stop = true;
        private CloudTable table;
        private CloudTable dashB;
        private CloudQueue urlQ;
        private CloudQueue commandQ;

        private HashSet<string> disallow = new HashSet<string>();
        private HashSet<string> visited = new HashSet<string>();
        private Queue<string> lastTen = new Queue<string>();
        private List<string> errorUrl = new List<string>();

        private DateTime today = DateTime.Now;
        private PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        private PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        
        private int tCrawled = 0;
        private int qCnt = 0;
        private int iCnt = 0;

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole1 entry point called");
            cloudInitialization();

            while (true)
            {
                urlQ.FetchAttributes();
                qCnt = (int)urlQ.ApproximateMessageCount;
                getComMessage();
                if (!stop)
                {
                    getUrlMessage();
                }

                Thread.Sleep(500);
                Trace.TraceInformation("Working");
            }
        }

        private void cloudInitialization()
        {
            CloudStorageAccount storageAccount =
                CloudStorageAccount.Parse(ConfigurationManager.AppSettings["kcinfo344"]);

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference("pa3Table");
            table.CreateIfNotExists();
            dashB = tableClient.GetTableReference("dashboard");
            dashB.CreateIfNotExists();

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            commandQ = queueClient.GetQueueReference("command");
            commandQ.CreateIfNotExists();
            urlQ = queueClient.GetQueueReference("url");
            urlQ.CreateIfNotExists();

            updateDash();
        }

        private float getCPUCount()
        {
            return cpuCounter.NextValue();
        }

        private float getRAMCount()
        {
            return ramCounter.NextValue();
        }

        private void updateDash()
        {
            Dashboard dashI = new Dashboard("worker1", getCPUCount(), getRAMCount(), lastTen, tCrawled,
                   qCnt, iCnt, errorUrl);
            TableOperation insertOperation = TableOperation.InsertOrReplace(dashI);

            dashB.Execute(insertOperation);
        }

        private void getUrlMessage()
        {
            CloudQueueMessage url = urlQ.GetMessage();
            if (url != null)
            {
                urlQ.DeleteMessage(url);
                try
                {
                    WebClient wClient = new WebClient();
                    string htmlString = wClient.DownloadString(url.AsString);
                    HtmlDocument htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(htmlString);
                    getURLTitle(htmlDoc, url.AsString);
                    downloadSubUrls(htmlDoc, url.AsString);
                }
                catch (Exception EX)
                {
                    errorUrl.Add(EX + ": " + url.AsString);
                }
            }
        }

        private void getComMessage()
        {
            CloudQueueMessage comMessage = commandQ.GetMessage();
            if (comMessage != null)
            {
                commandQ.DeleteMessage(comMessage);
                string mes = comMessage.AsString;
                if (mes.Equals("stop"))
                {
                    stop = true;
                }
                else if (mes.Equals("clear"))
                {
                    disallow = new HashSet<string>();
                    visited = new HashSet<string>();
                    lastTen = new Queue<string>();
                    errorUrl = new List<string>();

                    tCrawled = 0;
                    qCnt = 0;
                    iCnt = 0;

                    ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                    cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
 
                    urlQ.Clear();
                    table.Delete();
                    updateDash();
                }
                else
                {
                    stop = false;

                    getRobots(mes);
                }
            }
        }

        private void updateLastTenUrl(string url)
        {
            if (lastTen.Count > 9)
            {
                lastTen.Dequeue();
            }
            lastTen.Enqueue(url);
        }

        private void getURLTitle(HtmlDocument htmlDoc, string url)
        {
            var metas = htmlDoc.DocumentNode.Descendants("meta");
            string date = "";

            foreach (var metaTag in metas)
            {
                if (metaTag.Attributes.Contains("http-equiv") &&
                    metaTag.Attributes["http-equiv"].Value == "last-modified")
                {
                    date = metaTag.Attributes["content"].Value;
                    break;
                }
            }

            if (date != "" && compareDate(date))
            {
                var title = htmlDoc.DocumentNode.SelectSingleNode("//title").InnerText;
                Uri uriAddress = new Uri(url);
                string category = uriAddress.Segments[1].Trim(new char[] { '/' }).ToLower();

                var urlencode = HttpUtility.UrlEncode(url);
                UrlInfo urlInfo = new UrlInfo(category, urlencode, title, date);
                TableOperation insertOperation = TableOperation.Insert(urlInfo);
                table.Execute(insertOperation);
                iCnt++;
                updateLastTenUrl(url);

                updateDash();
            }
        }

        // if valid, add to urlq
        private void downloadSubUrls(HtmlDocument htmlDoc, string url)
        {
            var hrefs = htmlDoc.DocumentNode.Descendants("a");
            string href = "";

            foreach (var aTag in hrefs)
            {
                href = aTag.Attributes["href"].Value;
                if (href.EndsWith("html") || href.EndsWith("htm"))
                //Uri uriAddress = new Uri(href);
                //if (uriAddress.GetLeftPart(UriPartial.Path).EndsWith("html") || uriAddress.GetLeftPart(UriPartial.Path).EndsWith("htm"))
                {
                    if (!href.StartsWith("http://"))
                    {
                        if (url.Contains("www.cnn"))
                        {
                            href = "http://www.cnn.com" + href;
                        }
                        else
                        {
                            href = "http://sportsillustrated.cnn.com" + href;
                        }
                        // !disallow.Contains(uriAddress.Segments[1])
                        string[] tokens = url.Split(new string[] { "/" }, StringSplitOptions.None);
                        if (!disallow.Contains(tokens[3]) && !visited.Contains(href))
                        {
                            visited.Add(href);
                            CloudQueueMessage m = new CloudQueueMessage(href);
                            urlQ.AddMessage(m);
                        }
                    }
                    else
                    {
                        if (href.StartsWith("http://www.cnn.com") || href.StartsWith("http://www.sportsillustrated.cnn.com"))
                        {
                            string[] tokens = href.Split(new string[] { "/" }, StringSplitOptions.None);
                            if (!disallow.Contains(tokens[3]) && !visited.Contains(href))
                            {
                                visited.Add(href);
                                CloudQueueMessage m = new CloudQueueMessage(href);
                                urlQ.AddMessage(m);
                            }
                        }
                    }
                }
            }
        }

        private bool compareDate(string day)
        {
            day = day.Substring(0, 10);
            DateTime lastModified = DateTime.ParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            double difference = today.Subtract(lastModified).TotalDays;
            return difference <= 60;
        }

        private XmlNodeList createList(string xml, string tagName)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(xml);
            return xDoc.GetElementsByTagName(tagName);
        }

        public void getRobots(string robot)
        {
            WebClient wClient = new WebClient();
            string htmlString = wClient.DownloadString(robot);
            string line;
            int temp = 0;
            using (StringReader reader = new StringReader(htmlString))
            {
                line = reader.ReadLine();
                if (line.StartsWith("Sitemap: "))
                {
                    string xml1 = line.Substring(9);
                    XmlNodeList sitemapList = createList(xml1, "sitemap");

                    foreach (XmlNode sitemap in sitemapList)
                    {
                        string day1 = sitemap["lastmod"].InnerText;
                        if (compareDate(day1))
                        {
                            string xml2 = sitemap["loc"].InnerText;
                            XmlNodeList urlList = createList(xml2, "url");
                            foreach (XmlNode url in urlList)
                            {
                                string day2 = url["lastmod"].InnerText;
                                if (compareDate(day2))
                                {
                                    string loc = url["loc"].InnerText;
                                    if (!visited.Contains(loc))
                                    {
                                        visited.Add(loc);
                                        CloudQueueMessage m = new CloudQueueMessage(loc);
                                        urlQ.AddMessage(m);
                                        temp++;
                                        //Trace.TraceInformation(urlq.GetMessage().AsString);
                                    }
                                }
                                if (temp > 1)
                                {
                                    break;
                                }
                            }
                        }
                        if (temp > 1)
                        {
                            break;
                        }
                    }
                }
                else if (line.StartsWith("Disallow: "))
                {
                    disallow.Add(line.Substring(10));
                }
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            return base.OnStart();
        }
    }
}
