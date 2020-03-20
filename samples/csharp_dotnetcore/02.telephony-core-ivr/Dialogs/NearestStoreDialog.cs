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
        private readonly VoiceFactory VoiceFactory;
        private const string StateStepMsgText = "What state are you in?";
        private const string CityStepMsgText = "What city are you in?";
        public NearestStoreDialog(VoiceFactory voiceFactory): base(nameof(NearestStoreDialog))
        {
            VoiceFactory = voiceFactory;

            AddStep(StateStepAsync);
            AddStep(CityStepAsync);
            AddStep(FinalStepAsync);
        }

        private async Task<DialogTurnResult> StateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var locationDetails = (StoreLocationViewModel)stepContext.Options;

            if (locationDetails.State == null)
            {
                //Build and send our prompt if the viewmodel is not populated
                var promptMessage = VoiceFactory.TextAndVoice(StateStepMsgText, InputHints.ExpectingInput);
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
                //Build and send the next prompt if the viewmodel is not populated
                var promptMessage = VoiceFactory.TextAndVoice(CityStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(locationDetails.City, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var locationDetails = (StoreLocationViewModel)stepContext.Options;

            locationDetails.City = (string)stepContext.Result;

            var response = $"The nearest store to {locationDetails.City}, {locationDetails.State} is at 16600 NE 1st Avenue";
            await stepContext.Context.SendActivityAsync(VoiceFactory.TextAndVoice(response, InputHints.IgnoringInput), cancellationToken);

            // Restart the upstream dialog with a different message the second time around
            var promptMessage = "What else can I do for you?";
            return await stepContext.ReplaceDialogAsync(nameof(CustomerDialog), promptMessage, cancellationToken);
        }
    }
}
