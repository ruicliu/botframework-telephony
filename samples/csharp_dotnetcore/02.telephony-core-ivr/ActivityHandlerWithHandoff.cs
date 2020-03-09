using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreBot
{
    /// <summary>
    /// ActivityHandlerWithHandoff will be merged into ActivityHandler in SDK 4.6
    /// </summary>
    public class ActivityHandlerWithHandoff : ActivityHandler
    {
        public override Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            if (turnContext.Activity.Type == ActivityTypes.Handoff)
            {
                return OnHandoffActivityAsync(new DelegatingTurnContext<IHandoffActivity>(turnContext), cancellationToken);
            }

            return base.OnTurnAsync(turnContext, cancellationToken);
        }

        protected virtual Task OnHandoffActivityAsync(ITurnContext<IHandoffActivity> turnContext, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
