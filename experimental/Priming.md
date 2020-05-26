# Allowing speech priming for indicating expected input formats as well as phrases

## Priming speech to identify input styles
To signal speech to prefer individual symbols or characters (ie. 2 instead of "too"), the listenFor field may be extended with inputtype indicators from the following set:

* {inputtype:digit} for preferring digits
* {inputtype:symbol} for preferring symbols
* {inputtype:alpha} for preferring letters

Digits include the characters 0-9
Letters include individual characters from an alphabet
Symbols include individual characters not included the first two sets like !@#$%^&*() 

Prefer implementing these in prompts.

```csharp
private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
{
    var messageText = "Please say your phone number";
    var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
    var listenFor = new List<string>()
      {
          "{inputtype:digit}",
          "{inputtype:symbol}"
      };
    promptMessage.ListenFor = listenFor;
    return await stepContext.PromptAsync(nameof(NumberPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
}
```

## Signaling the channel to enable batch DTMF input
By default DTMF signals from the users phone are sent to the bot instantly. To enable batching of these DTMF signals, specify a regex pattern in the listenFor field.

```csharp
private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
{
    var messageText = "Please dial your phone number followed by the pound sign";
    var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
    var listenFor = new List<string>()
      {
          "{pattern:^[0-9].#$}"
      };
    promptMessage.ListenFor = listenFor;
    return await stepContext.PromptAsync(nameof(NumberPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
}
```

The following example accepts a specified number of digits before the input is sent to the bot.

```csharp
private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
{
    var messageText = "Please dial your phone number followed by the pound sign";
    var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
    var listenFor = new List<string>()
      {
          "{pattern:^[0-9]{10}$}"
      };
    promptMessage.ListenFor = listenFor;
    return await stepContext.PromptAsync(nameof(NumberPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
}
```