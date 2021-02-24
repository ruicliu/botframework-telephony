using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Schema.CommandExtensions
{
    /// <summary>
    /// Helpers to convert Activity.Value to Command.Value and CommandResult.Value
    /// </summary>
    public static class CommandExtensions
    {
        /// <summary>
        /// Gets the CommandValue from the activity.
        /// </summary>
        /// <param name="cmdActivity">The command activity</param>
        /// <returns>Gets the CommandValue from the activity</returns>
        public static CommandValue<T> GetCommandValue<T>(this ICommandActivity cmdActivity)
        {
            if (cmdActivity?.Value == null)
            {
                return null;
            }
            else if (cmdActivity.Value is CommandValue<T> commandValue)
            {
                return commandValue;
            }
            else
            {
                return ((JObject)cmdActivity.Value).ToObject<CommandValue<T>>();
            }
        }

        /// <summary>
        /// Gets the CommandResultValue from the activity.
        /// </summary>
        /// <param name="cmdResultActivity">The command result activity</param>
        /// <returns>Gets the CommandResultValue from the activity.</returns>
        public static CommandResultValue<T> GetCommandResultValue<T>(this ICommandResultActivity cmdResultActivity)
        {
            if (cmdResultActivity?.Value == null)
            {
                return null;
            }
            else if (cmdResultActivity.Value is CommandResultValue<T> commandResultValue)
            {
                return commandResultValue;
            }
            else
            {
                return ((JObject)cmdResultActivity.Value).ToObject<CommandResultValue<T>>();
            }
        }
    }
}
