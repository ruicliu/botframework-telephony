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
    public class CustomerDialog : WaterfallDialog
    {
        private readonly IVRRecognizer _luisRecognizer;
        protected readonly ILogger Logger;

        // Dependency injection uses this constructor to instantiate MainDialog
        public CustomerDialog(IVRRecognizer luisRecognizer, ILogger<MainDialog> logger)
            : base(nameof(CustomerDialog))
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
            var messageText = stepContext.Options?.ToString() ?? "Would you like to find a store near you or ask about the status of an order?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Call LUIS and gather any potential booking details. (Note the TurnContext has the response to the prompt.)
            var luisResult = await _luisRecognizer.RecognizeAsync<IVR>(stepContext.Context, cancellationToken);
            switch (luisResult.TopIntent().intent)
            {
                case IVR.Intent.FindNearestStore:
                    //Get Luis model and convert it to an easier to work with viewmodel
                    var nearestStoreResult = luisResult.Entities.ToObject<FindNearestStoreModel>();
                    var nearestStoreViewModel = new StoreLocationViewModel();

                    if(nearestStoreResult.StoreLocation != null && nearestStoreResult.StoreLocation.Length > 0)
                    {
                        var storeLocation = nearestStoreResult.StoreLocation.First();
                        if(storeLocation.City.Length > 0)
                        {
                            nearestStoreViewModel.City = storeLocation.City.First();
                        }
                        if (storeLocation.State.Length > 0)
                        {
                            nearestStoreViewModel.State = storeLocation.State.First();
                        }
                    }

                    return await stepContext.ReplaceDialogAsync(nameof(NearestStoreDialog), nearestStoreViewModel);
                case IVR.Intent.OrderStatus:
                    var orderStatusResult = luisResult.Entities.ToObject<OrderStatusModel>();
                    var orderStatusViewModel = new OrderStatusViewModel();
                    
                    if(orderStatusResult.OrderNumber != null && orderStatusResult.OrderNumber.Length > 0)
                    {
                        orderStatusViewModel.OrderNumber = orderStatusResult.OrderNumber.First();
                    }

                    return await stepContext.ReplaceDialogAsync(nameof(OrderStatusDialog), orderStatusViewModel);
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
            return await stepContext.ReplaceDialogAsync(nameof(CustomerDialog), promptMessage, cancellationToken);
        }
    }
}
