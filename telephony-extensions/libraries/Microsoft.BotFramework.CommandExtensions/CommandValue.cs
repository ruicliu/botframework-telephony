using System;
using System.Collections.Generic;
namespace Microsoft.Bot.Schema.CommandExtensions
{
    /// <summary>
    /// Value schema of an ICommandActivity
    /// </summary>
    public class CommandValue<T>
    {
        /// <summary>
        /// Sender generated id of the command
        /// </summary>
        public string CommandId { get; set; }

        /// <summary>
        /// Open-ended data
        /// </summary>
        public T Data { get; set; }
    }
}
