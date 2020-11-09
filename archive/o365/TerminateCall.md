# Terminate the ongoing call

In cases such as when end user is unauthorized or when bot decides to end the conversation, bot might want to hang up the call. To do so, bot can send EndOfConversation activity. Here is the sample bot code to end the call:

```csharp
protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
{
    string customerCode = turnContext.Activity.Text.Trim().ToLower();

    // If user is not from white list, drop the call.
    if (!whiteListedCustomers.Contains(customerCode.ToLower()))
    {
        await turnContext.SendActivityAsync(
            Activity.CreateEndOfConversationActivity(),
            cancellationToken);
    }
    else
    {
        responseMessage = SimpleConvertToSSML("Thanks for providing customer code", "en-US", "en-US-GuyNeural");
        await turnContext.SendActivityAsync(
            GetActivity(responseMessage, responseMessage),
            cancellationToken);
    }
}
```
