using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;

namespace Share
{
    /// <summary>
    /// To store numerical data about the current crawling process
    /// </summary>
    public class Dashboard : TableEntity
    {
        public int numUrlsIndexed { set; get; }
        public int numUrlsCrawled { set; get; }
        public int numUrlsQueued { set; get; }
        public List<string> urlsCrawled;
        public List<string> crawlErrors;
        
        public Dashboard() { }

        public Dashboard(int numUrlsIndexed, int numUrlsCrawled, int numUrlsQueued, List<string> urlsCrawled, List<string> crawlErrors)
        {
            this.PartitionKey = "0000";
            this.RowKey = "0";

            this.numUrlsCrawled = numUrlsCrawled;
            this.numUrlsIndexed = numUrlsIndexed;
            this.numUrlsQueued = numUrlsQueued;
            this.urlsCrawled = urlsCrawled;
            this.crawlErrors = crawlErrors;
        }
    }
}
