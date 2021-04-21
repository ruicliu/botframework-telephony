# Barge-in

Barge-in, also called "allow interrupt", is the ability of a system to allow callers to interrupt or barge-in during voice playback by speaking or pressing a numpad key (DTMF). Telephony channel enables both forms of barge-in (speech and DTMF) by default. On barge-in, the current bot message will be stopped and any queued bot messages will be dequeued. 

Sometimes it is necessary though, due to legal or compliance requirements, to disallow barge-in on a per message basis. Telephony channel allows barge-in to be disabled for just speech barge-in, just DTMF barge-in or disabled for both.

 ## Disable Barge-In

To disable barge-in (either for speech barge-in, DTMF barge-in, or both), the bot activity must have an inputHint value. The inputHint property indicates whether your bot is accepting, ignoring speech, ignoring non-speech, or ignoring all user input after the message is delivered to the client. The barge-in mode will only affect the message it is applied to, not any subsequent messages. As stated before, both speech and DTMF barge-in are enabled by default so no inputHint is needed for that case (although "acceptingInput" and "expectingInput" will be understood as like the default barge-in enabled by the channel). Below are examples on how to disable only speech, only dtmf, or both forms of barge-in:

### InputHint Additions
There is a class of valid constants for the inputHint field called InputHints in the Microsoft.Bot.Internal.Schema nuget package. The IgnoringNonSpeechInput and IgnoringSpeechInput constants will be added in the future, so for those two cases please use string literal InputHint values "ignoringNonSpeechInput" and "ignoringSpeechInput" respectively. The planned expansion of InputHints looks as follows:
```csharp
    public static class InputHints
    {
        public const string AcceptingInput = "acceptingInput";
        public const string IgnoringInput = "ignoringInput";
        public const string IgnoringNonSpeechInput = "ignoringNonSpeechInput";
        public const string IgnoringSpeechInput = "ignoringSpeechInput";
        public const string ExpectingInput = "expectingInput";
    }
```

 ### Example 1: Disable Both Speech and DTMF Barge-In
To disable both speech and DTMF barge-in, the inputHint field should be set to InputHints.IgnoringInput. This mode ensures the entirety of the message will be played out before any user action can be taken.
```csharp
    private string SimpleConvertToSSML(string text, string voiceId, string locale)
    {
        string ssmlTemplate = @"
        <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{2}'>
            <voice name='{1}'> {0} </voice>
        </speak>";

        return string.Format(ssmlTemplate, text, voiceId, locale);
    }

    var replyText = "The barge-in mode for this message is none, both speech and dtmf barge-in are disabled"
    await turnContext.SendActivityAsync(
        MessageFactory.Text(
            replyText,
            SimpleConvertToSSML(
                replyText,
                "en-US-GuyNeural",
                "en-US"),
            InputHints.IgnoringInput
        ), cancellationToken);
```



 ### Example 2: Disable Just DTMF Barge-In
To disable only DTMF barge-in, the inputHint field should be set to "ignoringNonSpeechInput" (InputHints.IgnoringNonSpeechInput once it is added). This mode ensures only speech barge-in will be honored during message playout, all DTMF (numpad keys) will be ignored.
```csharp
    private string SimpleConvertToSSML(string text, string voiceId, string locale)
    {
        string ssmlTemplate = @"
        <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{2}'>
            <voice name='{1}'> {0} </voice>
        </speak>";

        return string.Format(ssmlTemplate, text, voiceId, locale);
    }

    var replyText = "The barge-in mode for this message is speech only, only dtmf barge-in is disabled"
    await turnContext.SendActivityAsync(
        MessageFactory.Text(
            replyText,
            SimpleConvertToSSML(
                replyText,
                "en-US-GuyNeural",
                "en-US"),
            "ignoringNonSpeechInput"
        ), cancellationToken);
```


 ### Example 3: Disable Just Speech Barge-In
To disable only speech barge-in, the inputHint field should be set to "ignoringSpeechInput" (InputHints.IgnoringSpeechInput once it is added). This mode ensures only DTMF (numpad keys) barge-in will be honored during message playout, any speech from end user will be ignored.
```csharp
    private string SimpleConvertToSSML(string text, string voiceId, string locale)
    {
        string ssmlTemplate = @"
        <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{2}'>
            <voice name='{1}'> {0} </voice>
        </speak>";

        return string.Format(ssmlTemplate, text, voiceId, locale);
    }

    var replyText = "The barge-in mode for this message is dtmf only, only speech barge-in is disabled"
    await turnContext.SendActivityAsync(
        MessageFactory.Text(
            replyText,
            SimpleConvertToSSML(
                replyText,
                "en-US-GuyNeural",
                "en-US"),
            "ignoringSpeechInput"
        ), cancellationToken);
```
_Note:_ In the Public Preview release, the botframework SDK does not yet contain all InputHints contstants (InputHints.IgnoringSpeechInput, InputHints.IgnoringNonSpeechInput). Until support is added in the SDK, please use the strings listed in the "InputHint Additions" section.