// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;
using SpeechAlphanumericPostProcessing;

namespace SkillSample.Dialogs
{
    public class PostProcessedSerialNumOutput
    {
        [JsonProperty("postProcessedSerialNum")]
        public string PostProcessedSerialNum { get; set; }

        public PostProcessedSerialNumOutput(string output)
        {
            PostProcessedSerialNum = output;
        }
    }

    public class SerialNumPostProcessAction : SkillDialogBase
    {
        protected const string AggregationDialogMemory = "aggregation";

        private readonly AlphaNumericTextGroup g1 = new AlphaNumericTextGroup(true, true, 10);

        private readonly List<AlphaNumericTextGroup> groups = new List<AlphaNumericTextGroup>();

        private readonly AlphaNumericSequencePostProcessor snp;

        public SerialNumPostProcessAction(
            IServiceProvider serviceProvider)
            : base(nameof(SerialNumPostProcessAction), serviceProvider)
        {
            var sample = new WaterfallStep[]
            {
                PromptForSerialNumAsync,
            };

            AddDialog(new WaterfallDialog(nameof(SerialNumPostProcessAction), sample));
            AddDialog(new TextPrompt(DialogIds.SerialNumPrompt));

            InitialDialogId = nameof(SerialNumPostProcessAction);
            groups.Add(g1);
            snp = new AlphaNumericSequencePostProcessor(groups.AsReadOnly(), true);
        }

        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default)
        {
            if (dc.ActiveDialog.State.ContainsKey("this.ambiguousChoices"))
            {
                string[] choices = (string[])dc.ActiveDialog.State["this.ambiguousChoices"];
                bool isAmbiguousPrompt = choices != null && choices.Length >= 2;
                if (isAmbiguousPrompt)
                {
                    dc.ActiveDialog.State["this.ambiguousChoices"] = null;
                    string choice = dc.Context.Activity.Text;
                    string result = string.Empty;
                    switch (choice)
                    {
                        case "1":
                            result = choices[0];
                            break;
                        case "2":
                            result = choices[1];
                            break;
                        default:
                            await dc.Context.SendActivityAsync("Sorry we could not process your input");
                            return await dc.EndDialogAsync(new PostProcessedSerialNumOutput("Sorry"), cancellationToken);
                    }

                    if (snp.PatternLength == result.Length)
                    {
                        // End this dialog
                        await dc.Context.SendActivityAsync($"I got that your serial number is {result}");
                        return await dc.EndDialogAsync(new PostProcessedSerialNumOutput(result), cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await dc.Context.SendActivityAsync("Sorry we could not process your input");
                        return await dc.EndDialogAsync(new PostProcessedSerialNumOutput("Sorry"), cancellationToken);
                    }
                }
            }

            // append the message to the aggregation memory state
            var existingAggregation = dc.ActiveDialog.State.ContainsKey(AggregationDialogMemory) == true ? dc.ActiveDialog.State[AggregationDialogMemory].ToString() : string.Empty;
            existingAggregation += dc.Context.Activity.Text;

            string[] results = snp.Inference(existingAggregation.ToUpper());

            // Is the current aggregated message the termination string?
            if (results.Length == 1)
            {
                if (snp.PatternLength == results[0].Length)
                {
                    // End the dialog
                    await dc.Context.SendActivityAsync($"I got that your serial number is {results[0]}");
                    return await dc.EndDialogAsync(new PostProcessedSerialNumOutput(results[0]), cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    dc.ActiveDialog.State[AggregationDialogMemory] = results[0];
                    string promptMsg = "Please continue with next letter or digit";
                    await dc.Context.SendActivityAsync(promptMsg, promptMsg).ConfigureAwait(false);
                    return new DialogTurnResult(DialogTurnStatus.Waiting);
                }
            }
            else if (results.Length == 2)
            {
                if (results[0].Length == snp.PatternLength && results[1].Length == snp.PatternLength)
                {
                    dc.ActiveDialog.State["this.ambiguousChoices"] = results;
                    string promptMsg = "Say or type 1 for " + results[0] + " or 2 for " + results[1];
                    await dc.Context.SendActivityAsync(promptMsg, promptMsg).ConfigureAwait(false);
                    return new DialogTurnResult(DialogTurnStatus.Waiting);
                }
                else
                {
                    // else, save the updated aggregation and end the turn
                    // space is needed at the end to help us separate any substitutions from the input of the next turn
                    dc.ActiveDialog.State[AggregationDialogMemory] = existingAggregation + " ";
                    string promptMsg = "Please continue with next letter or digit";
                    await dc.Context.SendActivityAsync(promptMsg, promptMsg).ConfigureAwait(false);
                    return new DialogTurnResult(DialogTurnStatus.Waiting);
                }
            }
            else
            {
                // for the case where result is empty
                await dc.Context.SendActivityAsync("Sorry we could not process your input");
                return await dc.EndDialogAsync(new PostProcessedSerialNumOutput("Sorry"), cancellationToken);
            }
        }

        private async Task<DialogTurnResult> PromptForSerialNumAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(DialogIds.SerialNumPrompt, new PromptOptions
            {
                Prompt = MessageFactory.Text("What's your serial number?"),
            }, cancellationToken);
        }

        private static class DialogIds
        {
            public const string SerialNumPrompt = "serialNumPrompt";
        }
    }
}