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

namespace Microsoft.BotFramework.Composer.TelephonyCustomActions.Action
{
    public class BatchDtmfAction : Dialog
    {
        [JsonConstructor]
        public BatchDtmfAction([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            : base()
        {
            // enable instances of this command as debug break point
            this.RegisterSourceLocation(sourceFilePath, sourceLineNumber);
        }

        [JsonProperty("$kind")]
        public const string Kind = "BatchDtmfAction";

        [JsonProperty("resultProperty")]
        public StringExpression ResultProperty { get; set; }

        [JsonProperty("completeDTMFChar")]
        public StringExpression CompleteDtmfChar { get; set; }

        [JsonProperty("activity")]
        public ITemplate<Activity> Activity { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object dialogOptions = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var activity = await Activity.BindAsync(dc, dc.State).ConfigureAwait(false);
            await dc.Context.SendActivityAsync(activity);
            return EndOfTurn;
        }

        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = new CancellationToken())
        {
            var current = dc.State.GetStringValue("dialog.current", string.Empty);

            if (dc.Context.Activity.Text == CompleteDtmfChar.GetValue(dc.State))
            {
                string path = ResultProperty.GetValue(dc.State);
                if (path == string.Empty)
                {
                    throw new InvalidOperationException("Unable to save the DTMF value, incorrect Result expression");
                }
                else
                {
                    dc.State.SetValue(path, current);
                    return await dc.EndDialogAsync(result: current, cancellationToken: cancellationToken);
                }
            }

            if (Regex.IsMatch(dc.Context.Activity.Text, "^[0123456789]{1}$"))
            {
                dc.State.SetValue("dialog.current", $"{current}{dc.Context.Activity.Text}");
            }

            return EndOfTurn;
        }
    }
}
