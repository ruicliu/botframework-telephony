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
        private readonly VoiceFactory VoiceFactory;
        protected readonly ILogger Logger;

        private const string IntroText = "Ask about when tax forms will be delivered, leave policies, or the status of your purchase order.";
        private const string ReintroText = "What else can I do for you?";
        private const string HangupText = "Thank you and have a great day";
        private const string TaxFormsResponse = "Tax forms will be mailed in February.";

        public EmployeeDialog(IVRRecognizer luisRecognizer, VoiceFactory voiceFactory, ILogger<MainDialog> logger)
            : base(nameof(EmployeeDialog))
        {
            _luisRecognizer = luisRecognizer;
            Logger = logger;
            VoiceFactory = voiceFactory;

            AddStep(IntroStepAsync);
            AddStep(ActStepAsync);
            AddStep(FinalStepAsync);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Use the text provided in FinalStepAsync or the default if it is the first time.
            var messageText = stepContext.Options?.ToString() ?? IntroText;
            var promptMessage = VoiceFactory.TextAndVoice(messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Call LUIS, try and identify what the user is trying to do.
            //If the entities are included in the response, convert them to a viewmodel and use them in the next prompt, otherwise prompt the user for the missing data.
            var luisResult = await _luisRecognizer.RecognizeAsync<IVR>(stepContext.Context, cancellationToken);
            switch (luisResult.TopIntent().intent)
            {
                case IVR.Intent.TaxForms:
                    await stepContext.Context.SendActivityAsync(VoiceFactory.TextAndVoice(TaxFormsResponse), cancellationToken);
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
                case IVR.Intent.Cancel:
                    //If the user indicates that they are done with the call, leave a message and hangup.
                    var goodbyeMessage = VoiceFactory.TextAndVoice(HangupText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(goodbyeMessage, cancellationToken);

                    await Task.Delay(3000); // Temporary hack to make sure that the message has finished being read.Bug is tracked to fix this issue.

                    //End of conversation activity currently causes a HangUp. This functionality is experimental, and the API might change.
                    await stepContext.Context.SendActivityAsync(Activity.CreateEndOfConversationActivity(),cancellationToken);
                    return await stepContext.EndDialogAsync();
                default:
                    // Catch all for unhandled intents
                    var didntUnderstandMessageText = $"Sorry, I didn't get that. Please try asking in a different way (intent was {luisResult.TopIntent().intent}). What I heard was {stepContext.Context.Activity.Text}";
                    var didntUnderstandMessage = VoiceFactory.TextAndVoice(didntUnderstandMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);
                    break;
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Restart this dialog with a different message the second time around
            return await stepContext.ReplaceDialogAsync(nameof(EmployeeDialog), ReintroText, cancellationToken);
        }
    }
}
