# Barge-in

Barge-in, also called "allow interrupt", is the ability of a system to allow callers to interrupt or barge-in during voice playback by speaking or pressing a numpad key (DTMF). Telephony channel enables both forms of barge-in (speech and DTMF) by default. On barge-in, the current bot message will be stopped and any queued bot messages will be dequeued. 

Sometimes it is necessary though, due to legal or compliance requirements, to disallow barge-in on a per message basis. Telephony channel allows barge-in to be disabled for just speech barge-in, just DTMF barge-in or disabled for both.

 ## Disable Barge-In

 To disable barge-in, the activity must have an "Input" Enitity with the desired barge-in mode. The barge-in mode will only affect the message it is applied to, not any subsequent messages. As stated before, both speech and DTMF barge-in are enabled by default so no Input entity needs to be added in that case.

 ### Example 1: Disable Both Speech and DTMF Barge-In
To disable both speech and DTMF barge-in, a "BargeInMode" property in the "Input" entitiy needs to be set to "None". This mode ensures the entirety of the message will be played out before any user action can be taken.

```csharp
private string SimpleConvertToSSML(string text, string voiceId, string locale)
{
    string ssmlTemplate = @"
    <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{2}'>
        <voice name='{1}'> {0} </voice>
    </speak>";

    return string.Format(ssmlTemplate, text, voiceId, locale);
}

var entities = new List<Entity>();
var entityProperties = new Newtonsoft.Json.Linq.JObject();
var inputEntity = new Entity("Input");

entityProperties.Add("BargeInMode", "None");
inputEntity.Properties = entityProperties;
entities.Add(inputEntity);

var replyText = $"the current barge-in mode is None, try barging into this utterance";

await turnContext.SendActivityAsync(
    new Activity(
        type: ActivityTypes.Message,
        text: replyText, 
        speak: SimpleConvertToSSML(
            replyText, 
            "en-US-GuyNeural",
            "en-US"),
        entities: entities),
    cancellationToken: cancellationToken);
```


 ### Example 2: Disable Just DTMF Barge-In
To disable only DTMF barge-in, a "BargeInMode" property in the "Input" entitiy needs to be set to "Speech".

```csharp
private string SimpleConvertToSSML(string text, string voiceId, string locale)
{
    string ssmlTemplate = @"
    <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{2}'>
        <voice name='{1}'> {0} </voice>
    </speak>";

    return string.Format(ssmlTemplate, text, voiceId, locale);
}

var entities = new List<Entity>();
var entityProperties = new Newtonsoft.Json.Linq.JObject();
var inputEntity = new Entity("Input");

entityProperties.Add("BargeInMode", "Speech");
inputEntity.Properties = entityProperties;
entities.Add(inputEntity);

var replyText = $"the current barge-in mode is Speech only, try barging into this utterance";

await turnContext.SendActivityAsync(
    new Activity(
        type: ActivityTypes.Message,
        text: replyText, 
        speak: SimpleConvertToSSML(
            replyText, 
            "en-US-GuyNeural",
            "en-US"),
        entities: entities),
    cancellationToken: cancellationToken);
```


 ### Example 3: Disable Just Speech Barge-In
To disable only speech barge-in, a "BargeInMode" property in the "Input" entitiy needs to be set to "DTMF".

```csharp
private string SimpleConvertToSSML(string text, string voiceId, string locale)
{
    string ssmlTemplate = @"
    <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{2}'>
        <voice name='{1}'> {0} </voice>
    </speak>";

    return string.Format(ssmlTemplate, text, voiceId, locale);
}

var entities = new List<Entity>();
var entityProperties = new Newtonsoft.Json.Linq.JObject();
var inputEntity = new Entity("Input");

entityProperties.Add("BargeInMode", "DTMF");
inputEntity.Properties = entityProperties;
entities.Add(inputEntity);

var replyText = $"the current barge-in mode is DTMF only, try barging into this utterance";

await turnContext.SendActivityAsync(
    new Activity(
        type: ActivityTypes.Message,
        text: replyText, 
        speak: SimpleConvertToSSML(
            replyText, 
            "en-US-GuyNeural",
            "en-US"),
        entities: entities),
    cancellationToken: cancellationToken);
```