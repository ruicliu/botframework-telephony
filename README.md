# Connect your bot to a phone number with Microsoft Bot Framework 

<!-- ![](images/bot-framework-default.png) -->

<p align="center">
  <img src="images/bot-framework-default.png" />
</p>

**Telephony channel** in a Microsoft technology for enabling phone calling capabilities in a bot. Put another way, you can connect your bot to a phone number and interact with your bot by voice via a phone.

Telephony channel leverages the power of Microsoft Bot Framework combined with the Azure Communication Services and the Microsoft Speech Services. 
 
 ---
__Please note__:  This is a Beta (preview) version of software, and as with any preview software, there may be initial risks and limitations you run into, such as a need to integrate with your existing IVR, etc.  We are working on and supporting this product and are here to help you in case you run into any issues.  Reach us at ms-ivr-preview@microsoft.com or submit an issue [here](https://github.com/microsoft/botframework-telephony/issues).

Refer to our [roadmap](roadmap.md) for timeline of individual feature and General Availability.

---

# Getting started

Are you ready to build a bot that answers phone calls? Follow these four easy steps:

* [Step 1: Create a new bot](CreateBot.md). You can skip this step if you already have a working bot.
* [Step 2: Get a phone number](https://docs.microsoft.com/en-us/azure/communication-services/quickstarts/create-communication-resource). At this step you will create an instance of the Azure Communication Services Resource and acquired a phone number in it.
* [Step 3: Create a speech resource](CreateSpeechResource.md). You need an instance of the Speech Service to convert speech to text (for messages _received_ by the bot) and text to speech (for messages _sent_ by the bot)
* [Step 4: Configure the Telephony channel](EnableTelephony.md). This step connects it all together.

Once setup, you should be able to dial the acquired phone number using any phone and hear your bot echo your voice. If you hear that, congratulations! You're ready for dive deeper for more advanced features.

# Advanced features

While many bots can be Telephony-enabled by the steps above, you can build more effective bots by taking advantage of telephony and speech-specific capabilities as described below.

* [Custom speech inside of the bot](ProcessSpeechInBotCode.md)
* [Transfer call to an agent](TransferCallOut.md)
* [Terminating a call](TerminateCall.md)
* [DTMF](DTMF.md)
* [Troubleshooting](TroubleshootingTelephonyBot.md)

<!-- backup

* [Step 2: Provision a new phone number for your bot in Azure Communication Services](https://docs.microsoft.com/en-us/azure/communication-services/quickstarts/telephony-sms/get-phone-number) 


-->
