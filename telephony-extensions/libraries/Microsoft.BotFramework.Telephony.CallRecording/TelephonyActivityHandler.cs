using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;

namespace Microsoft.Bot.Schema.Telephony
{
    public class TelephonyActivityHandler : ActivityHandler
    {
        protected override Task OnCommandResultActivityAsync(
           ITurnContext<ICommandResultActivity> turnContext,
           CancellationToken cancellationToken)
        {
            switch(turnContext.Activity.Name)
            {
                case TelephonyExtensions.RecordingStart:
                    {
                        TelephonyExtensions.VerifyChannelForCommandResult(TelephonyExtensions.RecordingStart, turnContext);

                        return OnRecordingStartResultAsync(new DelegatingTurnContext<ICommandResultActivity>(turnContext), cancellationToken);
                    }

                case TelephonyExtensions.RecordingPause:
                    {
                        TelephonyExtensions.VerifyChannelForCommandResult(TelephonyExtensions.RecordingPause, turnContext);

                        return OnRecordingPauseResultAsync(new DelegatingTurnContext<ICommandResultActivity>(turnContext), cancellationToken);
                    }

                case TelephonyExtensions.RecordingResume:
                    {
                        TelephonyExtensions.VerifyChannelForCommandResult(TelephonyExtensions.RecordingResume, turnContext);

                        return OnRecordingResumeResultAsync(new DelegatingTurnContext<ICommandResultActivity>(turnContext), cancellationToken);
                    }
            }

            return base.OnCommandResultActivityAsync(turnContext, cancellationToken);
        }

        protected virtual Task OnRecordingStartResultAsync(
            ITurnContext<ICommandResultActivity> turnContext,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnRecordingPauseResultAsync(
            ITurnContext<ICommandResultActivity> turnContext,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnRecordingResumeResultAsync(
            ITurnContext<ICommandResultActivity> turnContext,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
