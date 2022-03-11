using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.Extensions.Logging;
using Azure.Communication.CallingServer;

namespace BotFramework.Telephony.Samples
{
    public static class CallRecordingDownloader
    {
        private static HttpClient client = new HttpClient();

        // Make sure Event Grid is registered for the subscription: 
        // az provider register --namespace Microsoft.EventGrid
        // az provider show -n Microsoft.EventGrid
        [FunctionName("CallRecordingDownloader")]
        public static async void Run([EventGridTrigger] EventGridEvent eventGridEvent, ILogger log)
        {
            try
            {
                log.LogInformation($"Received {eventGridEvent.EventType} event");

                if (eventGridEvent.EventType == SystemEventNames.AcsRecordingFileStatusUpdated)
                {
                    if (eventGridEvent.Data == null)
                    {
                        log.LogError("RecordingFileStatusUpdated received with invalid data.");
                        return;
                    }

                    var eventData = eventGridEvent.Data.ToObjectFromJson<AcsRecordingFileStatusUpdatedEventData>();

                    log.LogInformation($"Recording Notification received");
                    log.LogInformation($"Recording Start: {eventData.RecordingStartTime} Duration : {eventData.RecordingDurationMs}");
                    log.LogInformation($"#Chunks: {eventData.RecordingStorageInfo.RecordingChunks.Count}");
                    log.LogInformation($"Recording Session End Reason: {eventData.SessionEndReason}");

                    // Prepare ACS calling server client
                    var acsEndpoint = new Uri(Environment.GetEnvironmentVariable("AcsEndpoint", EnvironmentVariableTarget.Process));
                    var acsAccessKey = Environment.GetEnvironmentVariable("CUSTOMCONNSTR_AcsAccessKey", EnvironmentVariableTarget.Process);
                    var acsConnectionString = string.Format("endpoint={0};accesskey={1}", acsEndpoint, acsAccessKey);
                    CallingServerClient callingServerClient = new CallingServerClient(acsConnectionString);

                    // Download each recording chunk
                    foreach (var chunk in eventData.RecordingStorageInfo.RecordingChunks)
                    {
                        log.LogInformation($"Downloading chunk {chunk.DocumentId}");

                        // Download metadata for chunk to storage
                        log.LogInformation("Start processing metadata -- >");

                        await ProcessFile(
                            callingServerClient,
                            eventData.RecordingStorageInfo.RecordingChunks[0].MetadataLocation,
                            eventData.RecordingStorageInfo.RecordingChunks[0].DocumentId,
                            "json",
                            "metadata",
                            log);

                        // Download chunk to storage
                        log.LogInformation("Start processing recorded media -- >");

                        await ProcessFile(
                            callingServerClient,
                            eventData.RecordingStorageInfo.RecordingChunks[0].ContentLocation,
                            eventData.RecordingStorageInfo.RecordingChunks[0].DocumentId,
                            "mp3",
                            "recording",
                            log);
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError("Failed with {0}", ex.Message);
                log.LogError("Failed with {0}", ex.InnerException.Message);
            }
        }

        private static async Task<bool> ProcessFile(
            CallingServerClient callingServerClient,
            string downloadLocation,
            string documentId,
            string fileFormat,
            string downloadType,
            ILogger log)
        {
            var downloadUri = new Uri(downloadLocation);
            var downloadStream = callingServerClient.DownloadStreamingAsync(downloadUri).Result;

            log.LogInformation($"Download {downloadType} response  -- >" + downloadStream.GetRawResponse());
            log.LogInformation($"Save downloaded {downloadType} -- >");

            // Prepare storage blob to store recording files
            var blobContainerName = Environment.GetEnvironmentVariable("RecordingContainerName", EnvironmentVariableTarget.Process);
            var blobConnectionString = Environment.GetEnvironmentVariable("CUSTOMCONNSTR_RecordingStorageConnectionString", EnvironmentVariableTarget.Process);

            log.LogInformation($"Starting to upload {downloadType} to BlobStorage into container -- > {blobContainerName}");

            string blobName = documentId + "." + fileFormat;
            var blobStorageHelperInfo = await BlobStorageHelper.UploadFileAsync(blobConnectionString, blobContainerName, blobName, downloadStream);
            if (blobStorageHelperInfo.Status)
            {
                log.LogInformation(blobStorageHelperInfo.Message);
            }
            else
            {
                log.LogError($"{downloadType} file was not uploaded,{blobStorageHelperInfo.Message}");
            }

            return true;
        }
    }
}
