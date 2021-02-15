using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Memory.Scopes;

namespace Microsoft.BotFramework.Composer.TelephonyCustomActions
{
    public class CrossChannelUserMemoryScope : BotStateMemoryScope<CrossChannelUserState>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CrossChannelUserMemoryScope"/> class.
        /// </summary>
        public CrossChannelUserMemoryScope()
            : base("crosschanneluser")
        {
        }
    }
}
