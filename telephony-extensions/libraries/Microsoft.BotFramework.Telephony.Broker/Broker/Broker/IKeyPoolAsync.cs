using System.Threading.Tasks;

namespace Microsoft.BotFramework.Telephony.Broker
{
    public interface IKeyPoolAsync
    {
        void ReleaseKey(string key);
        Task<string> RequestKey();
    }
}