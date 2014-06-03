using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace WorkerRole1.Share
{
    class Mapping : TableEntity
    {
        public string keyword { get; set; }
        public string url { get; set; }

        public Mapping() { }

        public Mapping(string keyword, string url)
        {
            this.PartitionKey = HttpUtility.UrlEncode(keyword);
            this.RowKey = HttpUtility.UrlEncode(url);

            this.keyword = keyword;
            this.url = url;
        }
    }
}
