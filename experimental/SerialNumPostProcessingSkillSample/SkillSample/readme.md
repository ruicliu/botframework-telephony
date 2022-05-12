## Skill Overview
This is a skill that focuses on post processing serial number input with alphanumeric sequences. It will try to correct the interpreted input from speech to text as much as it can (such as fixing "One" to "1"), to output a serial number with the correct format and input. 
The main dialog is in SerialNumPostProcessAction, which starts-off prompting for user's serial number, and will continously prompt the user until the input from user has met the specified pattern. 
To specify the pattern needed for the core post processor, please see the section below.

## Detail on Post Processing Logic
The core post processing logic is in the SpeechAlphanumericPostProcesing project, please see [SpeechAlphanumericPostProcessing.md](https://github.com/microsoft/botframework-telephony/blob/main/experimental/SerialNumPostProcessingSkillSample/SpeechAlphanumericPostProcessing/SpeechAlphanumericPostProcessing.md) for instruction on how to use it in your skill, as well as further substitution that you can add to your skill.

## Adding this Skill to PVA bot
Ensure that you have added an appsettings.json file [appsettings.json](https://github.com/microsoft/botframework-solutions/blob/master/samples/csharp/skill/SkillSample/appsettings.json) in the same directory as this readme.md file. 

If you want to add this skill to your own PVA bot so that PVA can use this skill, simply clone this sample and start following the instructions here [Skill Template documentation](https://microsoft.github.io/botframework-solutions/skills/tutorials/create-skill/csharp/2-download-and-install/) by first downloading the necessary tools for deploying the skills to Azure. 
Then, navigate to the "Provision your Azure resources" section on the same tutorial page to deploy your skill to Azure.

To use this skill in PVA, please update manifest-1.0.json in the wwwroot folder, please update all occurrences of {YOUR_SKILL_URL} and {YOUR_SKILL_APPID} with the appropriate values. The Skill Template doc has more guidance on that as well.

## Customization
If you would like to customize this skill for handling inputs that are alphanumeric but are not serial numbers, please customize the prompt and manifest to fit your scenario.

## Future Work
Right now, only English post processing with aggregation of input is fully supported, we're working hard to provide support for Spanish and French.
Please refer to [Known Issues](https://microsoft.github.io/botframework-solutions/help/known-issues/#http-500-error-when-invoking-a-skill) for known issues when invoking a skill.
