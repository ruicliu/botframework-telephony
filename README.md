# Building an IVR with Microsoft Bot Framework 

Telephony channel in Microsoft Bot Framework is the Microsoft technology for enabling PSTN calling capabilites in a Bot. Telephony channel allows you to build an IVR by leveraging Azure Communication Services phone numbers, along with the full power of Microsoft Azure Bot Framework and Microsoft Speech Services.

 ![](images/telephonychannel.png)

Please note:  This is a Beta (preview) version of software, and as with any preview software, there may be initial risks and limitations you run into, such as a need to integrate with your existing IVR, etc.  We are actively working on and supporting this product and are here to help you in case you run into any issues.  Reach us at ms-ivr-preview@microsoft.com.

**General Availability**:  due to the shifting impact of COVID-19, we have not set the GA date yet and are instead working with private preview customers directly on their IVR's.

## Requirements

* **IVR Private Preview Approval** - To get started, your Tenant/Organization needs to be approved for a Private Preview of the Microsoft Intelligent Call Center / IVR project.  Good candidates for the preview are existing Bot Framework customers with existing production bots and a dedicated Microsoft account manager (including MSC/CSA). Please have your Microsoft account manager send an e-mail to ms-ivr-preview@microsoft.com from their @microsoft.com e-mail address with following information:
  * Organization/company name
  * Is experienced with Bot Framework and has existing bots in production? Yes/No
  * Azure account e-mail that should be enabled for preview
  * Detailed description of the bot (please include a specific use case plus an overview summary "transcipt" outline of an expected call flow)
  
We are not approving general "evaluation" requests. Please only submit requests if you have actual target users who would be able to dial your IVR.

Once approved for privare preview, the Azure account provided will see Telephony channel in their bot settings. 
* **Azure Subscription** - You will need a valid, non-trial Azure subscription. Currently, only subscriptions with the US billing address are eligible.

# Enabling IVR 

After getting approved into the private preview, overall setup should take roughly an hour to enable a basic IVR bot callable using a phone number.

The following are the high-level steps needed you to enable IVR support in your bot:

* [Step 1: Create an Azure Communication Services Resource](https://docs.microsoft.com/en-us/azure/communication-services/quickstarts/create-communication-resource)
* [Step 2: Provision a new phone number for your bot in Azure Communication Services](https://docs.microsoft.com/en-us/azure/communication-services/quickstarts/telephony-sms/get-phone-number)
* [Step 3: Create a new Azure Web App Bot](CreateBot.md)
* [Step 4: Enable your bot to speak and understand voice](CreateSpeechResource.md)
* [Step 5: Enable Telephony Channel](EnableTelephony.md)
* [Step 6: Process speech inside of the bot](ProcessSpeechInBotCode.md)
* [Step 7: Transfer call to an agent](TransferCallOut.md)
* [Step 8: Terminating the ongoing call](TerminateCall.md)
* [Troubleshooting](TroubleshootingTelephonyBot.md)

Once setup, you should be able to simply dial the acquired phone number using any PSTN or mobile phone (subjected to cellular plan on the source phone).
