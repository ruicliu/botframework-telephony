using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.IO;
using System;
using System.Net.Http;
using System.Globalization;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;  
using System.Threading.Tasks;  

namespace BotFramework.Telephony.Samples
{       
    public static class CallRecordingDownloader
    {
        
        private static HttpClient client = new HttpClient();      

        // Make Event Grid is registered for the subscription: 
        // az provider register --namespace Microsoft.EventGrid
        // az provider show -n Microsoft.EventGrid
        [FunctionName("CallRecordingDownloader")]
        public static async void Run(
            [EventGridTrigger]EventGridEvent eventGridEvent, 
            ILogger log)
        {
            try
            {
                log.LogInformation("Received event : {0}", eventGridEvent.EventType);

                if ( eventGridEvent.EventType == "Microsoft.Communication.RecordingFileStatusUpdated")
                {
                    if(eventGridEvent.Data == null)
                    {
                        log.LogInformation("Received invalid event data");
                        return;
                    }               

                    RecordingFileStatusUpdatedEventData eventData = ((JObject)(eventGridEvent.Data)).ToObject<RecordingFileStatusUpdatedEventData>();

                    foreach (var chunk in eventData.RecordingStorageInfo.RecordingChunks)
                    {
                        // Download metadata for chunk
                        var metadataString = await GetDownloadMetadata(chunk.DocumentId, log).ConfigureAwait(false);
                        if (metadataString == null)
                        {
                            log.LogInformation("Invalid metadata");
                            return;
                        }
                        
                        RecordingMetadata metadata = JsonConvert.DeserializeObject<RecordingMetadata>(metadataString);

                        var recordingStream = await DownloadChunk(metadata, log).ConfigureAwait(false);
                        if (recordingStream == null)
                        {
                            log.LogInformation("Invalid recording stream");
                            return;
                        }

                        // Save to storage blob
                        string connStr = Environment.GetEnvironmentVariable("CUSTOMCONNSTR_RecordingStorageBlob",EnvironmentVariableTarget.Process);
                        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connStr);
                        
                        CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                        CloudBlobContainer container = blobClient.GetContainerReference("recordings");

                        string recordingMetadataFileName = String.Format("{0}_metadata.json", metadata.CallId);
                        CloudBlockBlob metadataBlob = container.GetBlockBlobReference(recordingMetadataFileName);
                        await metadataBlob.UploadTextAsync(metadataString).ConfigureAwait(false);      

                        string recordingFileName = String.Format("{0}.{1}", metadata.CallId, metadata.RecordingInfo.Format);
                        CloudBlockBlob downloadBlob = container.GetBlockBlobReference(recordingFileName);
                        await downloadBlob.UploadFromStreamAsync(recordingStream).ConfigureAwait(false);  

                        log.LogInformation("Saved recording successfully");
                    }
                }
            }
            catch(Exception ex)
            {
                log.LogInformation("Failed with {0}", ex.Message);
                log.LogInformation("Failed with {0}", ex.InnerException.Message);
            }
        }

        public static string CreateContentHash(string content)
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

        public static void AddHmacHeaders(
            HttpRequestMessage requestMessage, string contentHash, string accessKey, HttpMethod method)
        {
            var utcNowString = DateTimeOffset.UtcNow.ToString("r", CultureInfo.InvariantCulture);
            var uri = requestMessage.RequestUri;
            var host = uri.Authority;
            var pathAndQuery = uri.PathAndQuery;

            var stringToSign = $"{method.Method}\n{pathAndQuery}\n{utcNowString};{host};{contentHash}";
            var hmac = new HMACSHA256(Convert.FromBase64String(accessKey));
            var hash = hmac.ComputeHash(Encoding.ASCII.GetBytes(stringToSign));
            var signature = Convert.ToBase64String(hash);
            var authorization = $"HMAC-SHA256 SignedHeaders=date;host;x-ms-content-sha256&Signature={signature}";

            requestMessage.Headers.Add("x-ms-content-sha256", contentHash);
            requestMessage.Headers.Add("Date", utcNowString);
            requestMessage.Headers.Add("Authorization", authorization);
        }

        public static async Task<string> GetDownloadMetadata(
            string documentId, 
            ILogger log)        
        {
            string downloadMetadataUrl = "https://{0}/recording/download/{1}/metadata?api-version=2021-04-15-preview1";

            string acsEndPoint = Environment.GetEnvironmentVariable("AcsEndpoint",EnvironmentVariableTarget.Process);
            string acsAccessKey = Environment.GetEnvironmentVariable("CUSTOMCONNSTR_AcsAccessKey",EnvironmentVariableTarget.Process);

            // Download metadata for chunk
            var downloadMetadataUrlForChunk = String.Format(downloadMetadataUrl, acsEndPoint, documentId);

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(downloadMetadataUrlForChunk),
                Content = null // content if required for POST methods
            };

            // Hash the content of the request.
            var contentHashed = CreateContentHash(string.Empty);

            // Add HMAC headers.
            AddHmacHeaders(request, contentHashed, acsAccessKey, HttpMethod.Get);
            var response = await client.SendAsync(request);                

            if(!response.IsSuccessStatusCode)
            {
                log.LogInformation("Failed to download metadata. StatusCode: " + response.StatusCode);
                return null;
            }

            var metadataString = await response.Content.ReadAsStringAsync().ConfigureAwait(false); 
            log.LogInformation(metadataString);

            return metadataString;
        }

        public static async Task<Stream> DownloadChunk(RecordingMetadata metadata, ILogger log)
        {
            string downloadRecordingUrl = "https://{0}/recording/download/{1}?api-version=2021-04-15-preview1";  
            string acsEndPoint = Environment.GetEnvironmentVariable("AcsEndpoint",EnvironmentVariableTarget.Process);
            string acsAccessKey = Environment.GetEnvironmentVariable("CUSTOMCONNSTR_AcsAccessKey",EnvironmentVariableTarget.Process);
            
            var downloadUrlForChunk = String.Format(downloadRecordingUrl, acsEndPoint, metadata.ChunkDocumentId);               
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(downloadUrlForChunk),
                Content = null // content if required for POST methods
            };

            // Hash the content of the request.
            var contentHashed = CreateContentHash(string.Empty);

            // Add HMAC headers.
            AddHmacHeaders(request, contentHashed, acsAccessKey, HttpMethod.Get);

            // Make a request to the ACS apis mentioned above                    
            var response = await client.SendAsync(request).ConfigureAwait(false);
            log.LogInformation("Download recording statusCode: {0}", response.StatusCode);     

            if(!response.IsSuccessStatusCode)
            {
                log.LogInformation("Failed to download document. StatusCode: " + response.StatusCode);
                return null;
            }

            var contentStream = await response.Content.ReadAsStreamAsync();
            return contentStream;
        }
    }
}
