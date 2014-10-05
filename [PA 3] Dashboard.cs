using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole
{
    public class Dashboard : TableEntity
    {
        public Dashboard(string worker, float CPU, float RAM, Queue<string> LastTen, int TotalCrawled, int qCnt, int iCnt, List<string> ErrorUrl)
        {
            this.PartitionKey = "pa";
            this.RowKey = worker;
            this.CPU = CPU;
            this.RAM = RAM;
            this.LastTen = LastTen;
            this.TotalCrawled = TotalCrawled;
            this.qCnt = qCnt;
            this.iCnt = iCnt;
            this.ErrorUrl = ErrorUrl;
        }

        public Dashboard() { }

        public float CPU { get; set; }

        public float RAM { get; set; }

        public Queue<string> LastTen { get; set; }

        public int TotalCrawled { get; set; }
        
        public int qCnt { get; set; }
        
        public int iCnt { get; set; }
        
        public List<string> ErrorUrl { get; set; }
    }
}
