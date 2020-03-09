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
    public class OrderStatusDialog : WaterfallDialog
    {
        private const string OrderNumberStepMsgText = "What is your five digit order number?";
        public OrderStatusDialog(): base(nameof(OrderStatusDialog))
        {
            AddStep(OrderNumberStepAsync);
            AddStep(FinalStepAsync);
        }

        private async Task<DialogTurnResult> OrderNumberStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var orderDetails = (OrderStatusViewModel)stepContext.Options;

            if (orderDetails.OrderNumber == null)
            {
                var promptMessage = MessageFactory.Text(OrderNumberStepMsgText, OrderNumberStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync("OrderNumberPrompt", new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(orderDetails.OrderNumber, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var orderDetails = (OrderStatusViewModel)stepContext.Options;

            orderDetails.OrderNumber = (string)stepContext.Result;

            var response = $"Order {orderDetails.OrderNumber} will arrive Wednesday.";
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(response, response, InputHints.IgnoringInput), cancellationToken);
            // Restart the main dialog with a different message the second time around
            var promptMessage = "What else can I do for you?";
            return await stepContext.ReplaceDialogAsync(nameof(CustomerDialog), promptMessage, cancellationToken);
        }
    }
}
