using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema.CommandExtensions;

namespace Microsoft.Bot.Schema.Telephony
{
    public class TelephonyActivityHandler : ActivityHandlerWithCommands
    {
        public override Task OnCommandResultActivityAsync(
           ITurnContext<ICommandResultActivity> turnContext,
           CancellationToken cancellationToken)
        {
            if (turnContext.Activity.Name == TelephonyExtensions.RecordingStart)
            {
                return OnRecordingStartResultAsync(new DelegatingTurnContext<ICommandResultActivity>(turnContext), cancellationToken);
            }

            return base.OnCommandResultActivityAsync(turnContext, cancellationToken);
        }

        protected virtual Task OnRecordingStartResultAsync(
            ITurnContext<ICommandResultActivity> turnContext,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
