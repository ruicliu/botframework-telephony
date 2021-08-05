using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using Azure.Communication.Sms;
using Azure.Communication;

namespace BotFramework.Telephony.Samples
{
    struct AcsConnectionParams
    {
        public string AcsEndPoint;
        public string AcsAccessKey;
    }

    public static class CallRecordingDownloader
    {
        private static readonly HttpClient client = new HttpClient();

        // Make Event Grid is registered for the subscription: 
        // az provider register --namespace Microsoft.EventGrid
        // az provider show -n Microsoft.EventGrid
        [FunctionName("CallRecordingDownloader")]
        public static async void Run(
            [EventGridTrigger] EventGridEvent eventGridEvent,
            ILogger log)
        {
            try
            {
                log.LogInformation("Received event : {0}", eventGridEvent.EventType);

                string json = JsonConvert.SerializeObject(eventGridEvent, Formatting.Indented);
                log.LogInformation(json);

                if (eventGridEvent.EventType == "Microsoft.Communication.RecordingFileStatusUpdated")
                {
                    if (eventGridEvent.Data == null)
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
                        string connStr = Environment.GetEnvironmentVariable("CUSTOMCONNSTR_RecordingStorageBlob", EnvironmentVariableTarget.Process);
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

                        // Create a new record in Redis for this recording

                        string lastIDKey = "LastID";
                        string lastIDValue = "";

                        string cacheConnection = Environment.GetEnvironmentVariable("CUSTOMCONNSTR_cacheConnection", EnvironmentVariableTarget.Process);
                        if (string.IsNullOrEmpty(cacheConnection))
                        {
                            log.LogInformation($"Missing Redis connection string '{cacheConnection}'");
                        }
                        else
                        {
                            ConnectionMultiplexer cm = ConnectionMultiplexer.Connect(cacheConnection);
                            IDatabase cache = cm.GetDatabase();

                            lastIDValue = cache.StringGet("LastID");
                            log.LogInformation($"{lastIDKey}={lastIDValue}");

                            var trans = cache.CreateTransaction();
                            var task = trans.StringIncrementAsync(lastIDKey);
                            if (await trans.ExecuteAsync().ConfigureAwait(false))
                            {
                                await task.ConfigureAwait(false);
                                log.LogInformation($"transaction completed");
                                lastIDValue = cache.StringGet(lastIDKey);
                                log.LogInformation($"After change: {lastIDKey}={lastIDValue}");

                                cache.StringSet(lastIDValue, recordingFileName);

                                log.LogInformation($"Mapping '{lastIDValue}' --> '{recordingFileName}' created");

                                var message = $"IVR Demo call record is available here: https://arturlcallrecordingdownloader.azurewebsites.net/api/Get?id={lastIDValue}";

                                string phoneNumberTo = "";
                                foreach(var participant in metadata.Participants)
                                {
                                    if (participant.ParticipantId.Contains("acs:")) continue; // this is not a human
                                    if (participant.ParticipantId.Length >= 12)
                                    {
                                        phoneNumberTo = participant.ParticipantId.Substring(participant.ParticipantId.Length - 12, 12);
                                        log.LogInformation($"Found target phone number: {phoneNumberTo}");
                                    }
                                }

                                if (string.IsNullOrEmpty(phoneNumberTo))
                                {
                                    log.LogError("Cannot extract target phone number");
                                }
                                else
                                {
                                    var acsParams = GetACSConnectionParameters(log);
                                    string acsconnectionString = $"endpoint=https://{acsParams.AcsEndPoint}/;accesskey={acsParams.AcsAccessKey}";

                                    string SMSSenderPhoneNumber = Environment.GetEnvironmentVariable("SMSSenderPhoneNumber", EnvironmentVariableTarget.Process);
                                    if (string.IsNullOrEmpty(SMSSenderPhoneNumber))
                                    {
                                        log.LogError($"SMSSenderPhoneNumber application setting not set, cannot send SMS");
                                    }
                                    else
                                    {
                                        var smsResponse = SendSMSAsync(acsconnectionString, SMSSenderPhoneNumber, phoneNumberTo, message); // TBD: parametrize phone number
                                        log.LogInformation($"Sent SMS to {phoneNumberTo}. Message: '{message}'");

                                        string QAOperatorPhoneNumber = Environment.GetEnvironmentVariable("QAOperatorPhoneNumber", EnvironmentVariableTarget.Process);
                                        if (!string.IsNullOrEmpty(QAOperatorPhoneNumber))
                                        {
                                            // Text a copy to QA operator for quality assurance purposes
                                            if (phoneNumberTo != QAOperatorPhoneNumber)
                                            {
                                                var smsResponse2 = SendSMSAsync(acsconnectionString, SMSSenderPhoneNumber, QAOperatorPhoneNumber, message);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                log.LogError($"transaction failed");
                            }
                            log.LogInformation($"Done");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError("Failed with {0}", ex.Message);
                log.LogError("Failed with {0}", ex.InnerException.Message);
            }
        }

        private static Azure.Response<SendSmsResponse> SendSMSAsync(string acsConnectionString, string phoneNumberFrom, string phoneNumberTo, string message)
        {
            var smsClient = new SmsClient(acsConnectionString,
                                    new SmsClientOptions(SmsClientOptions.ServiceVersion.V1));

            var sendSmsResponse = smsClient.Send(
                new PhoneNumber(phoneNumberFrom),
                new PhoneNumber(phoneNumberTo),
                message,
                new SendSmsOptions()
                {
                    EnableDeliveryReport = true
                });

            return sendSmsResponse;
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

        private static void AddHmacHeaders(
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

        private static AcsConnectionParams GetACSConnectionParameters(ILogger log)
        {
            string acsEndPoint = Environment.GetEnvironmentVariable("AcsEndpoint", EnvironmentVariableTarget.Process);
            if(string.IsNullOrEmpty(acsEndPoint))
            {
                string errorMsg = $"Error: AcsEndpoint not set in the Azure Function Application settings. Cannot continue";
                log.LogError(errorMsg);
                throw new Exception(errorMsg);
            }

            string acsAccessKey = Environment.GetEnvironmentVariable("CUSTOMCONNSTR_AcsAccessKey", EnvironmentVariableTarget.Process);
            if (string.IsNullOrEmpty(acsAccessKey))
            {
                string errorMsg = $"Error: acsAccessKey not set in the Azure Function Connection strings. Cannot continue";
                log.LogError(errorMsg);
                throw new Exception(errorMsg);
            }

            return new AcsConnectionParams { AcsEndPoint = acsEndPoint, AcsAccessKey = acsAccessKey };
        }

        private static async Task<string> GetDownloadMetadata(
            string documentId,
            ILogger log)
        {
            string downloadMetadataUrl = "https://{0}/recording/download/{1}/metadata?api-version=2021-04-15-preview1";

            var acsParams = GetACSConnectionParameters(log);

            // Download metadata for chunk
            var downloadMetadataUrlForChunk = String.Format(downloadMetadataUrl, acsParams.AcsEndPoint, documentId);

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(downloadMetadataUrlForChunk),
                Content = null // content if required for POST methods
            };

            // Hash the content of the request.
            var contentHashed = CreateContentHash(string.Empty);

            // Add HMAC headers.
            AddHmacHeaders(request, contentHashed, acsParams.AcsAccessKey, HttpMethod.Get);
            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                log.LogError("Failed to download metadata. StatusCode: " + response.StatusCode);
                return null;
            }

            var metadataString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            log.LogInformation(metadataString);

            return metadataString;
        }

        private static async Task<Stream> DownloadChunk(RecordingMetadata metadata, ILogger log)
        {
            string downloadRecordingUrl = "https://{0}/recording/download/{1}?api-version=2021-04-15-preview1";
            string acsEndPoint = Environment.GetEnvironmentVariable("AcsEndpoint", EnvironmentVariableTarget.Process);
            string acsAccessKey = Environment.GetEnvironmentVariable("CUSTOMCONNSTR_AcsAccessKey", EnvironmentVariableTarget.Process);

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

            if (!response.IsSuccessStatusCode)
            {
                log.LogError("Failed to download document. StatusCode: " + response.StatusCode);
                return null;
            }

            var contentStream = await response.Content.ReadAsStreamAsync();
            return contentStream;
        }
    }
}
