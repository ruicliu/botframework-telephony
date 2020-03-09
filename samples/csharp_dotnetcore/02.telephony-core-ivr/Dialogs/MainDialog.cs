// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreBot.Prompts;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly IVRRecognizer _luisRecognizer;
        protected readonly ILogger Logger;

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(IVRRecognizer luisRecognizer, CustomerDialog customerDialog, NearestStoreDialog nearestStoreDialog, OrderStatusDialog orderStatusDialog, EmployeeDialog employeeDialog, PurchaseOrderStatusDialog purchaseOrderStatusDialog, LeavePolicyDialog leavePolicyDialog, ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            _luisRecognizer = luisRecognizer;
            Logger = logger;

            AddDialog(new OrderNumberPrompt());
            AddDialog(new PurchaseOrderNumberPrompt());
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));

            AddDialog(new WaterfallDialog("DTMF", new WaterfallStep[]
            {
                DTMFIntroStepAsync,
                DTMFActStepAsync
            }));

            AddDialog(customerDialog);
            AddDialog(nearestStoreDialog);
            AddDialog(orderStatusDialog);

            AddDialog(employeeDialog);
            AddDialog(purchaseOrderStatusDialog);
            AddDialog(leavePolicyDialog);

            // The initial child Dialog to run.
            InitialDialogId = "DTMF";
        }

        private async Task<DialogTurnResult> DTMFIntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Use the text provided in FinalStepAsync or the default if it is the first time.
            var messageText = stepContext.Options?.ToString() ?? "Thank you for calling. To reach customer service please press or say one. To reach employee resources please press or say two.";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> DTMFActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            switch (stepContext.Context.Activity.Text)
            {
                case "1":
                    var customerResponse = $"Thank you valued customer.";
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(customerResponse, customerResponse), cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(CustomerDialog));
                case "2":
                    var employeeResponse = $"You have selected employee resources.";
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(employeeResponse, employeeResponse), cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(EmployeeDialog));
                default:
                    // Catch all for unhandled intents
                    return await stepContext.ReplaceDialogAsync("DTMF");
            }
        }
    }
}
