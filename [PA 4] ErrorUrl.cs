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
    /// To store crawled urls with errors
    /// </summary>
    public class ErrorUrl : TableEntity
    {
        public string url { set; get; }
        public string message { set; get; }

        public ErrorUrl() { }

        public ErrorUrl(string url, string message)
        {
            this.PartitionKey = String.Format("{0:D19}", DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks);
            this.RowKey = HttpUtility.UrlEncode(url);

            this.url = url;
            this.message = message;
        }
    }
}
