using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace DataWriter
{
    public class LinkEntity : TableEntity
    {
        // Set up Partition and Row Key information
        public LinkEntity(string pk, string rk)
        {
            this.PartitionKey = pk;
            this.RowKey = rk;
        }

        public LinkEntity() { }
        public string Product { get; set; }
        public double Sentiment { get; set; }
    }

    class Program
    {
        static readonly Random r = new Random();
        public static async Task AddRecordToDb(string product, double sentiment)
        {
            CloudStorageAccount storageAccount =
            new CloudStorageAccount(new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials("recordingti", "JHUCnysRZdq3hWi/R7acdv+Q1iIFy6YArgcZ2kf6A3r5OCoUAOFhsvgNnwkM9vtIsgLJqRrWZWOYTQ0ajwgjmA=="),
                true);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            CloudTable _linkTable = tableClient.GetTableReference("products");

            // Create a new entity.
            LinkEntity link = new LinkEntity("p1", r.Next().ToString())
            {
                Product = product,
                Sentiment = sentiment
            };

            // Create the TableOperation that inserts the customer entity.
            TableOperation insertOperation = TableOperation.InsertOrMerge(link);

            await _linkTable.ExecuteAsync(insertOperation);
        }

        static void Main(string[] args)
        {
            AddRecordToDb("refrigerator", 90.0).Wait();
            Console.WriteLine("Hello World!");
        }
    }
}
