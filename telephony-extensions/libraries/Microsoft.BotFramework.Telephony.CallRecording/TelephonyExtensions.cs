using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema.CommandExtensions;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Schema.Telephony
{
    public static class TelephonyExtensions
    {
        public static readonly string TelephonyChannelId = "telephony";

        public static readonly string RecordingStart = "channel/vnd.microsoft.telephony.recording.start";
        public static readonly string RecordingPause = "channel/vnd.microsoft.telephony.recording.pause";
        public static readonly string RecordingResume = "channel/vnd.microsoft.telephony.recording.resume";

        public static bool IsTelephonyChannel(string channelId)
        {
            if(string.IsNullOrEmpty(channelId))
                throw new InvalidOperationException("Invalid activity. ChannelId is missing");
            
            return (channelId == TelephonyChannelId);
        }

        public static NotSupportedException CommandNotSupportedOnChannel(string command, string channelId)
        {
            throw new NotSupportedException($"'{command}' is not supported on {channelId} channel");
        }

        public static NotSupportedException CommandResultNotSupportedOnChannel(string commandResult, string channelId)
        {
            throw new NotSupportedException($"'{commandResult}' is not supported on {channelId} channel");
        }

        public static void VerifyChannelForCommand(string command, ITurnContext turnContext)
        {
            if (!IsTelephonyChannel(turnContext.Activity.ChannelId))
            {
                CommandNotSupportedOnChannel(command, turnContext.Activity.ChannelId);
            }
        }

        public static void VerifyChannelForCommandResult(string commandResult, ITurnContext turnContext)
        {
            if (!IsTelephonyChannel(turnContext.Activity.ChannelId))
            {
                CommandResultNotSupportedOnChannel(commandResult, turnContext.Activity.ChannelId);
            }
        }

        public static async Task<ResourceResponse> TryRecordingStart(this ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // Recording Start is only supported on the Telephony Channel
            VerifyChannelForCommand(RecordingStart, turnContext);

            var startRecordingActivity = new Activity(ActivityTypesWithCommand.Command);

            startRecordingActivity.Name = RecordingStart;

            startRecordingActivity.Value = new CommandValue<RecordingStartSettings>()
            {
                CommandId = Guid.NewGuid().ToString(),
                Data = new RecordingStartSettings()
                {
                    RecordingChannelType = RecordingChannelType.Mixed,
                    RecordingContentType = RecordingContentType.AudioVideo
                }
            };

            var response = await turnContext.SendActivityAsync(startRecordingActivity, cancellationToken).ConfigureAwait(false);

            return response;
        }

        public static async Task<ResourceResponse> TryRecordingPause(this ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // Recording Pause is only supported on the Telephony Channel
            VerifyChannelForCommand(RecordingPause, turnContext);

            var pauseRecordingActivity = new Activity(ActivityTypesWithCommand.Command);

            pauseRecordingActivity.Name = RecordingPause;
            var response = await turnContext.SendActivityAsync(pauseRecordingActivity, cancellationToken).ConfigureAwait(false);

            return response;
        }

        public static async Task<ResourceResponse> TryRecordingResume(this ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // Recording Resume is only supported on the Telephony Channel
            VerifyChannelForCommand(RecordingResume, turnContext);

            var resumeRecordingActivity = new Activity(ActivityTypesWithCommand.Command);

            resumeRecordingActivity.Name = RecordingResume;

            var response = await turnContext.SendActivityAsync(resumeRecordingActivity, cancellationToken).ConfigureAwait(false);

            return response;
        }
    }
}
