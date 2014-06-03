using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Share
{
    /// <summary>
    /// To store information about crawled pages
    /// </summary>
    public class PageInfo : TableEntity
    {
        public string title { get; set; }
        public string url { get; set; }
        public DateTime date { get; set; }
        public long crawl { get; set; }

        public PageInfo() { }

        public PageInfo(string url) : this(null, url, DateTime.MinValue) { }

        public PageInfo(string title, string url, DateTime datetime)
        {
            this.PartitionKey = String.Format("{0:D19}", DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks);
            this.RowKey = HttpUtility.UrlEncode(url);

            this.title = title;
            this.url = url;
            this.date = datetime;
            this.crawl = DateTime.Now.Ticks;
        }
    }
}
