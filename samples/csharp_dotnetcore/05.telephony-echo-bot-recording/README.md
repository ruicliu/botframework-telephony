# Telephony EchoBot with call recording

Bot Framework v4 telephony echo bot sample with call recording.

## To try this sample

### Create a Telephony bot and deploy to Azure

To get started with this step see [Build a bot that answers phone calls](https://github.com/microsoft/botframework-telephony/blob/main/README.md#documentation-and-samples)

### Publish the echo bot with recording enabled

    ```bash
    git clone https://github.com/microsoft/botframework-telephony.git
    ```
    * Open samples\csharp_dotnetcore\05.telephony-call-recording-echo\telephony-echo-recording.sln in Visual Studio
    * Build and publish telephony-echo-bot-recording to the telephony bot above
    * This bot should now be able to recording incoming calls

### Download call recording files

Azure Communication Services emit a RecordingFileStatusUpdated when a new recoding is available. 
The event is delivered via the Event Grid to the specified endpoint. See [Azure Communication Services as an Event Grid source](https://docs.microsoft.com/en-us/azure/event-grid/event-schema-communication-services?tabs=event-grid-event-schema)
This sample uses an Azure function to receive the RecordingFileStatusUpdated notification.

#### Make sure Event Grid is registered for the subscription: 
    * az provider show -n Microsoft.EventGrid
    * az provider register --namespace Microsoft.EventGrid

#### Choose a storage location to download the call recording files
This sample uses blob storage to illustrate the usage. 

    * Create a new Storage Container
    * Create a container to store the recording files. See [Create a container](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-portal#create-a-container)
    * Note the connection string to allow the Azure function to download recording files to this location.
    * Note the name of the container above.

#### Setup an endpoint to receive RecordingFileStatusUpdated events 
Event grid can deliver events to a variety of endpoints. For a complete list see [Event handlers in Azure Event Grid](https://docs.microsoft.com/en-us/azure/event-grid/event-handlers)
To learn about authenticating event delivery see [Authenticate event delivery to event handlers ](https://docs.microsoft.com/en-us/azure/event-grid/security-authentication)

This sample uses an Azure Function App to receive RecordingFileStatusUpdated events.

    * Create a new Azure Function App
    * Build and Publish telephony-recording-download-function as new function in the Function App above.
    * Set the following Configuration --> Application settings:
        - AcsEndpoint - Set this to the endpoint of your ACS resource.
        - RecordingContainerName - Set this to the blob container name where the recording files will be saved.

    * Set the following Configuration --> Connection strings:
        AcsAccessKey - Set this to the access key of your ACS resource.
        RecordingStorageConnectionString - Set this to the connection string of your recording storage account.

#### Subscribe the endpoint to receive RecordingFileStatusUpdated events 

The final step is to subscribe to the RecordingFileStatusUpdated event in the ACS resource
    * Go to your ACS resource --> Events
    * Create a new Event subscription (+ Event Subscription)
    * Choose a name and Filter to Event type `Recording File Status Updated(Preview)`
    * Set the endpoint Type as Azure Function and choose the function above.
    * Save the new event subscription

The Azure function should now be able to receive notifications and save the recording to the blob container as they become available.

## Further reading

- [Bot Framework Documentation](https://docs.botframework.com)
- [Bot Basics](https://docs.microsoft.com/azure/bot-service/bot-builder-basics?view=azure-bot-service-4.0)
- [Activity processing](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-concept-activity-processing?view=azure-bot-service-4.0)
- [Azure Bot Service Introduction](https://docs.microsoft.com/azure/bot-service/bot-service-overview-introduction?view=azure-bot-service-4.0)
- [Azure Bot Service Documentation](https://docs.microsoft.com/azure/bot-service/?view=azure-bot-service-4.0)
- [.NET Core CLI tools](https://docs.microsoft.com/en-us/dotnet/core/tools/?tabs=netcore2x)
- [Azure CLI](https://docs.microsoft.com/cli/azure/?view=azure-cli-latest)
- [Azure Portal](https://portal.azure.com)
- [Language Understanding using LUIS](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/)
- [Channels and Bot Connector Service](https://docs.microsoft.com/en-us/azure/bot-service/bot-concepts?view=azure-bot-service-4.0)
- [Restify](https://www.npmjs.com/package/restify)
- [dotenv](https://www.npmjs.com/package/dotenv)
