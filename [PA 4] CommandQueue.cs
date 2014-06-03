using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;

namespace Share
{
    public class CommandQueue
    {
        public static CommandQueue instance = null;
        private static JavaScriptSerializer json = new JavaScriptSerializer();

        private CloudQueue queue;
        public TimeSpan visibilityTimeout { get; set; }

        public static CommandQueue GetInstance()
        {
            if (instance == null)
            {
                instance = new CommandQueue(Azure.GetInstance().getQueueReference(ConfigurationManager.AppSettings["commandQ"]));
            }
            return instance;
        }

        private CommandQueue(CloudQueue queue)
        {
            this.queue = queue;
            visibilityTimeout = new TimeSpan(0, 3, 0);
        }

        public void addMessage(Command command, string parameter = "")
        {
            addMessage(new CMsg(command, parameter));
        }

        public void addMessage(CMsg msg)
        {
            queue.AddMessage(new CloudQueueMessage(json.Serialize(msg)));
        }

        public CMsg getMessage()
        {
            CloudQueueMessage msg = queue.GetMessage(visibilityTimeout);
            if (msg != null)
            {
                CMsg cMsg = json.Deserialize<CMsg>(msg.AsString);
                cMsg.cloudQueueMsg = msg;
                return cMsg;
            }
            else
            {
                return null;
            }
        }

        public void removeMessage(CMsg msg)
        {
            queue.DeleteMessage(msg.cloudQueueMsg);
        }
    }
}
