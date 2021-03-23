// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MessageReactionBot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class EchoBot : ActivityHandler
    {
        private readonly MessageReceiptStorage messageReceiptStorage;

        public EchoBot(IStorage istorage)
        {
            messageReceiptStorage = new MessageReceiptStorage(istorage);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var utterance = turnContext.Activity.Text;

            if (string.IsNullOrEmpty(utterance)) return;

            var replyText = $"You said: {utterance}";
            var sendActivity = MessageFactory.Text(replyText, replyText);
            var resourceResponse = await turnContext.SendActivityAsync(sendActivity);

            await messageReceiptStorage.SaveAsync(resourceResponse.Id, sendActivity, cancellationToken);

            await turnContext.SendActivityAsync($"message stored with id '{resourceResponse.Id}'");

            // For testing, feed the bot the response id and verify that it is stored. This what OnReactionsAddedAsync will do when it's working
            if (utterance.Length>=3 && utterance.Contains('-'))
            {
                var text = await messageReceiptStorage.FindAsync(utterance, cancellationToken);
                if(text != null)
                {
                    await turnContext.SendActivityAsync($"I see that you read my message {text}");
                }
            }
        }

        protected override async Task OnReactionsAddedAsync(IList<MessageReaction> messageReactions, ITurnContext<IMessageReactionActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var reaction in messageReactions)
            {
                if (reaction.Type == "messageRead")
                {
                    // The ReplyToId property of the inbound MessageReaction Activity will correspond 
                    // to a Message Activity which had previously been sent from this bot.
                    var text = messageReceiptStorage.FindAsync(turnContext.Activity.ReplyToId, cancellationToken);
                    if (text != null)
                    {
                        await turnContext.SendActivityAsync($"I see that you have read my message '{text}'");
                    }
                }
            }
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Hello and welcome!";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                }
            }
        }
    }
}
