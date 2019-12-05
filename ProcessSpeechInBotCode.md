# Processing Speech inside the Bot

To ensure your bot can speak, all you need is to populate the 'Speak' field in the response activities.

```
private string SimpleConvertToSSML(string text, string voiceId, string locale)
{
    string ssmlTemplate = @"
    <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{2}'>
        <voice name='{1}'>{0}</voice>
    </speak>";

    return string.Format(ssmlTemplate, text, voiceId, locale);
}


protected override async Task OnConversationUpdateActivityAsync
  (ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var intromessagetext = $@"
                        Welcome to demo telephony bot. 
                        For billing questions, press or say 1. 
                        To sign up for the new service, press or say 2.
                        Otherwise, just tell me something, and I will repeat it back.";

            var ssml = SimpleConvertToSSML(intromessagetext, "en-US-GuyNeural", "en-us");

            await turnContext.SendActivityAsync(
              MessageFactory.Text(intromessagetext, ssml), 
              cancellationToken);
        }
```

You can also leverage the full power of [Speech Synthesis Markup Language (SSML)](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-synthesis-markup) to customize your scenarios.
