using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema.CommandExtensions;

namespace Microsoft.Bot.Schema.Telephony
{
    public class TelephonyActivityHandler : ActivityHandlerWithCommands
    {
        public override Task OnCommandResultActivityAsync(
           ITurnContext<IActivity> turnContext,
           CancellationToken cancellationToken)
        {
            if (((Activity)turnContext.Activity).Name == TelephonyExtensions.RecordingStart)
            {
                TelephonyExtensions.VerifyChannelForCommandResult(TelephonyExtensions.RecordingStart, turnContext);

                return OnRecordingStartResultAsync(new DelegatingTurnContext<IActivity>(turnContext), cancellationToken);
            }
            else if (((Activity)turnContext.Activity).Name == TelephonyExtensions.RecordingPause)
            {
                TelephonyExtensions.VerifyChannelForCommandResult(TelephonyExtensions.RecordingPause, turnContext);

                return OnRecordingPauseResultAsync(new DelegatingTurnContext<IActivity>(turnContext), cancellationToken);
            }
            else if (((Activity)turnContext.Activity).Name == TelephonyExtensions.RecordingResume)
            {
                TelephonyExtensions.VerifyChannelForCommandResult(TelephonyExtensions.RecordingResume, turnContext);

                return OnRecordingResumeResultAsync(new DelegatingTurnContext<IActivity>(turnContext), cancellationToken);
            }

            return base.OnCommandResultActivityAsync(turnContext, cancellationToken);
        }

        protected virtual Task OnRecordingStartResultAsync(
            ITurnContext<IActivity> turnContext,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnRecordingPauseResultAsync(
            ITurnContext<IActivity> turnContext,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnRecordingResumeResultAsync(
            ITurnContext<IActivity> turnContext,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
