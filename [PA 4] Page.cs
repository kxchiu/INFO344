using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using Share;
using HtmlAgilityPack;
using System;

namespace WorkerRole1
{
    public class WebParseDatetimeException : Exception
    {
        public WebParseDatetimeException() : base() { }
    }

    public class Page
    {
        protected HtmlDocument parser;
        protected HtmlNode root;
        protected string html;
        protected PageInfo webpageInfo;
        public string url;

        public Page(string url)
        {
            this.url = url;
            webpageInfo = new PageInfo(url);
            html = new WebClient().DownloadString(url);

            parser = new HtmlDocument();
            parser.LoadHtml(html);
            root = parser.DocumentNode;
        }

        PageInfo getWebpageInfo()
        {
            return null;
        }

        public HashSet<string> getLinks()
        {
            HashSet<string> links = new HashSet<string>();
            HtmlNodeCollection nodes = root.SelectNodes("//*[@href]");
            if (nodes != null)
            {
                foreach (HtmlNode node in nodes)
                {
                    links.Add(Urls.BuildAbsUrl(webpageInfo.url, node.GetAttributeValue("href", "")));
                }
            }
            return links;
        }
    }
}
