// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Bot.Builder.Community.Adapters.ACS.SMS;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Microsoft.BotFramework.Composer.TelephonyCustomActions.Action
{
    /// <summary>
    /// Action which begins executing another dialog, when it is done, it will return to the caller.
    /// </summary>
    public class ContinueWithSmsDialog : Dialog
    {
        /// <summary>
        /// Class identifier.
        /// </summary>
        [JsonProperty("$kind")]
        public const string Kind = "Microsoft.ContinueWithSmsDialog";

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinueWithSmsDialog"/> class.
        /// </summary>
        /// <param name="sourceFilePath">Optional, source file full path.</param>
        /// <param name="sourceLineNumber">Optional, line number in source file.</param>
        [JsonConstructor]
        public ContinueWithSmsDialog([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            // enable instances of this command as debug break point
            RegisterSourceLocation(sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Gets or sets an optional expression which if is true will disable this action.
        /// </summary>
        /// <example>
        /// "user.age > 18".
        /// </example>
        /// <value>
        /// A boolean expression. 
        /// </value>
        [JsonProperty("disabled")]
        public BoolExpression Disabled { get; set; }

        [JsonProperty("smsNumber")]
        public StringExpression SmsNumber { get; set; }

        [JsonProperty("problem")]
        public StringExpression Problem { get; set; }

        /// <summary>
        /// Gets or sets the property path to store the dialog result in.
        /// </summary>
        /// <value>
        /// The property path to store the dialog result in.
        /// </value>
        [JsonProperty("resultProperty")]
        public StringExpression ResultProperty { get; set; }

        /// <summary>
        /// Called when the dialog is started and pushed onto the dialog stack.
        /// </summary>
        /// <param name="dc">The <see cref="DialogContext"/> for the current turn of conversation.</param>
        /// <param name="options">Optional, initial information to pass to the dialog.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Pull some values we stored in turnState so we can continue the conversation on the SMS adapter
            // Note: this is a bit hacky, we may need to find a more elegant way of doing this.
            var acsSmsAdapter = dc.Context.TurnState.Get<AcsSmsAdapter>();
            var leBot = dc.Context.TurnState.Get<IBot>();
            var convoState = dc.Context.TurnState.Get<ConversationState>();
            var config = dc.Context.TurnState.Get<IConfiguration>();

            var smsNumber = SmsNumber.GetValue(dc.State);
            // Manually create a conversation reference for ACS_SMS so we can proactively start a conversation there.
            var smsCref = new ConversationReference
            {
                User = new ChannelAccount(smsNumber),
                Bot = new ChannelAccount(config.GetSection("acsSmsAdapterSettings")["AcsPhoneNumber"], "bot"),
                Conversation = new ConversationAccount(false, null, smsNumber),
                ChannelId = "ACS_SMS"
            };

            // Capture the problem so we can send it in the continue conversation event.
            var problem = Problem.GetValue(dc.State); 
            async Task BotCallback(ITurnContext context, CancellationToken ct)
            {
                // Set the problem in value property of the continuation activity.
                context.Activity.Value = problem;
                await leBot.OnTurnAsync(context, ct);
            }

            // send proactive message with convRef to start SMS
            await acsSmsAdapter.ContinueConversationAsync(config["MicrosoftAppId"], smsCref, BotCallback, cancellationToken);

            // End the action. 
            return await dc.EndDialogAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}