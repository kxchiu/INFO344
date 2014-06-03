using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WorkerRole1
{
    public class Urls : Uri
    {
        public Urls(string url) : base(url) { }

        public static string BuildAbsUrl(string currentUrl, string anotherUrl)
        {
            if (anotherUrl.Length > 0)
            {
                if (anotherUrl[0] == '/')
                {
                    return (new Urls(currentUrl)).getAuthority() + anotherUrl;
                }
                else if (anotherUrl.Contains(':'))
                {
                    return anotherUrl;
                }
                else
                {
                    return currentUrl.TrimEnd('/') + anotherUrl;
                }
            }
            else
            {
                return currentUrl;
            }
        }

        public string getAuthority()
        {
            return GetLeftPart(UriPartial.Authority);
        }

        public string getDomain()
        {
            return getAuthority().Replace("/www.", "/").Replace("http://", "");
        }

        public string getExtension()
        {
            return Path.GetExtension(AbsolutePath);
        }
    }
}
