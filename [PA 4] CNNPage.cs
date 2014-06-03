using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Share;
using System.Xml;
using HtmlAgilityPack;
using Microsoft.WindowsAzure.Storage.Table;
using WorkerRole1.Share;
using System.Configuration;
using Microsoft.WindowsAzure.Storage;

namespace WorkerRole1.CNNPage
{
    class CNNPage : Page
    {
        public CloudTable mappingTable;

        public CNNPage(string url) : base(url) { }

        public PageInfo getWebpageInfo()
        {
            mappingTable = Azure.GetInstance().getTableReference("mappingtable");
            webpageInfo.title = "";
            HtmlNode node = root.SelectSingleNode("//title");
            if (node != null)
            {
                webpageInfo.title = node.InnerText;
            }

            string title = webpageInfo.title;
            string[] keywords = title.Split(' ');

            try
            {
                foreach (string keyword in keywords)
                {
                    mappingTable.Execute(TableOperation.Insert(new Mapping(keyword, url)));
                }
            }
            catch (StorageException) { }

            string datetime = null;
            node = root.SelectSingleNode("//meta[@http-equiv='last-modified']");
            if (node != null)
            {
                datetime = node.GetAttributeValue("content", null);
            }
            else
            {
                node = root.SelectSingleNode("//meta[@name='date']");
                if (node != null)
                {
                    datetime = node.GetAttributeValue("content", null);
                }
                else
                {
                    Match match = Regex.Match(html, @"<div>Posted:\s*[^<]+\s*;\s*Updated:\s*([^<]+)\s*</div>");
                    if (match.Success)
                    {
                        datetime = match.Groups[1].Value;
                    }
                    else
                    {
                        match = Regex.Match(html, @"<div>Posted:\s*([^<]+)\s*</div>");
                        if (match.Success)
                        {
                            datetime = match.Groups[1].Value;
                        }
                    }
                }
            }

            try
            {
                webpageInfo.date = DateTime.Parse(datetime);
            }
            catch (ArgumentNullException)
            {
                Trace.TraceInformation("Datetime not fetched for url: " + webpageInfo.url);
                throw new WebParseDatetimeException();
            }
            catch (FormatException)
            {
                Trace.TraceInformation("Datetime not parsable: " + datetime);
                throw new WebParseDatetimeException();
            }
            return webpageInfo;
        }
    }
}
