using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.BotBuilderSamples
{
    public enum RecordingState
    {
        Uninitialized,
        Recording,
        Paused
    }

    public class ConversationData
    {
        public RecordingState RecordingState { get; set; }

        public ConversationData()
        {
            RecordingState = RecordingState.Uninitialized;
        }
    }
}
