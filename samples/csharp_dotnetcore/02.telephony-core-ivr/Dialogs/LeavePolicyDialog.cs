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
        private const string LeavePolicyStepMsgText = "Are you asking about parental leave, sabbaticals, or bereavement leave?";
        public LeavePolicyDialog(IConfiguration configuration) : base(nameof(LeavePolicyDialog))
        {
            AddStep(PolicyTypeStepAsync);
            AddStep(FinalStepAsync);

            TransferNumber = configuration["TransferNumber"];
        }

        private async Task<DialogTurnResult> PolicyTypeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var leavePolicyDetails = (LeavePolicyViewModel)stepContext.Options;

            if (leavePolicyDetails.LeaveOfAbscensePolicy == null)
            {
                var promptMessage = MessageFactory.Text(LeavePolicyStepMsgText, LeavePolicyStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(
                    nameof(ChoicePrompt),
                    new PromptOptions
                    {
                        Prompt = promptMessage,
                        Choices = ChoiceFactory.ToChoices(new List<string> { "Parental leave", "Sabbatical", "Bereavement leave" })
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

            if (leavePolicyDetails.LeaveOfAbscensePolicy == "Bereavement leave")
            {
                var bereavementResponse = "In line with federal mandate, we offer three days of paid bereavement leave. If you need someone to talk to, stay on the line and we will put you in touch with our on site bereavement counciler.";
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(bereavementResponse, bereavementResponse, InputHints.IgnoringInput), cancellationToken);
                await Task.Delay(8000);
                var context = new { TargetPhoneNumber = TransferNumber };
                var handoffEvent = EventFactory.CreateHandoffInitiation(stepContext.Context, context);
                await stepContext.Context.SendActivityAsync(
                    handoffEvent,
                    cancellationToken);

                return await stepContext.EndDialogAsync();
            }
            else
            {
                var leavePolicyResponse = $"Both our parental leave and sabbatical policies are 12 weeks.";
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(leavePolicyResponse, leavePolicyResponse, InputHints.IgnoringInput), cancellationToken);
            }

            // Restart the main dialog with a different message the second time around
            var promptMessage = "What else can I do for you?";
            return await stepContext.ReplaceDialogAsync(nameof(EmployeeDialog), promptMessage, cancellationToken);
        }
    }
}
