// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreBot.CognitiveModels;
using CoreBot.DialogViewModels;
using CoreBot.Prompts;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class LeavePolicyDialog : WaterfallDialog
    {
        private readonly string TransferNumber;
        private readonly VoiceFactory VoiceFactory;
        private const string LeavePolicyStepMsgText = "Are you asking about parental leave, sabbaticals, or bereavement leave?";
        private const string LeavePolicyRetryMsgText = "I'm sorry, I didn't get that.";
        private const string BereavementResponse = "In line with federal mandate, we offer three days of paid bereavement leave. If you need someone to talk to, stay on the line and we will put you in touch with our on site bereavement counciler.";
        private const string ParentalAndSabbaticalPolicyResponse = "Both our parental leave and sabbatical policies are 12 weeks.";

        public LeavePolicyDialog(VoiceFactory voiceFactory, IConfiguration configuration) : base(nameof(LeavePolicyDialog))
        {
            VoiceFactory = voiceFactory;

            AddStep(PolicyTypeStepAsync);
            AddStep(FinalStepAsync);

            //Get transfer number from config
            TransferNumber = configuration["TransferNumber"];
        }

        private async Task<DialogTurnResult> PolicyTypeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var leavePolicyDetails = (LeavePolicyViewModel)stepContext.Options;

            if (leavePolicyDetails.LeaveOfAbscensePolicy == null)
            {
                //Build and send our prompt
                var promptMessage = VoiceFactory.TextAndVoice(LeavePolicyStepMsgText, InputHints.ExpectingInput);
                var retryMessageText = $"{LeavePolicyRetryMsgText} {LeavePolicyStepMsgText}";
                var retryMessage = VoiceFactory.TextAndVoice(retryMessageText, InputHints.ExpectingInput);

                var choices = new List<Choice>()
                {
                    new Choice() { Value = "Bereavement leave", Synonyms = new List<string>() { "bereavement","bereavement leave","bereavement leave policy" } },
                    new Choice() { Value = "Sabbatical", Synonyms = new List<string>() { "sabbatical","sabbatical policy", "sabbatical leave policy" } },
                    new Choice() { Value = "Parental leave", Synonyms = new List<string>() { "parental", "parental leave", "parental leave policy" } },
                };

                return await stepContext.PromptAsync(
                    nameof(ChoicePrompt),
                    new PromptOptions
                    {
                        Prompt = promptMessage,
                        Choices = choices,
                        RetryPrompt = retryMessage
                    },
                    cancellationToken);
            }

            return await stepContext.NextAsync(leavePolicyDetails.LeaveOfAbscensePolicy, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var leavePolicyDetails = (LeavePolicyViewModel)stepContext.Options;
            if (leavePolicyDetails.LeaveOfAbscensePolicy == null)
            {
                leavePolicyDetails.LeaveOfAbscensePolicy = ((FoundChoice)stepContext.Result).Value;
            }

            //Handle bereavement leave by transferring the call to a more capable bot.
            if (leavePolicyDetails.LeaveOfAbscensePolicy == "Bereavement leave")
            {
                await stepContext.Context.SendActivityAsync(VoiceFactory.TextAndVoice(BereavementResponse, InputHints.IgnoringInput), cancellationToken);

                await Task.Delay(10000); //Temporary hack to make sure message is done reading out loud before transfer starts. Bug is tracked to fix this issue.
                
                //Create handoff event, passing the phone number to transfer to as context.
                var context = new { TargetPhoneNumber = TransferNumber };
                var handoffEvent = EventFactory.CreateHandoffInitiation(stepContext.Context, context);
                await stepContext.Context.SendActivityAsync(
                    handoffEvent,
                    cancellationToken);

                return await stepContext.EndDialogAsync();
            }
            else
            {
                //Handle other leave types with a single message.
                await stepContext.Context.SendActivityAsync(VoiceFactory.TextAndVoice(ParentalAndSabbaticalPolicyResponse, InputHints.IgnoringInput), cancellationToken);
            }

            // Restart the upstream dialog with a different message the second time around
            var promptMessage = "What else can I do for you?";
            return await stepContext.ReplaceDialogAsync(nameof(EmployeeDialog), promptMessage, cancellationToken);
        }
    }
}