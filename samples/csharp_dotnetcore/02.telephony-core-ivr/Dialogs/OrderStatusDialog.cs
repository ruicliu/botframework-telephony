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
    public class OrderStatusDialog : WaterfallDialog
    {
        private readonly VoiceFactory VoiceFactory;
        private const string OrderNumberStepMsgText = "What is your five digit order number?";
        private const string OrderNumberStepRetryMsgText = "I'm sorry, I didn't get that. Please state your five digit order number like '12345'";
        public OrderStatusDialog(VoiceFactory voiceFactory): base(nameof(OrderStatusDialog))
        {
            VoiceFactory = voiceFactory;

            AddStep(OrderNumberStepAsync);
            AddStep(FinalStepAsync);
        }

        private async Task<DialogTurnResult> OrderNumberStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var orderDetails = (OrderStatusViewModel)stepContext.Options;

            if (orderDetails.OrderNumber == null)
            {
                //Build and send the next prompt if the viewmodel is not populated
                var promptMessage = VoiceFactory.TextAndVoice(OrderNumberStepMsgText, InputHints.ExpectingInput);
                var retryMessage = VoiceFactory.TextAndVoice(OrderNumberStepRetryMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(OrderNumberPrompt), new PromptOptions { Prompt = promptMessage, RetryPrompt = retryMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(orderDetails.OrderNumber, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var orderDetails = (OrderStatusViewModel)stepContext.Options;

            orderDetails.OrderNumber = (string)stepContext.Result;

            var response = $"Order {orderDetails.OrderNumber} will arrive Wednesday.";
            await stepContext.Context.SendActivityAsync(VoiceFactory.TextAndVoice(response, InputHints.IgnoringInput), cancellationToken);
            // Restart the upstream dialog with a different message the second time around
            var promptMessage = "What else can I do for you?";
            return await stepContext.ReplaceDialogAsync(nameof(CustomerDialog), promptMessage, cancellationToken);
        }
    }
}
