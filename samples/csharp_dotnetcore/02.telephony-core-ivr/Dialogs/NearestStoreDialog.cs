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

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class NearestStoreDialog : WaterfallDialog
    {
        private const string StateStepMsgText = "What state are you in?";
        private const string CityStepMsgText = "What city are you in?";
        public NearestStoreDialog(): base(nameof(NearestStoreDialog))
        {
            AddStep(StateStepAsync);
            AddStep(CityStepAsync);
            AddStep(FinalStepAsync);
        }

        private async Task<DialogTurnResult> StateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var locationDetails = (StoreLocationViewModel)stepContext.Options;

            if (locationDetails.State == null)
            {
                var promptMessage = MessageFactory.Text(StateStepMsgText, StateStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(locationDetails.State, cancellationToken);
        }

        private async Task<DialogTurnResult> CityStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var locationDetails = (StoreLocationViewModel)stepContext.Options;

            locationDetails.State = (string)stepContext.Result;

            if (locationDetails.City == null)
            {
                var promptMessage = MessageFactory.Text(CityStepMsgText, CityStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(locationDetails.City, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var locationDetails = (StoreLocationViewModel)stepContext.Options;

            locationDetails.City = (string)stepContext.Result;

            var response = $"The nearest store to {locationDetails.City}, {locationDetails.State} is at 123 Example Rd.";
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(response, response, InputHints.IgnoringInput), cancellationToken);
            // Restart the main dialog with a different message the second time around
            var promptMessage = "What else can I do for you?";
            return await stepContext.ReplaceDialogAsync(nameof(CustomerDialog), promptMessage, cancellationToken);
        }
    }
}
