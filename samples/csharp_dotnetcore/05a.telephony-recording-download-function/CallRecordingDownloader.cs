using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BotFramework.Telephony.Samples
{
    public static class CallRecordingDownloader
    {
        private static HttpClient client = new HttpClient();

        // Make sure Event Grid is registered for the subscription: 
        // az provider register --namespace Microsoft.EventGrid
        // az provider show -n Microsoft.EventGrid
        [FunctionName("CallRecordingDownloader")]
        public static async void Run([EventGridTrigger]EventGridEvent eventGridEvent, ILogger log)
        {
            try
            {
                log.LogInformation($"Received {eventGridEvent.EventType} event");

                if (eventGridEvent.EventType == DownloadRecordingConstants.RecordingFileStatusUpdated)
                {
                    if(eventGridEvent.Data == null)
                    {
                        log.LogError("RecordingFileStatusUpdated received with invalid data.");
                        return;
                    }

                    var eventData = ((JObject)(eventGridEvent.Data)).ToObject<RecordingFileStatusUpdatedEventData>();

                    log.LogInformation($"Recording Notification received");
                    log.LogInformation($"Recording Start: {eventData.RecordingStartTime} Duration : {eventData.RecordingDurationMs}");
                    log.LogInformation($"#Chunks: {eventData.RecordingStorageInfo.RecordingChunks.Length}");
                    log.LogInformation($"Recording Session End Reason: {eventData.SessionEndReason}");

                    // Prepare storage blob to store recording files
                    var recordingContainer = await GetRecordingContainer(log).ConfigureAwait(false);

                    // Download each recording chunk
                    foreach (var chunk in eventData.RecordingStorageInfo.RecordingChunks)
                    {
                        log.LogInformation($"Downloading chunk {chunk.DocumentId}");

                        // Download metadata for chunk to storage
                        var metadata = await DownloadChunkMetadata(chunk.DocumentId, recordingContainer, log).ConfigureAwait(false);
                        if (metadata == null)
                        {
                            log.LogError($"Failed to download metadata for {chunk.DocumentId}");
                            return;
                        }

                        // Download recording file to storage
                        await DownloadChunk(metadata, recordingContainer, log).ConfigureAwait(false);}
                }
            }
            catch(Exception ex)
            {
                log.LogError("Failed with {0}", ex.Message);
                log.LogError("Failed with {0}", ex.InnerException.Message);
            }
        }

        private static async Task<RecordingMetadata> DownloadChunkMetadata(string documentId, CloudBlobContainer container, ILogger log)
        {
            var acsEndpoint = new Uri(Environment.GetEnvironmentVariable("AcsEndpoint", EnvironmentVariableTarget.Process));
            var acsAccessKey = Environment.GetEnvironmentVariable("CUSTOMCONNSTR_AcsAccessKey", EnvironmentVariableTarget.Process);

            var downloadMetadataUrlForChunk = string.Format("recording/download/{0}/metadata?api-version=2021-04-15-preview1", documentId);
            var downloadMetadataUrl = new Uri(acsEndpoint, downloadMetadataUrlForChunk);

            using (var request = new HttpRequestMessage(HttpMethod.Get, downloadMetadataUrl))
            {
                request.Content = null; // content required for POST methods
                AddHmacHeaders(request, CreateContentHash(string.Empty), acsAccessKey);

                using (var response = await client.SendAsync(request))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        log.LogError($"Failed to download metadata. StatusCode: {response.StatusCode}");
                        return null;
                    }

                    var metadataString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    log.LogInformation(metadataString);

                    var metadata = JsonConvert.DeserializeObject<RecordingMetadata>(metadataString);
                    log.LogInformation($"CallId: {metadata.CallId}");

                    var metadataFile = string.Format("{0}_metadata.json", metadata.CallId);
                    var metadataBlob = container.GetBlockBlobReference(metadataFile);
                    await metadataBlob.UploadTextAsync(metadataString).ConfigureAwait(false);
                    log.LogInformation("Saved call metadata successfully");

                    return metadata;
                }
            }
        }

        private static async Task DownloadChunk(RecordingMetadata metadata, CloudBlobContainer container, ILogger log)
        {
            var acsEndpoint = new Uri(Environment.GetEnvironmentVariable("AcsEndpoint", EnvironmentVariableTarget.Process));
            var acsAccessKey = Environment.GetEnvironmentVariable("CUSTOMCONNSTR_AcsAccessKey", EnvironmentVariableTarget.Process);

            var downloadUrlForChunk = string.Format("recording/download/{0}?api-version=2021-04-15-preview1", metadata.ChunkDocumentId);  
            var downloadRecordingUrl = new Uri(acsEndpoint, downloadUrlForChunk);

            using (var request = new HttpRequestMessage(HttpMethod.Get, downloadRecordingUrl))
            {
                request.Content = null; // content required for POST methods
                
                AddHmacHeaders(request, CreateContentHash(string.Empty), acsAccessKey);

                using (var response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        log.LogError($"Failed to download document. StatusCode: {response.StatusCode}");
                        return;
                    }

                    var recordingStream = await response.Content.ReadAsStreamAsync();
                    var recordingFile = string.Format("{0}.{1}", metadata.CallId, metadata.RecordingInfo.Format);
                    var recordingBlob = container.GetBlockBlobReference(recordingFile);
                    await recordingBlob.UploadFromStreamAsync(recordingStream).ConfigureAwait(false);
                    log.LogInformation("Saved recording successfully");
                }
            }
        }

        private static async Task<CloudBlobContainer> GetRecordingContainer(ILogger log)
        {
            var blobConnStr = Environment.GetEnvironmentVariable("CUSTOMCONNSTR_RecordingStorageConnectionString", EnvironmentVariableTarget.Process);
            var recordingContainer = Environment.GetEnvironmentVariable("RecordingContainerName", EnvironmentVariableTarget.Process);

            var storageAccount = CloudStorageAccount.Parse(blobConnStr);
            var blobClient = storageAccount.CreateCloudBlobClient();

            log.LogInformation($"recordingContainer: {recordingContainer}");
            var container = blobClient.GetContainerReference(recordingContainer);
            await container.CreateIfNotExistsAsync();

            log.LogInformation($"Recording will be downloaded to container : {recordingContainer}");
            return container;
        }

        private static string CreateContentHash(string content)
        {
            var alg = SHA256.Create();

            using (var memoryStream = new MemoryStream())
            using (var contentHashStream = new CryptoStream(memoryStream, alg, CryptoStreamMode.Write))
            {
                using (var swEncrypt = new StreamWriter(contentHashStream))
                {
                    if (content != null)
                    {
                        swEncrypt.Write(content);
                    }
                }
            }

            return Convert.ToBase64String(alg.Hash);
        }

        private static void AddHmacHeaders(HttpRequestMessage request, string contentHash, string accessKey)
        {
            var utcNowString = DateTimeOffset.UtcNow.ToString("r", CultureInfo.InvariantCulture);
            var uri = request.RequestUri;
            var host = uri.Authority;
            var pathAndQuery = uri.PathAndQuery;

            var stringToSign = $"{request.Method}\n{pathAndQuery}\n{utcNowString};{host};{contentHash}";
            var hmac = new HMACSHA256(Convert.FromBase64String(accessKey));
            var hash = hmac.ComputeHash(Encoding.ASCII.GetBytes(stringToSign));
            var signature = Convert.ToBase64String(hash);
            var authorization = $"HMAC-SHA256 SignedHeaders=date;host;x-ms-content-sha256&Signature={signature}";

            request.Headers.Add("x-ms-content-sha256", contentHash);
            request.Headers.Add("Date", utcNowString);
            request.Headers.Add("Authorization", authorization);
        }

    }
}
