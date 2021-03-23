using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Telephony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class RecordingHelpers
    {
        public static async Task<ResourceResponse> TryStartRecording(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Activity.ChannelId == "telephony")
            {
                await turnContext.StartRecording(cancellationToken).ConfigureAwait(false);
            }

            return null;
        }

        public static async Task<ResourceResponse> TryPauseRecording(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Activity.ChannelId == "telephony")
            {
                await turnContext.PauseRecording(cancellationToken).ConfigureAwait(false);
            }

            return null;
        }

        public static async Task<ResourceResponse> TryResumeRecording(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Activity.ChannelId == "telephony")
            {
                await turnContext.ResumeRecording(cancellationToken).ConfigureAwait(false);
            }

            return null;
        }
    }
}
