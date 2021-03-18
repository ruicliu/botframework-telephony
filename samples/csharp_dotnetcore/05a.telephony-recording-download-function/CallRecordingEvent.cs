using System;

namespace BotFramework.Telephony.Samples
{
    public static class DownloadRecordingConstants
    {
        public static string RecordingFileStatusUpdated = "Microsoft.Communication.RecordingFileStatusUpdated";
    }

    public class RecordingChunks
    {
        public string DocumentId {get; set;}
        public int Index {get; set;}
        public string EndReason {get; set;}
    }

    public class RecordingStorageInfo
    {
        public RecordingChunks[] RecordingChunks; 
    }

    public class RecordingFileStatusUpdatedEventData
    {
        public DateTime RecordingStartTime;
        public long RecordingDurationMs;
        public string SessionEndReason;
        public RecordingStorageInfo RecordingStorageInfo; 
    }
}