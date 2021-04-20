using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// Example broker client. 
    /// It is recommended to implement your own, more strictly typed client.
    /// Please follow best practices for writing httpclients (this is not the point of this sample).
    /// </summary>
    public class BrokerClient
    {
        private static HttpClient _client;
        public BrokerClient(string BaseURL)
        {
            _client = new HttpClient();
            _client.BaseAddress = new Uri(BaseURL);
        }

        public BrokerClient(HttpClient Client)
        {
            _client = Client;
        }

        public async Task<JObject> Fetch(string Key)
        {
            var result = await _client.GetAsync("/broker/" + Key);
            return JObject.Parse(await result.Content.ReadAsStringAsync());
        }

        public async Task<string> Store(JObject Context)
        {
            var content = new StringContent(Context.ToString());
            var result = await _client.PostAsync("/broker", content);
            return await result.Content.ReadAsStringAsync();
        }
    }
}
