using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CoreBot.Prompts
{
    public class PurchaseOrderNumberPrompt : TextPrompt
    {
        public PurchaseOrderNumberPrompt() : base(nameof(PurchaseOrderNumberPrompt), Validation)
        {
        }

        private static Task<bool> Validation(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            if (!promptContext.Recognized.Succeeded) return Task.FromResult(false);
            var textToValidate = ReplaceForValidation(promptContext.Recognized.Value);

            return Task.FromResult(
                Regex.IsMatch(textToValidate, "^[A-Za-z][0-9]{5}$")
                );
        }

        private static string ReplaceForValidation(string inputToReplace)
        {
            //Speech sometimes doesn't match our desired inputs perfectly, so we are handling some edge cases here.
            var replacements = new List<Tuple<string, string>>()
            {
                new Tuple<string, string> ( " ", "" ),
                new Tuple<string, string> ( "one", "1" ),
                new Tuple<string, string> ( "in", "N" )
            };

            foreach(var replacement in replacements)
            {
                inputToReplace = inputToReplace.Replace(replacement.Item1, replacement.Item2);
            }

            return inputToReplace;
        }
    }
}
