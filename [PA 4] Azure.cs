using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System.Configuration;

namespace Share
{
    public class Azure
    {
        private static Azure instance = null;
        private CloudStorageAccount storageAccount;

        public static Azure GetInstance()
        {
            if (instance == null)
            {
                instance = new Azure(ConfigurationManager.AppSettings["kcinfo344"]);
            }
            return instance;
        }

        private Azure(string connectionString)
        {
            storageAccount = CloudStorageAccount.Parse(connectionString);
        }

        public CloudTable getTableReference(string tableName)
        {
            CloudTable table = storageAccount.CreateCloudTableClient().GetTableReference(tableName);
            table.CreateIfNotExists();
            return table;
        }

        public CloudQueue getQueueReference(string queueName)
        {
            CloudQueue queue = storageAccount.CreateCloudQueueClient().GetQueueReference(queueName);
            queue.CreateIfNotExists();
            return queue;
        }
    }
}
