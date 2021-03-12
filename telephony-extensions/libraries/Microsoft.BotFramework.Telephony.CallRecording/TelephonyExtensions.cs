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
        public static readonly string RecordingStart = "channel/vnd.microsoft.telephony.recording.start";
        public static readonly string RecordingPause = "channel/vnd.microsoft.telephony.recording.pause";
        public static readonly string RecordingResume = "channel/vnd.microsoft.telephony.recording.resume";

        public static async Task<ResourceResponse> StartRecording(this ITurnContext turnContext, CancellationToken cancellationToken)
        {
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

        public static async Task<ResourceResponse> PauseRecording(this ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var pauseRecordingActivity = new Activity(ActivityTypesWithCommand.Command);

            pauseRecordingActivity.Name = RecordingPause;
            var response = await turnContext.SendActivityAsync(pauseRecordingActivity, cancellationToken).ConfigureAwait(false);

            return response;
        }

        public static async Task<ResourceResponse> ResumeRecording(this ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var resumeRecordingActivity = new Activity(ActivityTypesWithCommand.Command);

            resumeRecordingActivity.Name = RecordingResume;

            var response = await turnContext.SendActivityAsync(resumeRecordingActivity, cancellationToken).ConfigureAwait(false);

            return response;
        }
    }
}
