# Processing speech inside the bot

With Microsoft Bot Framework, you can use over 80 different locales / voice fonts to interact with your customers:  [language and region support for the speech service](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support).

## Making your bot speak
To ensure your bot can speak, all you need is to populate the 'Speak' field in the response activities.


```
protected override async Task OnConversationUpdateActivityAsync
  (ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
{
  var messageText = $@"
    Welcome to the demo telephony bot. 
    For billing questions, press or say 1. 
    To sign up for the new service, press or say 2.
    Otherwise, just tell me something, and I will repeat it back.";

  await turnContext.SendActivityAsync(MessageFactory.Text(messageText, messageText), cancellationToken);
}
```

## Advanced voice and speech control

For more advanced voice control, you can leverage the full power of [Speech Synthesis Markup Language (SSML)](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-synthesis-markup) to customize the speech output, including adding regional differences, specifying genders, speaking styles (cheerful, empathetics, etc).

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

  await turnContext.SendActivityAsync(MessageFactory.Text(intromessagetext, ssml), cancellationToken);
}

```
