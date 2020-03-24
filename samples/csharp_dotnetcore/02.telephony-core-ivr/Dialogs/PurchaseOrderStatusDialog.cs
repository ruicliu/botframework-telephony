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
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class PurchaseOrderStatusDialog : WaterfallDialog
    {
        private readonly VoiceFactory VoiceFactory;
        private readonly string TransferNumber;
        private const string OrderNumberStepMsgText = "Please state your purchase order code like 'J12345'.";
        private const string OrderNumberRetryStepMsgText = "I'm sorry, I didn't get that. Please state your purchase order code, which contains a letter followed by 5 digits like 'K54321'";
        public PurchaseOrderStatusDialog(VoiceFactory voiceFactory, IConfiguration configuration): base(nameof(PurchaseOrderStatusDialog))
        {
            VoiceFactory = voiceFactory;

            AddStep(OrderNumberStepAsync);
            AddStep(FinalStepAsync);

            //Get transfer number from config
            TransferNumber = configuration["TransferNumber"];
        }

        private async Task<DialogTurnResult> OrderNumberStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var orderDetails = (PurchaseOrderStatusViewModel)stepContext.Options;

            if (orderDetails.PONumber == null)
            {
                //Build and send the next prompt if the viewmodel is not populated
                var promptMessage = VoiceFactory.TextAndVoice(OrderNumberStepMsgText, InputHints.ExpectingInput);
                var retryMessage = VoiceFactory.TextAndVoice(OrderNumberRetryStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(PurchaseOrderNumberPrompt), new PromptOptions { Prompt = promptMessage, RetryPrompt = retryMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(orderDetails.PONumber, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var orderDetails = (PurchaseOrderStatusViewModel)stepContext.Options;

            orderDetails.PONumber = (string)stepContext.Result;

            var poStatusResponse = $"For information about purchase order {orderDetails.PONumber}, stay on the line and we will transfer you to a skilled representative.";
            await stepContext.Context.SendActivityAsync(VoiceFactory.TextAndVoice(poStatusResponse, InputHints.IgnoringInput), cancellationToken);

            await Task.Delay(10000); //Temporary hack to make sure message is done reading out loud before transfer starts. Bug is tracked to fix this issue.

            //Create handoff event, passing the phone number to transfer to as context.
            var poContext = new { TargetPhoneNumber = TransferNumber };
            var poHandoffEvent = EventFactory.CreateHandoffInitiation(stepContext.Context, poContext);
            await stepContext.Context.SendActivityAsync(
                poHandoffEvent,
                cancellationToken);

            return await stepContext.EndDialogAsync();
        }
    }
}
