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
using Microsoft.Bot.Builder;

namespace SkillSample.Dialogs
{
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
                EndAsync,
            };

            AddDialog(new WaterfallDialog(nameof(SerialNumPostProcessAction), sample));
            AddDialog(new TextPrompt(DialogIds.SerialNumPrompt));
            AddDialog(new NumberPrompt<int>(DialogIds.SerialNumConfirmationPrompt, SerialNumValidation));

            InitialDialogId = nameof(SerialNumPostProcessAction);
        }

        private async Task<DialogTurnResult> PromptForSerialNumAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(DialogIds.SerialNumPrompt, new PromptOptions
            {
                Prompt = MessageFactory.Text("What's your serial number?"),
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> PostProcessSerialNumber(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // e.g. input: ONE CR 00703 F 3, after post processing: 1CR00703F3
            var possiblePostProcessedSerialNums = TestHPSerialNumber(stepContext.Result.ToString().ToUpper());

            // Create an object that persists the user's post processed serial number(s) within the dialog
            stepContext.Values["SerialNums"] = possiblePostProcessedSerialNums;

            if (stepContext.Values["SerialNums"] == null)
            {
                // Pass int.MaxValue which is the flag telling the next step that we could not process serial number
                return await stepContext.NextAsync(int.MaxValue, cancellationToken: cancellationToken);
            }

            var finalOptionNum = possiblePostProcessedSerialNums.Length + 1;
            await stepContext.Context.SendActivityAsync("There are " + finalOptionNum + " options to choose from");
            for (int i = 0; i < possiblePostProcessedSerialNums.Length; i++)
            {
                var curOptionNum = i + 1;
                await stepContext.Context.SendActivityAsync("Option " + curOptionNum + " is " + possiblePostProcessedSerialNums[i]);
            }

            await stepContext.Context.SendActivityAsync("Final Option is " + finalOptionNum + " and it means that none of the above serial number is correct");

            return await stepContext.PromptAsync(DialogIds.SerialNumConfirmationPrompt, new PromptOptions
            {
                Prompt = MessageFactory.Text("Please tell us the numeric number option by saying the single digit number ranging from 1 to " + finalOptionNum),
                RetryPrompt = MessageFactory.Text("Please retry")
            }, cancellationToken);
        }

        private Task<bool> SerialNumValidation(PromptValidatorContext<int> promptContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        private async Task<DialogTurnResult> EndAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Logic to figure out which serial number the user has chosen
            var chosenSerialNum = "Sorry we could not correctly process your serial number.";
            var possibleSerialNums = (string[])stepContext.Values["SerialNums"];

            var userInputOption = (int)stepContext.Result;

            if (possibleSerialNums != null && userInputOption <= possibleSerialNums.Length && userInputOption > 0)
            {
                chosenSerialNum = possibleSerialNums[userInputOption - 1];
            }

            // Simulate a response object payload
            var actionResponse = new PostProcessedSerialNumOutput
            {
                PostProcessedSerialNum = chosenSerialNum
            };

            if (chosenSerialNum.Contains("Sorry"))
            {
                // tell C2 that we weren't able to properly process the serial number
                await stepContext.Context.SendActivityAsync(chosenSerialNum);
            }
            else
            {
                await stepContext.Context.SendActivityAsync("I got that your serial number is " + chosenSerialNum);
            }

            // We end the dialog (generating an EndOfConversation event) which will serialize the result object in the Value field of the Activity
            return await stepContext.EndDialogAsync(actionResponse, cancellationToken);
        }

        private static string[] TestHPSerialNumber(string? input = null)
        {
            var groups = new List<SerialNumberTextGroup>();
            var g1 = new SerialNumberTextGroup
            {
                AcceptsDigits = true,
                AcceptsAlphabet = true,
                LengthInChars = 10,
            };
            groups.Add(g1);

            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            var pattern = new SerialNumberPattern(groups.AsReadOnly()); // can also customize the pattern by inputting a string regex
            var result = pattern.Inference(input);

            if (result == null || result.Length == 0)
            {
                return null;
            }

            return result;
        }

        private static class DialogIds
        {
            public const string SerialNumPrompt = "serialNumPrompt";
            public const string SerialNumConfirmationPrompt = "Choose valid serial numbers";
        }
    }
}