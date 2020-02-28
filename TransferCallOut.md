# Transfer call to an agent

There could be various occasions when a bot would like to transfer call to a human agent. Telephony Channel in Microsoft Bot Framework allows you to transfer call to an agent over a phone number, using [Microsoft Bot Framework Handoff library](https://github.com/microsoft/BotBuilder-Samples/tree/handoffv2/experimental/handoff-library).

Please follow these steps to transfer a call to an agent.

## Initiate transfer
You can use ```OnMessageActivityAsync``` method to initiate call transfer:

```csharp
protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
{
    string requestMessage = turnContext.Activity.Text.Trim().ToLower();
    string responseMessage = null;

    if (string.Equals("0", requestMessage) ||
        string.Equals("Talk to agent", requestMessage, System.StringComparison.OrdinalIgnoreCase))
    {
		var context = new { TargetPhoneNumber = "+14251231234" };
		var transcript = new List<Activity> { };
		var handoffInitiateEvent = EventFactory.CreateHandoffInitiation(turnContext, context, new Transcript(transcript));
		await turnContext.SendActivityAsync(handoffInitiateEvent);
    }
    else if (string.Equals("1", requestMessage) ||
        string.Equals("one", requestMessage, System.StringComparison.OrdinalIgnoreCase))
    {
        var responseText = @"Hello and thank you for calling billing department.  
          If you are a current customer, press 1.  
          If you are a new customer, press 2.";
          
        responseMessage = SimpleConvertToSSML(responseText, "en-US", "en-US-JessaNeural");
    }
    else
    {
        responseMessage = SimpleConvertToSSML("What I heard was \"" + requestMessage + "\"", "en-US", "en-US-GuyNeural");
    }

    if (!string.IsNullOrWhiteSpace(responseMessage))
    {
        await turnContext.SendActivityAsync(
            GetActivity(responseMessage, responseMessage),
            cancellationToken);
    }
}
```

Microsoft Bot Framework Handoff library will be available in Bot Framework SDK 4.8. For now, please copy [EventFactory.cs](https://github.com/microsoft/BotBuilder-Samples/blob/handoffv2/experimental/handoff-library/csharp_dotnetcore/Microsoft.Bot.Builder.Handoff/EventFactory.cs) and [HandoffEventNames.cs](https://github.com/microsoft/BotBuilder-Samples/blob/handoffv2/experimental/handoff-library/csharp_dotnetcore/Microsoft.Bot.Builder.Handoff/HandoffEventNames.cs) to your project.

## Track transfer status
Telephony Channel sends back ```handoff.status``` event to let the bot know if transfer was successful. You can use ```OnEventActivityAsync``` to handle transfer status.

```csharp
protected override async Task OnEventActivityAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
{
	string eventName = turnContext.Activity.Name;

	if (string.Equals(eventName, "handoff.status"))
	{
		dynamic handoffStatus = turnContext.Activity.Value;
		string state = handoffStatus["state"];
		if (string.Equals(state, "accepted"))
		{
			// Hand off has been accepted by the target successfully
			// Bot is no more in the loop.
		} else if (string.Equals(state, "failed"))
		{
			// Hand off has failed.
			// Bot is still connected to end user.
			string errorMessage = handoffStatus["message"];
		}
	}

	await base.OnEventActivityAsync(turnContext, cancellationToken);
}
```

## Notes
* If target doesn't pick up the call, Telephony Channel would keep calling target again and again.
