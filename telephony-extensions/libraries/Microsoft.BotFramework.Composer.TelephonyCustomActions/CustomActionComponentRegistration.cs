// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using AdaptiveExpressions.Converters;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.BotFramework.Composer.TelephonyCustomActions.Action;
using Newtonsoft.Json;

namespace Microsoft.BotFramework.Composer.TelephonyCustomActions
{
    public class CustomActionComponentRegistration : ComponentRegistration, IComponentDeclarativeTypes
    {
        public IEnumerable<DeclarativeType> GetDeclarativeTypes(ResourceExplorer resourceExplorer)
        {
            // Actions
            yield return new DeclarativeType<BatchDtmfAction>(BatchDtmfAction.Kind);
            yield return new DeclarativeType<ContinueWithSmsDialog>(ContinueWithSmsDialog.Kind);
        }

        public IEnumerable<JsonConverter> GetConverters(ResourceExplorer resourceExplorer, SourceContext sourceContext)
        {
            yield return new ObjectExpressionConverter<BatchDtmfAction>();
        }
    }
}