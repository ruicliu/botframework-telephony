using Microsoft.BotFramework.Telephony.Broker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BrokerService.Tests
{
    public class ConcurrencyTests
    {
        private readonly ITestOutputHelper logger;
        public ConcurrencyTests(ITestOutputHelper output)
        {
            logger = output;
        }

        [Fact]
        public async Task PoolHandlesConcurrency()
        {
            var pool = new List<string>()
            {
                "a",
                "b",
                "c",
                "d",
                "e",
                "f",
                "g",
                "h",
                "i",
                "j"
            };
            var validationQueue = new ConcurrentQueue<(string,bool)>(); //key should never be reserved if latched

            var subject = new LatchingKeyPool(pool);
            List<Task> poolRequests = new List<Task>();
            for (var i = 0; i < 10000; i++)
            {
                poolRequests.Add(Task.Run(
                    async () =>
                    {
                        var key = await subject.RequestKey();
                        validationQueue.Enqueue((key, true));
                        await Task.Delay(10);
                        //before giving up our latch on the key, but after we have had it for a bit, mark it ready to release
                        validationQueue.Enqueue((key, false));
                        subject.ReleaseKey(key);
                    }
                ));
            }
            await Task.WhenAll(poolRequests);

            var consumedKeys = new HashSet<string>();
            //Guarantees that in the order events happened, no desyncs happened.
            foreach((string key, bool toggle) in validationQueue)
            {
                if(toggle == true)
                {
                    Assert.DoesNotContain(key, consumedKeys);
                    consumedKeys.Add(key);
                } else
                {
                    Assert.True(consumedKeys.Remove(key));
                }
            }
        }
    }
}
