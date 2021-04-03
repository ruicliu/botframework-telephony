# Configure telephony channels using Azure Command-Line interface

Telephony channel configuration process can be automated using [Azure CLI](https://docs.microsoft.com/cli/azure). Here you will learn how to configure a Telephony channel with one or several phone numbers associated with it. Bots and other channels can also be created using Azure CLI, refer to the documentation for details.

## Prerequisites

First, you need to install the Azure CLI as described [here](https://docs.microsoft.com/cli/azure/install-azure-cli). 

Next, log in to Azure using the account you want to use:

```Powershell
az login 
```

After that, you will need to prepare the following Azure resources:

- Your bot
- An instance of Azure Communication Service
- An instance of Cognitive Services resource

(This process is explained in detail [here](EnableTelephony.md).)

## ARM template

Copy the following script into a file called `template.json`. This file is called the ARM template, and it defines the properties of the Telephony channel you want to create:

```JSON
{
    "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
    "contentVersion": "1.0.0.0",
    "parameters": {
        "botId": {
            "type": "String"
        },
        "phoneNumber1": {
            "type": "String"
        },
        "acsResourceId": {
            "type": "String"
        },
        "acsEndpoint": {
            "type": "String"
        },
        "acsSecret": {
            "type": "String"
        },
        "cognitiveServiceResourceId": {
            "type": "String"
        },
        "cognitiveServiceSubscriptionKey": {
            "type": "String"
        },
        "cognitiveServiceRegion": {
            "type": "String"
        }
    },
    "variables": {},
    "resources": [
        {
            "type": "Microsoft.BotService/botServices/channels",
            "apiVersion": "2020-06-02",
            "name": "[concat(parameters('botId'), '/TelephonyChannel')]",
            "location": "global",
            "properties": {
                "properties": {
                    "phoneNumbers": [
                        {
                            "phoneNumber": "[parameters('phoneNumber1')]",
                            "acsResourceId": "[parameters('acsResourceId')]",
                            "acsEndpoint": "[parameters('acsEndpoint')]",
                            "acsSecret": "[parameters('acsSecret')]",

                            "cognitiveServiceResourceId": "[parameters('cognitiveServiceResourceId')]",
                            "cognitiveServiceSubscriptionKey": "[parameters('cognitiveServiceSubscriptionKey')]",
                            "cognitiveServiceRegion": "[parameters('cognitiveServiceRegion')]",
                            "defaultLocale": null
                        }
                    ],
                    "cognitiveServiceSubscriptionKey": "[parameters('cognitiveServiceSubscriptionKey')]",
                    "cognitiveServiceRegion": "[parameters('cognitiveServiceRegion')]",
                    "defaultLocale": null,
                    "premiumSKU": null
                },
                "channelName":"TelephonyChannel"
            }
        }
    ]
}
```

You will be using this file as one of the parameters in Azure CLI. 

## Parameters file

As you can see, the template file does not contain any actual data, such as the name of your bot or other resources. This information is provided in a different file, the `parameters.json`. Below is an example of such a file. You will need to replace the sample values with the actual values representing your resources.

```JavaScript
{
    "$schema": "https://schema.management.azure.com/schemas/2015-01-01/deploymentParameters.json#",
    "contentVersion": "1.0.0.0",
    "parameters": {
        "botId": {
            "value": "my-telephony-bot" // REPLACE: The id of your bot
        },
        "phoneNumber1": {
            "value": "+14254445555" // REPLACE: The phone number you want to connect to your bot. Read below on how to connect multiple phone numbers
        },
        "acsResourceId": {
            // REPLACE: The full resource Id of the Azure Communication Service. You can get this string
            // by navigating to the Overview pane of the resource in the Azure portal and clicking 'JSON View'
            "value": "/subscriptions/47397625-56fa-4c6b-9301-a7edddc893ed/resourceGroups/my-rg/providers/Microsoft.Communication/CommunicationServices/my-acs"
        },
        "acsEndpoint": {
            // REPLACE: The endpoint Url - this string always ends with 'communication.azure.com/'
            "value": "https://my-acs.communication.azure.com/"
        },
        "acsSecret": {
            "value": "..." // REPLACE: ACS key
        },
        "cognitiveServiceResourceId": {
            // REPLACE: The full resource Id of the Cognitive Services resource. You can get this string
            // by navigating to the Overview pane of the resource in the Azure portal and clicking 'JSON View'
            "value": "/subscriptions/47397625-56fa-4c6b-9301-a7edddc893ed/resourceGroups/my-rg/providers/Microsoft.CognitiveServices/accounts/my-cognitive-service"
        },
        "cognitiveServiceSubscriptionKey": {
            "value": "..." // REPLACE: Cognitive Services key
        },
        "cognitiveServiceRegion": {
            "value": "eastus" // REPLACE: The region of the Cognitive Services resource
        }
    }
}
```

Once you have filled out the `parameters.json` file with your data, validate your configuration by running the `az deployment group validate` command, replacing _your-subscription-id_ and _your-resource-group_ with the subscription and resource group of your bot:

```Powershell
az deployment group validate --subscription your-subscription-id -g your-resource-group --template-file template.json --parameters parameters.json
```

If the validation succeeds, you will receive a full ARM template populated with your resource names. At this point you can proceed to actually configuring the channel. You can do this by running `az deployment group create`:

```Powershell
az deployment group create --subscription your-subscription-id -g your-resource-group --template-file template.json --parameters parameters.json
```
If the process succeeds, you can navigate to the Azure portal's Channels blade and make sure the bot's Telephony channel is configured.

## Connecting multiple phone numbers to the bot

A bot can be associated with multiple phone numbers. To accomplish this, add another "phoneNumber" element `template.json` file as follows:

```JavaScript
{
    "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
    "contentVersion": "1.0.0.0",
    "parameters": {
        ... // skipped
        "phoneNumber1": {
            "type": "String"
        },
        "phoneNumber2": {
            "type": "String"
        },
        ... // skipped
    },
    "variables": {},
    "resources": [
        {
            "type": "Microsoft.BotService/botServices/channels",
            "apiVersion": "2020-06-02",
            "name": "[concat(parameters('botId'), '/TelephonyChannel')]",
            "location": "global",
            "properties": {
                "properties": {
                    "phoneNumbers": [
                        {
                            "phoneNumber": "[parameters('phoneNumber1')]",
                            ... // skipped
                        },
                        {
                            "phoneNumber": "[parameters('phoneNumber2')]",
                            ... // copied from phone number 1
                        }
                    ],
                    ... // skipped
                },
                "channelName":"TelephonyChannel"
            }
        }
    ]
}
```

You will need to introduce the second phone number parameter in the `parameters.json` file. 

Other phone numbers can use different ACS and Cognitive Service resources. In that case, you will also need to add additional parameters to the `parameters.json` file and use them in `template.json` file.
