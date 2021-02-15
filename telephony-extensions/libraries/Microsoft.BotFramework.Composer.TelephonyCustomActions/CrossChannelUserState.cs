using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace Microsoft.BotFramework.Composer.TelephonyCustomActions
{
    /// <summary>
    /// Defines a state management object for user state.
    /// </summary>
    /// <remarks>
    /// User state is available in any turn that the bot is conversing with that user on that
    /// channel, regardless of the conversation.
    /// </remarks>
    public class CrossChannelUserState : BotState
    {
        private readonly IStatePropertyAccessor<string> _crossChannelUserIdAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserState"/> class.
        /// </summary>
        /// <param name="storage">The storage layer to use.</param>
        public CrossChannelUserState(IStorage storage, UserState userState)
            : base(storage, nameof(CrossChannelUserState))
        {
            _crossChannelUserIdAccessor = userState.CreateProperty<string>("CrossChannelUserId");
        }

        public override Task LoadAsync(ITurnContext turnContext, bool force = false,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return base.LoadAsync(turnContext, true, cancellationToken);
        }

        public override Task SaveChangesAsync(ITurnContext turnContext, bool force = false,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (GetStorageKey(turnContext) == string.Empty)
            {
                base.DeleteAsync(turnContext, cancellationToken);
            }

            return base.SaveChangesAsync(turnContext, force, cancellationToken);
        }

        /// <summary>
        /// Gets the key to use when reading and writing state to and from storage.
        /// </summary>
        /// <param name="turnContext">The context object for this turn.</param>
        /// <returns>The storage key.</returns>
        /// <remarks>
        /// User state includes the channel ID and user ID as part of its storage key.
        /// </remarks>
        /// <exception cref="ArgumentNullException">The <see cref="ITurnContext.Activity"/> for the
        /// current turn is missing <see cref="Schema.Activity.ChannelId"/> or
        /// <see cref="Schema.Activity.From"/> information, or the sender's
        /// <see cref="ConversationAccount.Id"/> is missing.</exception>
        protected override string GetStorageKey(ITurnContext turnContext)
        {
            string crossChannelUserId = null;

            //try
            //{
                crossChannelUserId = _crossChannelUserIdAccessor.GetAsync(turnContext).Result;
            //}
            //catch
            //{
            //    throw new InvalidOperationException("Cross Channel User State Not Configured");
            //}

            return crossChannelUserId ?? string.Empty;
        }
    }
}
