// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Memory;
using Microsoft.Bot.Builder.Dialogs.Memory.Scopes;

namespace Microsoft.BotFramework.Composer.TelephonyCustomActions
{
    public class CustomComponentMemoryScopesRegistration : ComponentRegistration, IComponentMemoryScopes
    {
        public IEnumerable<MemoryScope> GetMemoryScopes()
        {
            yield return new CrossChannelUserMemoryScope();
        }
    }
}