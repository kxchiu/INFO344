using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Share;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WorkerRole1
{
    public class Crawler
    {
        public int numThreads { get; set; }

        private bool crawl;
        private RobotsReader robotTxtMngr;
        private HashSet<string> authorities;
        private HashSet<Thread> threads;
        private CloudQueue urlQueue;
        private CloudTable webinfoTable;
        private CloudTable crawlErrorsTable;

        private readonly string[] SUPPORTED_DOMAINS = { "cnn.com", "sportsillustrated.cnn.com" };

        public Crawler(CloudQueue urlQueue, CloudTable webinfoTable, CloudTable crawlErrorsTable) : this(urlQueue, webinfoTable, crawlErrorsTable, 1) { }

        public Crawler(CloudQueue urlQueue, CloudTable webinfoTable, CloudTable crawlErrorsTable, int numThreads)
        {
            this.numThreads = numThreads;
            this.crawl = false;
            this.robotTxtMngr = new RobotsReader();
            this.authorities = new HashSet<string>();
            this.threads = new HashSet<Thread>();
            this.urlQueue = urlQueue;
            this.webinfoTable = webinfoTable;
            this.crawlErrorsTable = crawlErrorsTable;
        }

        public void addAuthority(string authority)
        {
            authorities.Add(authority);
        }

        public void clearAuthorities()
        {
            authorities.Clear();
        }

        private void crawlThreadFunction()
        {
            Trace.TraceInformation("WebCrawler::crawlThreadFunction(): new crawling thread created");
            while (crawl)
            {
                CloudQueueMessage msg = urlQueue.GetMessage(new TimeSpan(0, 3, 0));
                if (msg != null)
                {
                    string url = msg.AsString;
                    Trace.TraceInformation("WebCrawler::crawlThreadFunction(): Processing " + url);

                    if (!shouldCrawl(url))
                    {
                        continue;
                    }

                    try
                    {
                        CNNPage.CNNPage parser = new CNNPage.CNNPage(url);
                        PageInfo pageInfo = parser.getWebpageInfo();
                        HashSet<string> links = parser.getLinks();

                        foreach (string link in links)
                        {
                            if (shouldCrawl(link))
                            {
                                urlQueue.BeginAddMessage(new CloudQueueMessage(link), (IAsyncResult ar) => { return; }, null);
                            }
                        }

                        webinfoTable.Execute(TableOperation.Insert(pageInfo));

                        DashboardUpdate.GetInstance().increaseStats(1, 1);
                    }
                    catch (WebException e)
                    {
                        if (e.Status == WebExceptionStatus.ProtocolError)
                        {
                            var response = e.Response as HttpWebResponse;
                            if (response != null)
                            {
                                crawlErrorsTable.Execute(TableOperation.Insert(new ErrorUrl(url, "Page Unreachable; HTTP Status Code: " + (int)response.StatusCode)));
                            }
                            else
                            {
                                crawlErrorsTable.Execute(TableOperation.Insert(new ErrorUrl(url, "Page Unreachable; HTTP Status Code N/A")));
                            }
                        }
                        else
                        {
                            crawlErrorsTable.Execute(TableOperation.Insert(new ErrorUrl(url, "Page Unreachable; HTTP Status Code N/A")));
                        }
                        DashboardUpdate.GetInstance().increaseStats(1, 0);
                    }
                    catch (WebParseDatetimeException)
                    {
                        crawlErrorsTable.Execute(TableOperation.Insert(new ErrorUrl(url, "Missing Last Modified Date")));

                        DashboardUpdate.GetInstance().increaseStats(1, 0);
                    }
                    catch (UriFormatException)
                    {
                        crawlErrorsTable.Execute(TableOperation.Insert(new ErrorUrl(url, "Invalid URL")));

                        DashboardUpdate.GetInstance().increaseStats(1, 0);
                    }
                    urlQueue.DeleteMessage(msg);
                }
                else
                {
                    Trace.TraceInformation("WebCrawler::crawlThreadFunction(): No URL in queue");
                    Thread.Sleep(200);
                }
            }
            Trace.TraceInformation("WebCrawler::crawlThreadFunction(): crawling thread terminated");
        }

        public bool isCrawling()
        {
            urlQueue.FetchAttributes();
            return urlQueue.ApproximateMessageCount > 0 && threads.Count > 0;
        }

        private void parseSitemapThreadFunction(Object o)
        {
            Trace.TraceInformation("WebCrawler::parseSitemapThreadFunction(): starting");

            Robots parser = (Robots)o;
            parser.parseSitemaps();

            HashSet<string> links = parser.getLinks();
            Trace.TraceInformation("WebCrawler::parseSitemapThreadFunction(): finished parsing robots.txt, found " + links.Count + " links");

            foreach (string link in links)
            {
                if (!crawl)
                {
                    break;
                }

                if (shouldCrawl(link))
                {
                    urlQueue.AddMessage(new CloudQueueMessage(link));
                }
            }
            Trace.TraceInformation("WebCrawler::parseSitemapThreadFunction(): finished");
        }

        public void resetDatabase()
        {
            stop(true);

            urlQueue.Clear();

            webinfoTable.Delete();
            crawlErrorsTable.Delete();

            while (true)
            {
                try
                {
                    webinfoTable.CreateIfNotExists();
                }
                catch (StorageException)
                {
                    continue;
                }
                break;
            }
            while (true)
            {
                try
                {
                    crawlErrorsTable.CreateIfNotExists();
                }
                catch (StorageException)
                {
                    continue;
                }
                break;
            }
            DashboardUpdate.GetInstance().clear();
        }

        private bool shouldCrawl(string link)
        {
            if ((new Urls(link)).getExtension() != ".html")
            {
                return false;
            }

            bool valid = false;
            foreach (string supportedDomain in SUPPORTED_DOMAINS)
            {
                if (Regex.Match((new Urls(link)).getDomain(), Regex.Escape(supportedDomain) + "$").Success)
                {
                    valid = true;
                    break;
                }
            }
            if (!valid)
            {
                return false;
            }
            foreach (string disallowed in robotTxtMngr.getAllDisallowed())
            {
                if (link.StartsWith(disallowed))
                {
                    return false;
                }
            }
            TableQuery<PageInfo> webinfoQuery = new TableQuery<PageInfo>().Where(
                TableQuery.GenerateFilterCondition("url", QueryComparisons.Equal, link)
            );
            if (webinfoTable.ExecuteQuery(webinfoQuery).GetEnumerator().MoveNext())
            {
                return false;
            }

            TableQuery<ErrorUrl> crawlErrorQuery = new TableQuery<ErrorUrl>().Where(
                TableQuery.GenerateFilterCondition("url", QueryComparisons.Equal, link)
            );
            if (crawlErrorsTable.ExecuteQuery(crawlErrorQuery).GetEnumerator().MoveNext())
            {
                return false;
            }

            return true;
        }

        public void start()
        {
            Trace.TraceInformation("WebCrawler::start(): called");
            if (crawl)
            {
                Trace.TraceInformation("WebCrawler::start(): already crawling, exited");
                return;
            }
            crawl = true;

            foreach (string authority in authorities)
            {
                Trace.TraceInformation("WebCrawler::parseRobotsTxtFunction(): processing " + authority);

                Urls url = new Urls(authority);
                string domain = url.getDomain();
                if (!SUPPORTED_DOMAINS.Contains(domain))
                {
                    Trace.TraceInformation("WebCrawler::parseRobotsTxtFunction(): domain " + domain + " not supported");
                }

                Trace.TraceInformation("WebCrawler::parseRobotsTxtFunction(): begin parsing robots.txt");
                try
                {
                    robotTxtMngr.addParser(authority);
                }
                catch (WebException)
                {

                    Trace.TraceInformation("WebCrawler::parseRobotsTxtFunction(): robots.txt does not exist");
                }

                if (domain == "cnn.com")
                {
                    robotTxtMngr.getParser(authority).excludeEntriesBefore(DateTime.Now.AddMonths(-3));
                }
                else if (domain == "sportsillustrated.cnn.com")
                {
                    robotTxtMngr.getParser(authority).excludeUrlsWithout("nba");
                }

                Thread thread = new Thread(new ParameterizedThreadStart(parseSitemapThreadFunction));
                thread.Start(robotTxtMngr.getParser(authority));
                threads.Add(thread);
            }

            for (int i = 0; i < numThreads; i++)
            {
                Trace.TraceInformation("WebCrawler::start(): creating new thread");
                Thread thread = new Thread(new ThreadStart(crawlThreadFunction));
                thread.Start();
                threads.Add(thread);
            }

            Trace.TraceInformation("WebCrawler::start(): exited");
        }

        public void stop(bool wait)
        {
            Trace.TraceInformation("WebCrawler::stop(): called");

            if (!crawl)
            {
                return;
            }

            crawl = false;

            if (wait)
            {
                foreach (Thread thread in threads)
                {
                    thread.Join();
                }
            }

            threads.Clear();
        }

        public bool stopped()
        {
            return !crawl;
        }
    }
}
