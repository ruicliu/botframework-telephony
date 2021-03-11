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

            // If we have not yet started recording, ask the user the wait.
            if ((turnContext.Activity.ChannelId == "telephony") && 
                (conversationData.RecordingState == RecordingState.Uninitialized))
            {
                var waitText = $"Please wait";

                await turnContext.SendActivityAsync(
                    VoiceFactory.TextAndVoice(waitText, InputHints.IgnoringInput), 
                    cancellationToken);

                return;
            }

            // Recording is either started, resumed or paused.
            // We are ready to reply to the bot
            var userText = turnContext.Activity.Text;
            if (string.IsNullOrWhiteSpace(userText))
            {
                return;
            }

            // Echo what the caller says
            var replyText = $"You said {userText}";
            await turnContext.SendActivityAsync(
                    VoiceFactory.TextAndVoice(replyText, InputHints.IgnoringInput),
                    cancellationToken);

            // Simple command to test pausing the recording
            if (userText == "pause")
            {
                // Pause only if the recording is active. Ignore the command otherwise.
                if (conversationData.RecordingState == RecordingState.Recording)
                {
                    await RecordingHelpers.TryPauseRecording(turnContext, cancellationToken);
                }
            }

            // Simple command to test resuming the recording
            if (userText == "resume")
            {
                // Resume only if the recording is paused. Ignore the command otherwise.
                if (conversationData.RecordingState == RecordingState.Paused)
                {
                    await RecordingHelpers.TryResumeRecording(turnContext, cancellationToken);
                }
                
            }
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
                    VoiceFactory.TextAndVoice(consentText, InputHints.IgnoringInput),
                    cancellationToken);
        }

        protected override async Task OnRecordingPauseResultAsync(
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
            conversationData.RecordingState = RecordingState.Paused;
        }

        protected override async Task OnRecordingResumeResultAsync(
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
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    // Welcome message that will not recorded
                    // Played to minimize initial silence till call recording starts
                    var welcome = "Hello and welcome to the ivr bot";
                    await turnContext.SendActivityAsync(
                        VoiceFactory.TextAndVoice(welcome, InputHints.IgnoringInput),
                        cancellationToken);

                    // Start recording if Telephony channel
                    await RecordingHelpers.TryStartRecording(turnContext, cancellationToken);
                }
            }
        }
    }
}
