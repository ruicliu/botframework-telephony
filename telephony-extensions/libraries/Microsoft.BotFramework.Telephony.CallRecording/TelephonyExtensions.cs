using Microsoft.Bot.Schema.CommandExtensions;

namespace Microsoft.Bot.Schema.Telephony
{
    public class TelephonyExtensions
    {
        public static readonly string RecordingStart = "todo_add_command_name";

        public static Activity CreateRecordingStartCommand()
        {
            var startRecordingActivity = new Activity(ActivityTypesWithCommand.Command);

            startRecordingActivity.Name = RecordingStart;

            //startRecordingActivity.Value = new CommandValue<RecordingStartSettings>()
            //{
            //    CommandId = "",
            //    Data = new RecordingStartSettings()
            //    {
            //        ...todo
            //    }
            //};

            return startRecordingActivity;
        }
    }
}
