using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Templates;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;

namespace MultiTurnPromptBot.Dialogs
{
    public class BatchNumericInput : Dialog
    {
        [JsonConstructor]
        public BatchNumericInput([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            : base()
        {
            // enable instances of this command as debug break point
            this.RegisterSourceLocation(sourceFilePath, sourceLineNumber);
        }
        [JsonProperty("prompt")]
        public String Prompt { get; set; }

        [JsonProperty("property")]
        public StringExpression Property { get; set; }

        /// <summary>
        /// Complete input collection when encountering this symbol
        /// </summary>
        [JsonProperty("condition")]
        public String TerminatingSymbol { get; set; }

        /// <summary>
        /// Complete input collection when the input length reaches this number
        /// </summary>
        [JsonProperty("maxLen")]
        public int MaxLen { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object dialogOptions = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!string.IsNullOrEmpty(Prompt))
            {
                var activity = MessageFactory.Text(Prompt);
                await dc.Context.SendActivityAsync(activity);
            }
            return EndOfTurn;
        }

        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = new CancellationToken())
        {
            var current = dc.State.GetStringValue("dialog._current_dtmf", string.Empty);

            if (dc.Context.Activity.Text == TerminatingSymbol || current.Length >= MaxLen)
            {
                string path = Property.GetValue(dc.State);
                if (path == string.Empty)
                {
                    throw new InvalidOperationException("Unable to save the DTMF value, incorrect ResultProperty expression");
                }
                else
                {
                    dc.State.SetValue(path, current);
                    return await dc.EndDialogAsync(result: current, cancellationToken: cancellationToken);
                }
            }

            // TODO: translate spoken numbers into digits

            if (Regex.IsMatch(dc.Context.Activity.Text, "^[0123456789]{1}$"))
            {
                dc.State.SetValue("dialog._current_dtmf", $"{current}{dc.Context.Activity.Text}");
            }

            return EndOfTurn;
        }
    }

}
