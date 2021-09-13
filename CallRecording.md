# Call Recording

__Disclaimer__: Call recording is temporarily available through the Telephony Channel public preview at no charge. Be aware that Azure billing for Call Recording will begin in April 2021, at a rate of $0.01/minute of recorded content. Services in public preview are subject to future pricing changes.

> Many countries and states have laws and regulations that apply to the recording of PSTN, voice, and video calls, which often require that users consent to the recording of their communications. It is your responsibility to use the call recording capabilities in compliance with the law. You must obtain consent from the parties of recorded communications in a manner that complies with the laws applicable to each participant.

## Start recording, stop recording, and attach metadata to the recording
Telephony extensions package offers _StartRecording_, _PauseRecording_, and _ResumeRecording_ methods:

- _StartRecording_ starts recording of the conversation. 
- _PauseRecording_ pauses recording of the conversation.
- _ResumeRecording_ resumes recording of the conversation, appending the new section of the recording to the previously started recording for this conversation.
- _StopRecording_ stops recording of the conversation. 

Note that it is not required to call _StopRecording_ explicitly. The recording is always stopped when the bot/caller ends the conversation or if the call is transferred to an external phone number.

### Validation
- If a recording is started for a conversation, another recording for the same conversation cannot be started. In such case, Telephony channel returns an error indicating that the "Recording is already in progress".

- If _PauseRecording_ is called and there is no recording in progress, Telephony channel returns an error indicating that the "Recording has not started".

- If _StopRecording_ is called and there is no recording in progress, Telephony channel returns an error indicating that the "Recording has not started".

### Recording Sessions
- If a recording for a single conversation is paused and resumed again, the recordings are appended in storage.

- If a recording for a single conversation is stopped and started again, the recordings appear as multiple recording sessions in the storage.

- We do not recommend using the pattern StartRecording-StopRecording-StartRecording-StopRecording since it creates multiple recording files for a single conversation. Instead, we recommend using StartRecording-PauseRecording-ResumeRecording-EndCall/StopRecording to create a single recording file for the converastion.

The following is an example of starting call recording at the beginning of the conversation:

```csharp
public class TelephonyExtensions
{
    public static readonly string RecordingStart = "channel/vnd.microsoft.telephony.recording.start";

    public static Activity CreateRecordingStartCommand()
    {
        var startRecordingActivity = new Activity(ActivityTypesWithCommand.Command)
        {
            Name = RecordingStart            
        };

        return startRecordingActivity;
    }
}
```

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
        }
    }
}
```

To ensure call recording has started successfully, bots should await the "recording start" command result. This is done by overriding the `OnRecordingStartResultAsync` method of bot:

```csharp
protected override async Task OnRecordingStartResultAsync(ITurnContext<ICommandResultActivity> turnContext, CancellationToken cancellationToken)
{
    var result = CommandExtensions.GetCommandResultValue<object>(turnContext.Activity);

    // Check if recording started successfully
    if (result.Error == null)
    {
        // Call recording has started successfully, bot can proceed
        // ...
    }
    else
    {
        var recordingFailed = VoiceFactory.TextAndVoice($"Recording has failed, but your call will continue.");
        await turnContext.SendActivityAsync(recordingStatusText, cancellationToken);
    }
}
```

## Retrieve call recordings
Azure Communication Services (ACS) provides short term media storage for recordings, please export any recorded content, you wish to preserve, within 48 hours. After 48 hours, recordings will no longer be available.

An Event Grid notification `Call Recording File Status Updated` is published when a recording is ready for retrieval, typically 1-2 minutes after the recording process has completed (i.e. meeting ended, recording stopped). Read more about these notifications on [ACS Call Recording documentation](https://docs.microsoft.com/en-us/azure/communication-services/concepts/voice-video-calling/call-recording#event-grid-notifications).

There are two documented ways to retrieve the call recording
* `[Recommended]` Through the `DownloadStreamingAsync` API in ACS SDK as documented [here](https://docs.microsoft.com/azure/communication-services/quickstarts/voice-video-calling/call-recording-sample?pivots=programming-language-csharp#download-recording-file-using-downloadstreamingasync-server-api). In-depth sample code is available on ACS [GitHub repo](https://github.com/Azure-Samples/communication-services-dotnet-quickstarts/blob/main/ServerRecording/Controller/CallRecordingController.cs#L258).
* Alternatively, directly call the REST API using authentiation Sample code for handling event grid notifications and downloading recording and meta-data files can be found [here](https://github.com/microsoft/botframework-telephony/tree/main/samples/csharp_dotnetcore/05a.telephony-recording-download-function).

Regulations such as GDPR require the ability to export user data. In order to enable to support these requirements, recording meta data files include the participantId for each call participant in the participants[] array. You can cross-reference the MRIs in the participants[] array with your internal user identities to identify participants in a call. 

_Learn more about ACS Call Recording on [their official documentation](https://docs.microsoft.com/azure/communication-services/quickstarts/voice-video-calling/call-recording-sample?pivots=programming-language-csharp#download-recording-file-using-downloadstreamingasync-server-api)._
