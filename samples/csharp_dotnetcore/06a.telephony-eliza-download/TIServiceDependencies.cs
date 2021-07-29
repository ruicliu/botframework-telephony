using Newtonsoft.Json;
using System;

namespace tiClient.TIServiceDependencies
{
    public sealed class SignalRRouteConstants
    {
        /// <summary>
        /// signalr endpoint to receive insights
        /// </summary>
        public const string InsightReceivingEndpoint = "SendInsightMessageAsync";

        /// <summary>
        /// signalr endpoint to receive subcribed user message
        /// </summary>
        public const string SubscribedMessageReceivingEndpoint = "SendSubscribedMessageAsync";

        /// <summary>
        /// signalr (service) endpoint to subscribe to a specific call/topic
        /// </summary>
        public const string SubscribeToCall = "SubscribeAsync";
    }

    public class SubscriptionMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionMessage"/> class.
        /// </summary>
        /// <param name="userId"> user id </param>
        [JsonConstructor]
        public SubscriptionMessage(string userId)
        {
            this.UserId = userId ?? throw new ArgumentNullException(userId);
        }

        /// <summary>
        /// User Id
        /// </summary>
        // todo: there will be a single subscription across all users per TI resource.
        [JsonProperty("userId")]
        public string UserId { get; }
    }
}
