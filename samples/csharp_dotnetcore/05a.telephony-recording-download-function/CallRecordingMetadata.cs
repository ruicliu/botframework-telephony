using System;

namespace BotFramework.Telephony.Samples
{
    public class AudioConfiguration
    {
        public string SampleRate;
        public int BitDepth;
        public int BitRate;
    }

    public class VideoConfiguration
    {
        public int LongerSideLength;
        public int ShorterSideLength;
        public int FrameRate;
        public int BitRate;
    }

    public class RecordingInfo
    {
        public string ContentType;
        public string ChannelType;
        public string Format;
        public AudioConfiguration AudioConfiguration;
        public VideoConfiguration VideoConfiguration;
    }

    public class Participant
    {
        public string ParticipantId;
    }

    public class RecordingMetadata
    {
        public string ResourceId;
        public string CallId;
        public string ChunkDocumentId;
        public int ChunkIndex;
        public DateTime ChunkStartTime;
        public long ChunkDuration;
        //"pauseResumeIntervals": [],
        public RecordingInfo RecordingInfo;
        public Participant[] Participants;
    }
}