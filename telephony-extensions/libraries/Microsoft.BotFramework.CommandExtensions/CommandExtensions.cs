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
        public static CommandValue<T> GetCommandValue<T>(this IActivity activity)
        {
            object value = ((Activity)activity)?.Value;

            if (value == null)
            {
                return null;
            }
            else if (value is CommandValue<T> commandValue)
            {
                return commandValue;
            }
            else
            {
                return ((JObject)value).ToObject<CommandValue<T>>();
            }
        }

        /// <summary>
        /// Gets the CommandResultValue from the activity.
        /// </summary>
        /// <param name="cmdResultActivity">The command result activity</param>
        /// <returns>Gets the CommandResultValue from the activity.</returns>
        public static CommandResultValue<T> GetCommandResultValue<T>(this IActivity activity)
        {
            object value = ((Activity)activity)?.Value;

            if (value == null)
            {
                return null;
            }
            else if (value is CommandResultValue<T> commandResultValue)
            {
                return commandResultValue;
            }
            else
            {
                return ((JObject)value).ToObject<CommandResultValue<T>>();
            }
        }
    }
}
