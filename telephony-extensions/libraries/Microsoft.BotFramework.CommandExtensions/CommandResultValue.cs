using System;
namespace Microsoft.Bot.Schema.CommandExtensions
{
    /// <summary>
    /// Value schema of an ICommandResultActivity
    /// </summary>
    public class CommandResultValue<T>
    {
        /// <summary>
        /// Sender generated id of the original command
        /// </summary>
        public string CommandId { get; set; }

        /// <summary>
        /// Optional error if the command result indicates a failure
        /// </summary>
        public Error Error { get; set; }

        /// <summary>
        /// Open-ended data
        /// </summary>
        public T Data { get; set; }
    }
}
