// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.BotBuilderSamples.Bots.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class EchoBot : ActivityHandler
    {
        private static BrokerClient _client;
        public EchoBot(BrokerClient Client)
        {
            _client = Client;
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var replyText = $"Got it, {turnContext.Activity.Text}. We will transfer you with this context";
            await turnContext.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);

            //Store context in brokerservice
            //We need to add a + to take our keys from the format 1234567890 to +1234567890, the standard phone number format.
            var targetNumber = await _client.Store(JObject.FromObject(new ContextModel() { UserName = turnContext.Activity.Text }));
            //call handoff
            var context = new { TargetPhoneNumber = "+"+targetNumber };
            var handoffEvent = EventFactory.CreateHandoffInitiation(turnContext, context);
            await turnContext.SendActivityAsync(
                handoffEvent,
                cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            
            
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    string context = null;
                    string botNumber = null;
                    //If channel == telephony
                    if (turnContext.Activity.ChannelId == "telephony")
                    {
                        //  get the number we were called on (note, not called from)
                        botNumber = turnContext.Activity.Recipient.Name;
                        //  try get context from broker for this number.
                        //  number comes in standard format with +, which IIS doesn't like. URL encoding it doesn't work either, as the implementors only implemented application/x-www-form-urlencoded, and not path encoding.
                        //  long story short, we drop the +
                        var brokerResult = await _client.Fetch(botNumber.Substring(1));
                        context = brokerResult?.ToObject<ContextModel>()?.UserName;
                    }

                    //  if not null, we can populate user name
                    //  else, give generic answer
                    var welcomeText = !string.IsNullOrEmpty(context) ? $"Greetings {context}. You've been successfully transferred to {botNumber}." : "Greetings.";
                    welcomeText += " Please provide your name so that we can transfer you with context.";
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                }
            }
        }
    }
}
