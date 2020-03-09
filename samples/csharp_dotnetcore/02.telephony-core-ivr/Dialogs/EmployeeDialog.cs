// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreBot.CognitiveModels;
using CoreBot.DialogViewModels;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class EmployeeDialog : WaterfallDialog
    {
        private readonly IVRRecognizer _luisRecognizer;
        protected readonly ILogger Logger;

        // Dependency injection uses this constructor to instantiate MainDialog
        public EmployeeDialog(IVRRecognizer luisRecognizer, ILogger<MainDialog> logger)
            : base(nameof(EmployeeDialog))
        {
            _luisRecognizer = luisRecognizer;
            Logger = logger;

            AddStep(IntroStepAsync);
            AddStep(ActStepAsync);
            AddStep(FinalStepAsync);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Use the text provided in FinalStepAsync or the default if it is the first time.
            var messageText = stepContext.Options?.ToString() ?? "Ask about when tax forms will be delivered, leave policies, or the status of your purchase order.";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Call LUIS and gather any potential booking details. (Note the TurnContext has the response to the prompt.)
            var luisResult = await _luisRecognizer.RecognizeAsync<IVR>(stepContext.Context, cancellationToken);
            switch (luisResult.TopIntent().intent)
            {
                case IVR.Intent.TaxForms:
                    var taxFormsResponse = $"Tax forms will be mailed in February.";
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(taxFormsResponse, taxFormsResponse), cancellationToken);
                    break;
                case IVR.Intent.POStatus:
                    var poStatusResult = luisResult.Entities.ToObject<POStatusModel>();
                    var poStatusViewModel = new PurchaseOrderStatusViewModel();

                    if (poStatusResult.PONumber != null && poStatusResult.PONumber.Length > 0)
                    {
                        poStatusViewModel.PONumber = poStatusResult.PONumber.First();
                    }

                    return await stepContext.ReplaceDialogAsync(nameof(PurchaseOrderStatusDialog), poStatusViewModel);
                case IVR.Intent.LeavePolicy:
                    var leavePolicyResult = luisResult.Entities.ToObject<LeavePolicyModel>();
                    var leavePolicyViewModel = new LeavePolicyViewModel();

                    if (leavePolicyResult.LeaveOfAbscensePolicies != null && leavePolicyResult.LeaveOfAbscensePolicies.Length > 0)
                    {
                        leavePolicyViewModel.LeaveOfAbscensePolicy = leavePolicyResult.LeaveOfAbscensePolicies.First().First();
                    }

                    return await stepContext.ReplaceDialogAsync(nameof(LeavePolicyDialog), leavePolicyViewModel);
                default:
                    // Catch all for unhandled intents
                    var didntUnderstandMessageText = $"Sorry, I didn't get that. Please try asking in a different way (intent was {luisResult.TopIntent().intent}). What I heard was {stepContext.Context.Activity.Text}";
                    var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);
                    break;
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Restart the main dialog with a different message the second time around
            var promptMessage = "What else can I do for you?";
            return await stepContext.ReplaceDialogAsync(nameof(EmployeeDialog), promptMessage, cancellationToken);
        }
    }
}
