// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.CommandExtensions;
using Microsoft.Bot.Schema.Telephony;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class EchoBotWithRecording : TelephonyActivityHandler
    {
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // Wait for the user to say something
            var userText = turnContext.Activity.Text;
            if (String.IsNullOrWhiteSpace(userText))
            {
                return;
            }

            // Echo what they say

            var replyText = $"You said {userText}";
            await turnContext.SendActivityAsync(
                MessageFactory.Text(
                    replyText,
                    SimpleConvertToSSML(
                        replyText,
                        "Microsoft Server Speech Text to Speech Voice (en-US, JessaRUS)",
                        "en-US")
                ), cancellationToken);
        }

        protected override async Task OnRecordingStartResultAsync(
            ITurnContext<ICommandResultActivity> turnContext,
            CancellationToken cancellationToken)
        {
            var result = CommandExtensions.GetCommandResultValue<object>(turnContext.Activity);

            string recordingStatusText;
            if (result.Error != null)
            {
                recordingStatusText = $"Recording start failed";
            }
            else
            {
                recordingStatusText = $"Recording start succeeded";
            }

            await turnContext.SendActivityAsync(
                    MessageFactory.Text(
                        recordingStatusText,
                        SimpleConvertToSSML(
                            recordingStatusText,
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
                        TelephonyExtensions.CreateRecordingStartCommand(), 
                        cancellationToken);

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
                string ssmlTemplate = $"<speak version='1.0' xmlns='https://www.w3.org/2001/10/synthesis' xml:lang='{locale}'><voice name='{voiceId}'>{text}</voice></speak>";
                return ssmlTemplate;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
