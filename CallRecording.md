# Call Recording

__Disclaimer__: Call recording is temporarily available through the Telephony Channel public preview at no charge. Be aware that Azure billing for Call Recording will begin in April, at a rate of $0.01/minute of recorded content. Services in public preview are subject to future pricing changes.

---

## Start recording, stop recording, and attach metadata to the recording
Telephony extensions package offers _StartRecording_, _StopRecording_, and _ResumeRecording_ methods:

- _StartRecording_ starts recording the conversation. 
- _StopRecording_ stops recording the conversation.
- _ResumeRecording_ resumes recording the conversation, appending the new section of the recording to the previously started recording for this conversation.

If _StopRecording_ is never called, the recording is stopped when the bot ends the conversation.

If a recording is started for a conversation, another recording for the same conversation cannot be started. In such case, Telephony channel returns an error indicating that the "Recording is already in progress".

If _StopRecording_ is called and there is no recording in progress, Telephony channel returns an error indicating that the "Recording has not started".

If a recording for a single conversation is paused and resumed again, the recordings are appended in storage.

If a recording for a single conversation is stopped and started again, the recordings appear as multiple recording sessions in the storage.

```csharp
public class TelephonyExtensions
{
    public static readonly string RecordingStart = "recording_start_command";

    public static Activity CreateRecordingStartCommand()
    {
        var startRecordingActivity = new Activity(ActivityTypesWithCommand.Command)
        {
            Name = RecordingStart;
            Value = new CommandValue<RecordingStartSettings>()
        };

        return startRecordingActivity;
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
            // Start recording when the call begins
            await turnContext.SendActivityAsync(TelephonyExtensions.CreateRecordingStartCommand(), cancellationToken);

            var response = VoiceFactory.TextAndVoice($"Welcome to {CompanyName}! This call may be recorded for quality assurance purposes.");
            await turnContext.SendActivityAsync(response, cancellationToken);

            await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
        }
    }
}

protected override async Task OnRecordingStartResultAsync(ITurnContext<ICommandResultActivity> turnContext, CancellationToken cancellationToken)
{
    var result = CommandExtensions.GetCommandResultValue<object>(turnContext.Activity);

    // Check if recording started successfully
    if (result.Error != null)
    {
        var recordingFailed = VoiceFactory.TextAndVoice($"Recording has failed, but your call will continue.");
        wait turnContext.SendActivityAsync(recordingStatusText, cancellationToken);
    }
}
```
