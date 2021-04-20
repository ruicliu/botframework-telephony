using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// Holds list of keys with latching condition
    /// </summary>
    public class LatchingKeyPool
    {
        private SemaphoreSlim latchingSemaphore = new SemaphoreSlim(1, 1);
        private readonly ConcurrentDictionary<string, bool> pool = new ConcurrentDictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);

        public LatchingKeyPool(List<string> Pool)
        {
            foreach (var key in Pool)
            {
                pool[key] = false;
            };
        }

        /// <summary>
        /// Finds the next unlatched key, latches it and returns it. Must be thread safe.
        /// </summary>
        /// <returns></returns>
        public async Task<string> RequestKey()
        {
            while (true)
            {
                try
                {
                    await latchingSemaphore.WaitAsync();

                    //search through our pool for the next unlatched key and latch it
                    foreach (var pair in pool)
                    {
                        if (pair.Value == false)
                        {
                            pool[pair.Key] = true;
                            return pair.Key;
                        }
                    }
                }
                finally
                {
                    latchingSemaphore.Release();
                }
                await Task.Delay(10); //We need the task.delay in case this thread is the one blocking the release.
            }
        }

        public void ReleaseKey(string key)
        {
            pool[key] = false;
        }
    }
}
