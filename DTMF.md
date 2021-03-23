# Recognize dual-tone multi-frequency signaling (DTMF) input

[DTMF](https://en.wikipedia.org/wiki/Dual-tone_multi-frequency_signaling), or dual-tone multi-frequency signaling, is sent to the bot as individual messages one number at a time. DTMF input can be recognized by the presence of an entity of type "DTMF", as demonstrated in the below example:

```csharp
protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext)
{
    if (turnContext.Activity.Text == null) return;

    string text = turnContext.Activity.Text.ToLower();

    if(Int32.TryParse(text, out int number) && number >= 0 && number <= 9)
    {
        if (turnContext.Activity.Entities?.Any(e => e.Type == "DTMF") == true)
        {
            await turnContext.SendActivityAsync($"You pressed number {text} on the numerical keypad");
        }
        else
        {
            await turnContext.SendActivityAsync($"You said {text}");
        }
    }
}
```
