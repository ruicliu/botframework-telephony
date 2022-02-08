// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;
using SpeechSerialNumber;
using System.Collections.Generic;

namespace SkillSample.Dialogs
{
    public class PreProcessedSerialNumInput
    {
        [JsonProperty("preProcessedSerialNum")]
        public string PreProcessedSerialNum { get; set; }
    }

    public class PostProcessedSerialNumOutput
    {
        [JsonProperty("postProcessedSerialNum")]
        public string PostProcessedSerialNum { get; set; }
    }

    public class SerialNumPostProcessAction : SkillDialogBase
    {
        public SerialNumPostProcessAction(
            IServiceProvider serviceProvider)
            : base(nameof(SerialNumPostProcessAction), serviceProvider)
        {
            var sample = new WaterfallStep[]
            {
                PromptForSerialNumAsync,
                PostProcessSerialNumber,
            };

            AddDialog(new WaterfallDialog(nameof(SerialNumPostProcessAction), sample));

            InitialDialogId = nameof(SerialNumPostProcessAction);
        }

        private async Task<DialogTurnResult> PostProcessSerialNumber(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // e.g. input: ONE CR 00703 F 3, after post processing: 1CR00703F3
            var response = TestHPSerialNumber(stepContext.Result.ToString().ToUpper());
            if (response == null)
            {
                response = "Oops, we could not do post-processing based on the input.";
            }
            else
            {
                response = "The serial number after post-processing is " + response;
            }

            await stepContext.Context.SendActivityAsync(response);

            // Simulate a response object payload
            var actionResponse = new PostProcessedSerialNumOutput
            {
                PostProcessedSerialNum = response
            };

            // We end the dialog (generating an EndOfConversation event) which will serialize the result object in the Value field of the Activity
            return await stepContext.EndDialogAsync(actionResponse, cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForSerialNumAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // If we have been provided a input data structure we pull out provided data as appropriate
            // and make a decision on whether the dialog needs to prompt for anything.
            if (stepContext.Options is PreProcessedSerialNumInput actionInput && !string.IsNullOrEmpty(actionInput.PreProcessedSerialNum))
            {
                // We have Name provided by the caller so we skip the Name prompt.
                return await stepContext.NextAsync(actionInput.PreProcessedSerialNum, cancellationToken);
            }

            var prompt = TemplateEngine.GenerateActivityForLocale("SerialNumPrompt");
            return await stepContext.PromptAsync(DialogIds.SerialNumPrompt, new PromptOptions { Prompt = prompt }, cancellationToken);
        }

        private static class DialogIds
        {
            public const string SerialNumPrompt = "serialNumPrompt";
        }

        private static string TestHPSerialNumber(string? input = null)
        {
            var groups = new List<TextGroup>();
            var g1 = new TextGroup
            {
                AcceptsDigits = true,
                AcceptsAlphabet = true,
                LengthInChars = 10,
            };
            groups.Add(g1);

            if (string.IsNullOrWhiteSpace(input))
            {
                input = "ONE CR 00703 F 3.";
            }

            var pattern = new SerialNumberPattern(groups.AsReadOnly(), input);
            var result = pattern.Inference();
            if (result == null || result.Length == 0)
            {
                return null;
            }

            // TODO: need to add re-prompting to allow C2 to choose the correct output
            return result[0];
        }
    }
}