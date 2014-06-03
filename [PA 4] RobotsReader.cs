using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole1
{
    class RobotsReader
    {
        private Dictionary<string, Robots> robotParsers = new Dictionary<string, Robots>();

        public RobotsReader() { }

        public void addParser(string authority)
        {
            robotParsers[authority] = new Robots(authority);
        }

        public HashSet<string> getAllDisallowed()
        {
            HashSet<string> disallowed = new HashSet<string>();
            foreach (Robots parser in robotParsers.Values)
            {
                disallowed.UnionWith(parser.getDisallowed());
            }
            return disallowed;
        }

        public HashSet<string> getAllLinks()
        {
            HashSet<string> links = new HashSet<string>();
            foreach (Robots parser in robotParsers.Values)
            {
                links.UnionWith(parser.getLinks());
            }
            return links;
        }

        public Robots getParser(string authority)
        {
            try
            {
                return robotParsers[authority];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }
    }
}
