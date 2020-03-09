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
        private readonly string TransferNumber;
        private const string OrderNumberStepMsgText = "Please state your purchase order code like 'N12345'.";
        public PurchaseOrderStatusDialog(IConfiguration configuration): base(nameof(PurchaseOrderStatusDialog))
        {
            AddStep(OrderNumberStepAsync);
            AddStep(FinalStepAsync);

            TransferNumber = configuration["TransferNumber"];
        }

        private async Task<DialogTurnResult> OrderNumberStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var orderDetails = (PurchaseOrderStatusViewModel)stepContext.Options;

            if (orderDetails.PONumber == null)
            {
                var promptMessage = MessageFactory.Text(OrderNumberStepMsgText, OrderNumberStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(PurchaseOrderNumberPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(orderDetails.PONumber, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var orderDetails = (PurchaseOrderStatusViewModel)stepContext.Options;

            orderDetails.PONumber = (string)stepContext.Result;

            var poStatusResponse = $"For information about purchase order {orderDetails.PONumber}, stay on the line and we will transfer you to a skilled representative.";
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(poStatusResponse, poStatusResponse, InputHints.IgnoringInput), cancellationToken);

            await Task.Delay(8000);
            var poContext = new { TargetPhoneNumber = TransferNumber };
            var poHandoffEvent = EventFactory.CreateHandoffInitiation(stepContext.Context, poContext);
            await stepContext.Context.SendActivityAsync(
                poHandoffEvent,
                cancellationToken);

            return await stepContext.EndDialogAsync();
        }
    }
}
