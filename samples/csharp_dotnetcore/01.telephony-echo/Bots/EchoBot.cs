// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class EchoBot : ActivityHandler
    {
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {

            //Wait for the user to say something :)
            if(string.IsNullOrWhiteSpace(turnContext.Activity.Text)) {
                return;
            }

            //Echo what they say!
            var replyText = $"You said {turnContext.Activity.Text}";
            await turnContext.SendActivityAsync(
                MessageFactory.Text(
                    replyText,
                    SimpleConvertToSSML(
                        replyText,
                        "Microsoft Server Speech Text to Speech Voice (en-US, JessaRUS)",
                        "en-US")
                ), cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Hello and welcome!";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(
                        MessageFactory.Text(
                            welcomeText,
                            SimpleConvertToSSML(
                                welcomeText,
                                "Microsoft Server Speech Text to Speech Voice (en-US, JessaRUS)",
                                "en-US")
                            ),
                        cancellationToken);
                }
            }
        }

        private string SimpleConvertToSSML(string text, string voiceId, string locale)
        {
            try
            {
                string ssmlTemplate = $"<speak version=\"1.0\" xmlns=\"https://www.w3.org/2001/10/synthesis\" xml:lang=\"{locale}\"> <voice name=\"{voiceId}\"> {text} </voice> </speak>";
                return ssmlTemplate;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
