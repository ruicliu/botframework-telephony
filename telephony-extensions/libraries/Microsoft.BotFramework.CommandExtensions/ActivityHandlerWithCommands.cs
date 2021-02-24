using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;

namespace Microsoft.Bot.Schema.CommandExtensions
{
    /// <summary>
    /// ActivityHandlerWithCommands will be merged into ActivityHandler in SDK
    /// </summary>
    public class ActivityHandlerWithCommands : ActivityHandler
    {
        public override Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            if (turnContext.Activity.Type == ActivityTypesWithCommand.Command)
            {
                return OnCommandActivityAsync(new DelegatingTurnContext<ICommandActivity>(turnContext), cancellationToken);
            }
            else if (turnContext.Activity.Type == ActivityTypesWithCommand.CommandResult)
            {
                return OnCommandResultActivityAsync(new DelegatingTurnContext<ICommandResultActivity>(turnContext), cancellationToken);
            }

            return base.OnTurnAsync(turnContext, cancellationToken);
        }

        public virtual Task OnCommandActivityAsync(ITurnContext<ICommandActivity> turnContext, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnCommandResultActivityAsync(ITurnContext<ICommandResultActivity> turnContext, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
