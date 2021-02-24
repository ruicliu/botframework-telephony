namespace Microsoft.Bot.Schema.CommandExtensions
{
    /// <summary>
    /// Result from a Command Activity.
    /// </summary>
    public interface ICommandResultActivity : IActivity
    {
        /// <summary>
        /// Name of the command
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Value of type CommandResultValue
        /// </summary>
        object Value { get; set; }
    }
}
