// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
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
        private BotState ConversationState;
        private readonly VoiceFactory VoiceFactory;

        public EchoBotWithRecording(ConversationState conversationState, VoiceFactory voiceFactory)
        {
            ConversationState = conversationState;
            VoiceFactory = voiceFactory;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var conversationStateAccessors = ConversationState.CreateProperty<ConversationData>(nameof(ConversationData));
            var conversationData = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationData());

            // Wait for recording to start.
            if ((TelephonyExtensions.IsTelephonyChannel(turnContext.Activity.ChannelId)) && 
                (conversationData.RecordingState == RecordingState.Uninitialized))
            {
                var waitText = $"Please wait while your call is setup.";

                await turnContext.SendActivityAsync(
                    VoiceFactory.TextAndVoiceNoBargeIn(waitText),
                    cancellationToken);

                return;
            }

            var userText = turnContext.Activity.Text;
            if (string.IsNullOrWhiteSpace(userText))
            {
                return;
            }

            // Echo what the caller says
            string replyText = $"You said {userText}";

            await turnContext.SendActivityAsync(
                VoiceFactory.TextAndVoice(replyText),
                cancellationToken);
            
        }

        protected override async Task OnRecordingStartResultAsync(
            ITurnContext<IActivity> turnContext,
            CancellationToken cancellationToken)
        {
            var recordingResult = turnContext.Activity.GetCommandResultValue<object>();
            if (recordingResult.Error != null)
            {
                // Recording error!
                await turnContext.SendActivityAsync(
                   new Activity(type: ActivityTypes.EndOfConversation),
                    cancellationToken);

                return;
            }

            var conversationStateAccessors = ConversationState.CreateProperty<ConversationData>(nameof(ConversationData));
            var conversationData = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationData());
            conversationData.RecordingState = RecordingState.Recording;

            // Send a consent message to the user to let them know the call may be recorded.
            var consentText = "Your call may be recorded or monitored for quality assurance purposes.";

            await turnContext.SendActivityAsync(
                    VoiceFactory.TextAndVoiceNoBargeIn(consentText),
                    cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    // Welcome message that will not be recorded
                    // Played to minimize initial silence till call recording starts
                    var welcome = "Hello and welcome.";
                    await turnContext.SendActivityAsync(
                        VoiceFactory.TextAndVoiceNoBargeIn(welcome),
                        cancellationToken);

                    // Start recording if Telephony channel
                    if (TelephonyExtensions.IsTelephonyChannel(turnContext.Activity.ChannelId))
                    {
                        await TelephonyExtensions.TryRecordingStart(turnContext, cancellationToken);
                    }
                }
            }
        }
    }
}
