This is a skill for post processing serial number input that contains alphanumeric sequences. 

If you want to add this skill to your own bot, simply clone this sample and start following the instructions here [Skill Template documentation](https://microsoft.github.io/botframework-solutions/skills/tutorials/create-skill/csharp/3-create-your-skill/) for resource provision, deployment and customization instructions. 

The core post processing logic is in the SpeechAlphanumericPostProcesing project, please see the SpeechAlphanumericPostProcessing.md for instruction on how to use it in your skill, as well as further substitution that you can add to your skill.

Right now, only English post processing with aggregation of input is fully supported, we're working hard to provide support for Spanish and French.
Please refer to [Known Issues](https://microsoft.github.io/botframework-solutions/help/known-issues/#http-500-error-when-invoking-a-skill) for known issues when invoking a skill.

If you would like to customize this skill for handling inputs that are alphanumeric but are not serial numbers, please customize the prompt and manifest to fit your scenario.
