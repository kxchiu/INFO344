using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace WorkerRole1
{
    public class Robots
    {
        private string content;
        private DateTime excludeBeforeDatetime;
        private string excludeUrlsWithoutToken;
        private HashSet<string> disallowed = new HashSet<string>();
        private HashSet<string> links = new HashSet<string>();

        public Robots(string authority)
        {
            string robotsTXTURL = authority + "/robots.txt";
            content = new WebClient().DownloadString(robotsTXTURL);

            int index = content.IndexOf("User-agent: *");
            if (index >= 0)
            {
                string disallowedContent = content.Substring(index);
                foreach (Match match in Regex.Matches(disallowedContent, @"Disallow:\s*([/-_.A-Za-z0-9]+)"))
                {
                    disallowed.Add(Urls.BuildAbsUrl(robotsTXTURL, match.Groups[1].Value));
                }
            }

            excludeUrlsWithoutToken = "";
        }

        public void excludeEntriesBefore(DateTime datetime)
        {
            excludeBeforeDatetime = datetime;
        }

        public void excludeUrlsWithout(string token)
        {
            excludeUrlsWithoutToken = token;
        }

        public HashSet<string> getDisallowed()
        {
            return disallowed;
        }

        public HashSet<string> getLinks()
        {
            return links;
        }

        private HashSet<string> parseSitemap(string url)
        {
            HashSet<string> urls = new HashSet<string>();
            try
            {
                content = new WebClient().DownloadString(url);

                XmlDocument sitemap = new XmlDocument();
                sitemap.LoadXml(content);
                XmlNodeList urlNodes = sitemap.GetElementsByTagName("sitemapindex");
                if (urlNodes.Count > 0)
                {
                    XmlNodeList sitemapNodes = urlNodes[0].ChildNodes;
                    foreach (XmlNode node in sitemapNodes)
                    {
                        if (node["lastmod"] != null)
                        {
                            DateTime entryLastModified;
                            if (DateTime.TryParse(node["lastmod"].InnerText, out entryLastModified))
                            {
                                if (entryLastModified.CompareTo(excludeBeforeDatetime) < 0)
                                {
                                    continue;
                                }
                            }
                        }

                        if (node["loc"] != null)
                        {
                            string crawlUrl = node["loc"].InnerText;
                            if (crawlUrl.Contains(excludeUrlsWithoutToken))
                            {
                                urls.UnionWith(parseSitemap(node["loc"].InnerText));
                            }
                        }
                    }
                }
                else
                {
                    urlNodes = sitemap.GetElementsByTagName("urlset");
                    if (urlNodes.Count > 0)
                    {
                        XmlNodeList sitemapNodes = urlNodes[0].ChildNodes;
                        foreach (XmlNode node in sitemapNodes)
                        {
                            if (node["loc"] != null)
                            {
                                string crawlUrl = node["loc"].InnerText;
                                if (crawlUrl.Contains(excludeUrlsWithoutToken))
                                {
                                    urls.Add(crawlUrl);
                                }
                            }
                        }
                    }
                }
            }
            catch (WebException) { /* 404 or Network Error */ }
            catch (XmlException) { /* Download Incomplete */ }
            return urls;
        }

        public void parseSitemaps()
        {
            foreach (Match match in Regex.Matches(content, @"Sitemap:\s*([/_.:A-Za-z0-9-]+)"))
            {
                string what = match.Groups[1].Value;

                links.UnionWith(parseSitemap(match.Groups[1].Value));
            }
        }
    }
}
