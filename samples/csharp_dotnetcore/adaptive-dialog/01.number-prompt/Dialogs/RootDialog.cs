using System.Collections.Generic;
using System.IO;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Generators;
using Microsoft.Bot.Builder.LanguageGeneration;

namespace MultiTurnPromptBot.Dialogs
{
    public class RootDialog : AdaptiveDialog
    {
        public RootDialog() : base(nameof(RootDialog))
        {
            string[] paths = { ".", "Dialogs", $"RootDialog.lg" };
            string fullPath = Path.Combine(paths);

            // These steps are executed when this Adaptive Dialog begins
            Triggers = new List<OnCondition>()
                {
                    // Add a rule to welcome user
                    new OnConversationUpdateActivity()
                    {
                        Actions = WelcomeUserSteps()
                    },

                    // Respond to user on message activity
                    new OnUnknownIntent()
                    {
                        Actions = GatheUserInformation()
                    },
                };
            Generator = new TemplateEngineLanguageGenerator(Templates.ParseFile(fullPath));
        }

        private static List<Dialog> WelcomeUserSteps()
        {
            return new List<Dialog>()
            {
                // Iterate through membersAdded list and greet user added to the conversation.
                new Foreach()
                {
                    ItemsProperty = "turn.activity.membersAdded",
                    Actions = new List<Dialog>()
                    {
                        // Note: Some channels send two conversation update events - one for the Bot added to the conversation and another for user.
                        // Filter cases where the bot itself is the recipient of the message. 
                        new IfCondition()
                        {
                            Condition = "$foreach.value.name != turn.activity.recipient.name",
                            Actions = new List<Dialog>()
                            {
                                new SendActivity("Hello, I'm a telephony test bot. Please send a message to get started!")
                            }
                        }
                    }
                }
            };
        }

        private static List<Dialog> GatheUserInformation()
        {
            return new List<Dialog>()
            {
                new BatchNumericInput() {
                    Prompt = "What is your phone number? Please say or dial using the numerical keypad. Press # when finished",
                    Property = "user.userProfile.Phone",
                    TerminatingSymbol = "#",    // terminate after seeing '#'
                    MaxLen = 10                 // ...or when the input length reaches 10
                },
                new SendActivity("Thanks, I got your phone as '${user.userProfile.Phone}'"),
                new IfCondition()
                {
                    Condition = "startsWith(user.userProfile.Phone, '425')",
                    Actions = new List<Dialog>()
                    {
                        new SendActivity("I think you're in Redmond WA"),
                    },
                    ElseActions = new List<Dialog>()
                    {
                        new SendActivity("I think you're form somewhere else"),
                    }
                },
                new EndDialog(),
            };
        }
    }
}
