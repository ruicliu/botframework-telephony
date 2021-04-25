using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.BotFramework.Telephony.Broker;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotBuilderSamples.Controllers
{
    [ApiController]
    [Route("broker")]
    public class BrokerController : ControllerBase
    {
        private readonly IDictionary<string, JObject> _storage;
        private readonly LatchingKeyPool _latchingKeyPool;

        public BrokerController(IDictionary<string,JObject> storage, LatchingKeyPool latchingKeyPool)
        {
            _storage = storage;
            _latchingKeyPool = latchingKeyPool;
        }

        [HttpGet("{key}")]
        public JObject Get(string key)
        {
            //look up result based on key
            if(_storage.ContainsKey(key))
            {
                var returnVar = _storage[key];
                _latchingKeyPool.ReleaseKey(key);
                _storage[key] = null;
                return returnVar;
            }
            //return result if exists, else return null
            return null;
        }

        [HttpPost]
        public async Task<string> Post([FromBody] JObject context)
        {
            var key = await _latchingKeyPool.RequestKey();
            //Store context
            _storage[key] = context;
            //return a new phone number from the pool of numbers available
            return key;
        }
    }
}
