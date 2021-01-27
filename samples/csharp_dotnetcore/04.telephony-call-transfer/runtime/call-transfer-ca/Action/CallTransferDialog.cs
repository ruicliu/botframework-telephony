// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;
using TransferToWebChat.CustomAction;

namespace Microsoft.BotFramework.Composer.CustomAction
{
    /// <summary>
    /// Custom command which takes takes 2 data bound arguments (arg1 and arg2) and multiplies them returning that as a databound result.
    /// </summary>
    public class CallTransferDialog : Dialog
    {
        [JsonConstructor]
        public CallTransferDialog([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            : base()
        {
            // enable instances of this command as debug break point
            this.RegisterSourceLocation(sourceFilePath, sourceLineNumber);
        }

        [JsonProperty("$kind")]
        public const string Kind = "CallTransfer";

        [JsonProperty("targetPhoneNumber")]
        public StringExpression TargetPhoneNumber { get; set; }

        public async override Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var targetPhoneNumber = TargetPhoneNumber.GetValue(dc.State);

            await dc.Context.SendActivityAsync($"Transferring to \"{targetPhoneNumber}\"...");

            await Task.Delay(5000); // Temporary hack to make sure message is done reading out loud before transfer starts. Bug is tracked to fix this issue.

            // Create handoff event, passing the phone number to transfer to as context.
            var poContext = new { TargetPhoneNumber = targetPhoneNumber };
            var poHandoffEvent = EventFactory.CreateHandoffInitiation(dc.Context, poContext);

            try
            {
                await dc.Context.SendActivityAsync(poHandoffEvent, cancellationToken);
                await dc.Context.SendActivityAsync($"Call transfer initiation succeeded");
            }
            catch
            {
                await dc.Context.SendActivityAsync($"Call transfer failed");
            }

            return await dc.EndDialogAsync(result: 0, cancellationToken: cancellationToken);
        }
    }
}
