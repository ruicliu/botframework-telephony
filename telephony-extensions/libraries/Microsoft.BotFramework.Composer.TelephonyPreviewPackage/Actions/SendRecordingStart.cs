using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Telephony;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;

namespace Microsoft.Bot.Components.Telephony.Preview
{
    public class SendRecordingStart : Dialog
    {
        public static string Kind = "Microsoft.Bot.Components.Telephony.Preview.StartCallRecording";

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var response = await TelephonyExtensions.TryRecordingStart(dc.Context, cancellationToken).ConfigureAwait(false);

            // Get activity ID (if there is one)
            var activityId = response != null && !string.IsNullOrEmpty(response.Id) ? response.Id : string.Empty;

            // Save actvity ID to memory
            var idProperty = ActivityIdProperty?.GetValue(dc.State);
            if (!string.IsNullOrEmpty(idProperty))
            {
                dc.State.SetValue(idProperty, activityId);
            }

            return await dc.EndDialogAsync(activityId, cancellationToken).ConfigureAwait(false);
        }

        [JsonProperty("activityIdProperty")]
        public StringExpression ActivityIdProperty { get; set; } = string.Empty;
    }
}
