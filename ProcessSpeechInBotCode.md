# Processing Speech Inside of the Bot

```
protected override async Task OnConversationUpdateActivityAsync
  (ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var intromessagetext = $@"
                        Welcome to demo telephony bot. 
                        For billing questions, press or say 1. 
                        To sign up for the new service, press or say 2.
                        Otherwise, just tell me something, and I will repeat it back.";

            var intromessagevoice = SimpleConvertToSSML(intromessagetext, "en-US", "en-US-GuyNeural", "chat");
            await turnContext.SendActivityAsync(GetActivity(intromessagetext, intromessagevoice), cancellationToken);
        }
```
