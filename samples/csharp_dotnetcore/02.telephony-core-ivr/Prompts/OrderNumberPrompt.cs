using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreBot.Prompts
{
    /// <summary>
    /// Simple prompt to validate a 5 digit order code
    /// </summary>
    public class OrderNumberPrompt : TextPrompt
    {
        public OrderNumberPrompt() : base(nameof(OrderNumberPrompt), Validation)
        {
        }
        private static Task<bool> Validation(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(
                promptContext.Recognized.Succeeded &&
                promptContext.Recognized.Value.Length == 5 &&
                promptContext.Recognized.Value.All(Char.IsDigit)
                );
        }
    }
}
