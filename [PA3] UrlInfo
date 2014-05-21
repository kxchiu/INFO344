using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole
{
    public class UrlInfo : TableEntity
    {
        public UrlInfo(string urlEncode, string title, string date, string category)
        {
            this.PartitionKey = category;
            this.RowKey = urlEncode;
            this.Url = urlEncode;
            this.Title = title;
            this.Date = date;
        }

        public UrlInfo() { }

        public string Url { get; set; }

        public string Title { get; set; }

        public string Date { get; set; }

        //public string Category { get; set; }
    }
}
