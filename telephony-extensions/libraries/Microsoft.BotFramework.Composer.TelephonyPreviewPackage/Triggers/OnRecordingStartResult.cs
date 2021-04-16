// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AdaptiveExpressions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Telephony;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.Bot.Components.Telephony.Preview
{
    /// <summary>
    /// Actions triggered when an invoke activity is received for a cards Action.Execute action.
    /// </summary>
    public class OnRecordingStartResult : OnCommandResultActivity
    {
        /// <summary>
        /// Class identifier.
        /// </summary>
        [JsonProperty("$kind")]
        public new const string Kind = "Microsoft.Bot.Components.Telephony.OnRecordingStartResult";

        /// <summary>
        /// Initializes a new instance of the <see cref="OnRecordingStartResult"/> class.
        /// </summary>
        /// <param name="verb">Optional, verb to match on.</param>
        /// <param name="actions">Optional, actions to add to the plan when the rule constraints are met.</param>
        /// <param name="condition">Optional, condition which needs to be met for the actions to be executed.</param>
        /// <param name="callerPath">Optional, source file full path.</param>
        /// <param name="callerLine">Optional, line number in source file.</param>
        [JsonConstructor]
        public OnRecordingStartResult(List<Dialog> actions = null, string condition = null, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(actions: actions, condition: condition, callerPath: callerPath, callerLine: callerLine)
        {
            
        }
        
        /// <inheritdoc/>
        protected override Expression CreateExpression()
        {
            // if name is 'actionableMessage/executeAction'
            return Expression.AndExpression(
                Expression.Parse($"{TurnPath.Activity}.ChannelId == '{TelephonyExtensions.TelephonyChannelId}' && {TurnPath.Activity}.name == '{TelephonyExtensions.RecordingStart}'"), base.CreateExpression());
        }
    }
}
