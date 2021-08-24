Telephony channel enriches FromId field of activities with phone number of the caller.

```csharp
protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
{
    string responseText;
    string responseMessage = null;

    var phoneNumber = turnContext.Activity.From.Name;
    UserAccount account = GetUserAccount(phoneNumber);

    if (account != null)
    {
        responseText = $"Hello and thank you for calling billing department. We have pulled up your account associated with {phoneNumber}. Do you want to continue with this account? Say yes or no.";  
    }
    else
    {
        responseText = $"Hello and thank you for calling billing department. Can you please provide the phone number associated with the account?";
    }
    responseMessage = SimpleConvertToSSML(responseText, "en-US", "en-US-AriaNeural");

        await turnContext.SendActivityAsync(
            GetActivity(responseMessage, responseMessage),
            cancellationToken);
    }
```
