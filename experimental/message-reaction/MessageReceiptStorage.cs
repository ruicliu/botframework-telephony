using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace MessageReactionBot
{
    // How to set up: https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-howto-v4-storage?view=azure-bot-service-4.0&tabs=csharp#using-cosmos-db

    public class ConvoRecord
    {
        public string ID { get; set; }
        public string Text { get; set; }
        public string ETag { get; set; } = "*";
    }

    public class MessageReceiptStorage
    {
        private readonly IStorage storage;

        public MessageReceiptStorage(IStorage storage)
        {
            this.storage = storage;
        }

        /// <summary>
        /// Save a message associated with a given id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="activity"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task SaveAsync(string id, Activity activity, CancellationToken cancellationToken)
        {
            var logItems = new ConvoRecord();
            logItems.Text = activity.Text;

            // Create dictionary object to hold received user messages.
            var changes = new Dictionary<string, object>();
            {
                changes.Add(id, logItems);
            }

            await storage.WriteAsync(changes, cancellationToken);
        }

        /// <summary>
        /// Find the message associated with the given id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>null if message not found</returns>
        public async Task<string> FindAsync(string id, CancellationToken cancellationToken)
        {
            string[] responseId = { id };
            for (int i = 0; i < 5; i++)
            {
                var result = storage.ReadAsync<ConvoRecord>(responseId).Result;
                if (result != null && result.Any())
                {
                    var entry = result.First().Value;
                    return entry.Text;
                }
                await Task.Delay(100);
            }

            return null;
        }
    }
}
