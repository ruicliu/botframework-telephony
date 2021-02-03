# Terminate the ongoing call

Bot can end the conversation by hanging up the call. To do so, send the `EndOfConversation` activity. Here is the sample bot code to end the call:

```csharp
protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext)
{
    ...

    if (NeedToEndConversation())
    {
        await turnContext.SendActivityAsync(Activity.CreateEndOfConversationActivity());
    }
    else
    {
        await turnContext.SendActivityAsync("Hello user");
    }
}
```
