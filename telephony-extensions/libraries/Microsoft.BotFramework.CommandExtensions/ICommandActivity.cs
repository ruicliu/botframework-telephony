namespace Microsoft.Bot.Schema.CommandExtensions
{
    /// <summary>
    /// Asynchronous command activity
    /// </summary>
    public interface ICommandActivity : IActivity
    {
        /// <summary>
        /// Name of the command
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Value of type CommandValue
        /// </summary>
        object Value { get; set; }
    }
}
