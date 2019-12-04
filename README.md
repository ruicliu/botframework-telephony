# Microsoft Bot Framework IVR

Telphony Channel in Microsoft Bot Framework is the Microsoft technology for enabling PSTN calling capabilites in a Bot. Telephony Channel allows you to build an IVR by leveraging Microsoft Teams Calling capabilities, along with the full power of Microsoft Azure Bot Framework and Microsoft Speech Services.

 ![](images/telephonychannel.png)

Please note:  this is a Beta (preview) version of software, and as with any preview software, there may be initial risks and limitations you run into, such as a need to integrate with your existing IVR, etc.  We are actively working on and supporting this product and are here to help you in case you run into any issues.  Reach us at ms-ivr-preview@microsoft.com.

## Requirements
* **IVR Private Preview Approval** - To get started, your Tenant/Organization needs to be approved for a Private Preview of the Microsoft Intelligent Call Center / IVR project.  Send the e-mail to ms-ivr-preview [AT] microsoft.com with following information
  * Tenant/Organization name
  * Azure account e-mail that should be whitelisted,
  * Description of the bot. 
  
  Once approved for privare preview, your customer will see Telephony channel in their bot settings. 
* **Office 365 License** - A minimum of Office 365 E3 + calling plan or an E5 plan is required 
* **Azure Subscription**

# Enabling IVR 

After getting approved into the private preview, overall setup should take roughly an hour to enable a basic IVR bot callable using a phone number.

The following are the high-level steps needed you to enable IVR support in your bot:

* [Optional: create a new Office 365 account trial](CreateOfficeTrial.md)
* [Provision a new phone number for your bot in Office 365](AcquirePhoneNumber.md)
   * [Acquire a phone number](AcquirePhoneNumber.md#Acquire-a-phone-number)
   * [Create a resource account](AcquirePhoneNumber.md#Create-a-resource-account)
   * [Assign license](AcquirePhoneNumber.md#Assign-license)
   * [Bind the phone number](AcquirePhoneNumber.md#Bind-the-phone-number)
* [Create a new bot](CreateBot.md)
* [Enable Telephony Channel](EnableTelephony.md)
   * [Create a Speech Services resource](EnableTelephony.md#Create-a-Speech-Services-resource)
   * [Enable Telephony Channel in the bot](EnableTelephony.md#Enable-Telephony-Channel-in-the-bot)

Once setup you should be able to simply dial the acquired phone number using any PSTN or mobile phone (subjected to cellular plan on the source phone) 
