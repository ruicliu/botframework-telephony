# Controlling Telephony and speech specific behavior
To allow speech specific functionality, the botframework SDK has extended existing APIs and implemented a new one for recording.

## Allow/disable barge in
To disable barge in for a message, the activity must have "IgnoringInput" as the input hint value.
When using InputHints.IgnoringInput, ensure that an AcceptingInput or ExpectingInput message follows it to guarantee compatibility with existing channels.

```csharp
protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
{
    foreach (var member in membersAdded)
    {
        if (member.Id != turnContext.Activity.Recipient.Id)
        {
            //First message can't be interrupted by the user
            var recordingCallMessage = $"This call may be recorded for quality assurance purposes.";
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(recordingCallMessage, recordingCallMessage, InputHints.IgnoringInput), cancellationToken)
            //This message can be interrupted by the user
            var supportingCallMessage = $"What can I help you with today?";
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(supportingCallMessage, supportingCallMessage, InputHints.ExpectingInput), cancellationToken);
        }
    }
}
```

## Start recording, stop recording, and attach metadata to the recording
Adapter offers “StartRecording”,“StopRecording”, and "ResumeRecording" methods, which have channel specific implementations.

StartRecording when called, starts recording the conversation and returns a recording result containing metadata about the recording.

StopRecording when called stops recording the conversation and returns a recording result containing metadata about the recording.

ResumeRecording when called, resumes recording the conversation, appending the new section of the recording to the previously started recording for this conversation, and returns a recoridng result containing metadata about the recording.

```csharp
public class RecordingResult
{
    public string RecordingId { get; set; }
    public RecordingStatus RecordingStatus { get; set; }
    public string Message { get; set; }
}

public enum RecordingStatus
{
    RecordingStarted,
    RecordingNotStarted,
    RecordingAlreadyInProgress,
    RecordingStopped
}
```

If StopRecording is never called, the recording must be stopped when the channel ends the conversation

If a recording is started for a conversation (On any storage path), recording cannot be started elsewhere. Channel should return "RecordingAlreadyInProgress" in this case, and otherwise do nothing. This will require the channel to ensure some synchronization so that no race condition is experienced by consumers of thsi API.

If StopRecording is called and there is no recording in progress, channel should return "RecordingNotStarted".

If a recording for a single conversation is stopped and started again, the recordings should be appended in storage.

Channels must return a recording id that uniquely refers to the recording at the storage path.
Channels should prefer to use conversation id as this recording id where possible.

```csharp
public class AdapterWithRecording : AdapterWithErrorHandler
{
    public AdapterWithRecording(IConfiguration configuration, ILogger<BotFrameworkHttpAdapter> logger, ConversationState conversationState = null) : base(configuration, logger, conversationState)
    {
    }

    public virtual async Task<RecordingResult> StartRecordingAsync(string StoragePath, ITurnContext turnContext, CancellationToken cancellationToken)
    {
        //Endpoint like /{conversationId}/StartRecording
        return new RecordingResult
        {
            RecordingStatus = RecordingStatus.RecordingStarted,
            Message = "Recording has started successfully."
        };
    }

    public virtual async Task<RecordingResult> StopRecordingAsync(ITurnContext turnContext, CancellationToken cancellationToken)
    {
        //Endpoint like /{conversationId}/StopRecording
        return new RecordingResult
        {
            RecordingStatus = RecordingStatus.RecordingStopped,
            Message = "Recording has stopped successfully."
        };
    }

    public virtual async Task<RecordingResult> ResumeRecordingAsync(ITurnContext turnContext, CancellationToken cancellationToken)
    {
        //Endpoint like /{conversationId}/ResumeRecording
        return new RecordingResult
        {
            RecordingStatus = RecordingStatus.RecordingStarted,
            Message = "Recording has resumed successfully."
        };
    }
}
```

Call Pattern
```csharp
protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
{
    foreach (var member in membersAdded)
    {
        // Greet anyone that was not the target (recipient) of this message.
        if (member.Id != turnContext.Activity.Recipient.Id)
        {
            var response = VoiceFactory.TextAndVoice($"Welcome to {CompanyName}! This call may be recorded for quality assurance purposes.");

            await turnContext.SendActivityAsync(response, cancellationToken);
            AdapterWithRecording adapterWithRecording = (AdapterWithRecording)turnContext.Adapter;

            var recordingResult = await adapterWithRecording.StartRecordingAsync("Storage1", turnContext, cancellationToken);
            if (recordingResult.RecordingStatus == RecordingStatus.RecordingNotStarted)
            {
                var recordingFailed = VoiceFactory.TextAndVoice($"Recording has failed, but your call will continue.");

                await turnContext.SendActivityAsync(recordingFailed, cancellationToken);
            }

            await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
        }
    }
}
```