// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Bot.Builder.Community.Adapters.ACS.SMS;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.Skills;
using Microsoft.BotFramework.Composer.Core;
using Microsoft.Extensions.Configuration;

namespace Microsoft.BotFramework.Composer.WebApp.Bots
{
    public class ComposerBotWithSms : ComposerBot
    {
        private readonly AcsSmsAdapter _acsSmsAdapter;
        private IConfiguration _configuration;

        public ComposerBotWithSms(ConversationState conversationState, UserState userState, ResourceExplorer resourceExplorer, BotFrameworkClient skillClient, SkillConversationIdFactoryBase conversationIdFactory, IBotTelemetryClient telemetryClient, string rootDialog, string defaultLocale, AcsSmsAdapter acsSmsAdapter, IConfiguration configuration, bool removeRecipientMention = false)
            : base(conversationState, userState, resourceExplorer, skillClient, conversationIdFactory, telemetryClient, rootDialog, defaultLocale, removeRecipientMention)
        {
            _acsSmsAdapter = acsSmsAdapter;
            _configuration = configuration;
        }

        public override Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            // HACK, making some global objects available in turnState to be used by the ContinueWithSmsDialog
            turnContext.TurnState.Add(_acsSmsAdapter);
            turnContext.TurnState.Add<IBot>(this);
            return base.OnTurnAsync(turnContext, cancellationToken);
        }
    }
}