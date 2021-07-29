using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Azure.Communication;
using Azure.Communication.Sms;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using NAudio.Wave;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using tiClient.TIServiceDependencies;
using TIClient.TIPipelineManagementHttpClient;

namespace BotFramework.Telephony.Samples
{
    class ProductSentiment
    {
        public string Product;
        public double Sentiment;
    }

    public class LinkEntity : TableEntity
    {
        // Set up Partition and Row Key information
        public LinkEntity(string pk, string rk)
        {
            this.PartitionKey = pk;
            this.RowKey = rk;
        }

        public LinkEntity() { }
        public string Product { get; set; }
        public double Sentiment { get; set; }
    }

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
        public static async Task Run(
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

                        log.LogInformation($"Analyzing sentiment...");
                        MemoryStream memStream = new MemoryStream();
                        await recordingStream.CopyToAsync(memStream);
                        recordingStream.Seek(0, SeekOrigin.Begin);
                        var productSentiment = await GetProductSentimentFromStreamAsync(memStream, log);
                        log.LogInformation($"Sentiment analysis done");

                        log.LogInformation($"Product = {productSentiment.Product}, Sentiment = {productSentiment.Sentiment}");
                        await AddRecordToDb(productSentiment.Product, productSentiment.Sentiment);

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

                                var message = $"Your call record is available here: https://arturlcallrecordingdownloader.azurewebsites.net/api/Get?id={lastIDValue}";

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
//                                        var smsResponse = SendSMSAsync(acsconnectionString, SMSSenderPhoneNumber, phoneNumberTo, message); // TBD: parametrize phone number
//                                        log.LogInformation($"Sent SMS to {phoneNumberTo}. Message: '{message}'");

                                        string QAOperatorPhoneNumber = Environment.GetEnvironmentVariable("QAOperatorPhoneNumber", EnvironmentVariableTarget.Process);
                                        if (!string.IsNullOrEmpty(QAOperatorPhoneNumber))
                                        {
                                            // Text a copy to QA operator for quality assurance purposes
                                            if (phoneNumberTo != QAOperatorPhoneNumber)
                                            {
//                                                var smsResponse2 = SendSMSAsync(acsconnectionString, SMSSenderPhoneNumber, QAOperatorPhoneNumber, message);
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

        static async Task<ProductSentiment> GetInsights(string wavFile, ILogger log)
        {
            ProductSentiment result = new ProductSentiment();
            double sentimentSum = 0.0;
            int countSentiments = 0;

            //Endpoints & Keys
            string pfSubscriptionKey = "b53e8279cd0f4b929743b346b01526ba";
            string pfRegion = "westcentralus";
            string pfEndpoint = $"https://{pfRegion}.orchestration.speech.microsoft.com";
            string signalREndpoint = $"{pfEndpoint}/signalrhub";
            string speechEndpoint = $"wss://{pfRegion}.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1?language=en-us&setfeature=projectfrankfurtspeech,multichannel2&initialSilenceTimeoutMs=600000&endSilenceTimeoutMs=600000";

            string pathAudioInput = wavFile;

            string binPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string pathPipeline = Path.Combine("c:\\home\\site\\wwwroot", @"AllPipeline.json"); // ugh....

            if(!File.Exists(pathPipeline))
            {
                log.LogError($"Error: file '{pathPipeline}' does not exist");
                throw new Exception("AllPipeline.json not found");
            }

            //Project Frankfurt Management API
            TIPipelineManagementHttpClient pipelineClient = new TIPipelineManagementHttpClient(pfEndpoint, pfSubscriptionKey);
            await pipelineClient.DeleteAllTriggersAsync(); // Start the sample with a clean (empty) set of triggers
            var pipelineId = await pipelineClient.CreatePipelineAsync(pathPipeline);    //Add Pipeline to Project Frankfurt resource
            var triggerExpr = $"contains($.PipelineId, '{pipelineId}')"; //Logical expression for when to trigger a given pipeline
            var triggerId = await pipelineClient.CreateTriggerAsync(triggerExpr, pipelineId);   //Add Trigger to Project Frankfurt resource

            //Connect to SignalR endpoint for Real Time insights
            var signalrConnection = new HubConnectionBuilder()
                .WithUrl(signalREndpoint, opt =>
                {
                    opt.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
                    opt.SkipNegotiation = true;
                    opt.Headers.Add("Ocp-Apim-Subscription-Key", pfSubscriptionKey);
                })
                .AddNewtonsoftJsonProtocol()
                .WithAutomaticReconnect()
                .Build();

            try
            {
                //Set Azure Speech Service endpoint
                var config = SpeechConfig.FromEndpoint(new Uri(speechEndpoint), pfSubscriptionKey);

                var productSentimentDict = new Dictionary<string, ProductSentiment>();

                //Set audio input file
                using (var audioInput = AudioConfig.FromWavFileInput(pathAudioInput))
                //Analyze input
                using (var recognizer = new SpeechRecognizer(config, audioInput))
                using (var connection = Connection.FromRecognizer(recognizer))
                {
                    var topicName = Guid.NewGuid().ToString();
                    var tiPayload = new
                    {
                        callerChannel = 1,  //Customer
                        calleeChannel = 0,  //Agent
                        userId = topicName, //Customer provided unique ID
                        CustomProperties = new { PipelineId = pipelineId } //Custom Property for Trigger expression
                    };

                    connection.SetMessageProperty("speech.context", "conversationMetadata", JsonConvert.SerializeObject(tiPayload));

                    //Consume insights
                    //var allInsights = new List<JObject>();
                    signalrConnection.On<JObject>(SignalRRouteConstants.InsightReceivingEndpoint, (msg) =>
                    {
                        var type = msg["Type"]?.Value<string>();
                        var PhraseId = msg["PhraseId"]?.Value<string>();
                        var SentimentScore = msg["SentimentScore"]?.Value<double>();
                        var HotPhrase = msg["HotPhrase"]?.Value<string>();

                        if (type == "SentimentInsight")
                        {
                            sentimentSum += SentimentScore.GetValueOrDefault();
                            countSentiments++;
                        }
                        else if (type == "HotPhraseInsight")
                        {
                            // The first one wins
                            if (string.IsNullOrEmpty(result.Product))
                            {
                                result.Product = HotPhrase;
                            }
                        }
                    });

                    //Start conversation Signal R connection
                    signalrConnection.On<SubscriptionMessage>(SignalRRouteConstants.SubscribedMessageReceivingEndpoint, (msg) =>
                    {
                        log.LogInformation($"Subscribed to conversation: {msg.UserId}");
                    });

                    await signalrConnection.StartAsync().ConfigureAwait(false);
                    await signalrConnection.InvokeAsync(SignalRRouteConstants.SubscribeToCall, new SubscriptionMessage(topicName)).ConfigureAwait(false);

                    recognizer.SessionStarted += (s, e) =>
                    {
                        // This ID can be used to investigate service logs for any issues
                        log.LogInformation($"Session ID: {e.SessionId}");
                    };

                    var completedTcs = new TaskCompletionSource<int>();
                    recognizer.SessionStopped += async (s, e) =>
                    {
                        log.LogInformation("Waiting...");
                        // Wait a little while for all insights to flow in.
                        //await Task.Delay(5000);
                        log.LogInformation("Done waiting");

                        // Save the raw insights
                        //var outDir = Path.Combine(binPath, "output");
                        //Directory.CreateDirectory(outDir);

                        //var insightsFilePath = Path.Combine(
                        //    outDir,
                        //    $"{Path.GetFileNameWithoutExtension(pathAudioInput)}-{Path.GetFileNameWithoutExtension(pathPipeline)}-insights.json");

                        //File.WriteAllText(insightsFilePath, JsonConvert.SerializeObject(allInsights, Formatting.Indented));

                        // Print the transcript with key insights shown inline
                        // Utilitites.DisplayTranscriptWithInsights(allInsights, tiPayload.callerChannel, tiPayload.calleeChannel);

                        //Clean Up Project Frankfurt Management API
                        await pipelineClient.DeletePipelineAsync(pipelineId); //Delete Pipeline to Project Frankfurt resource
                        await pipelineClient.DeleteTriggerAsync(triggerId);   //Delete Trigger to Project Frankfurt resource

                        completedTcs.SetResult(0);
                    };

                    log.LogInformation("Starting reco...");

                    await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                    log.LogInformation("Done reco");

                    await completedTcs.Task;

                    log.LogInformation("Done session stopped");

                    result.Sentiment = sentimentSum / countSentiments;
                    return result;
                }
            }
            catch (Exception ex)
            {
                // If you get a 404 error, try again. Known issue.
                log.LogWarning($"Exception {ex}");
                log.LogWarning(ex.Message);
                throw;
            }
            finally
            {
                await signalrConnection.StopAsync();
            }
        }

        static async Task<ProductSentiment> GetProductSentimentFromStreamAsync(Stream stream, ILogger log)
        {
            string tmpMp4FileName = null;
            string tmpWavFileName = null;

            try
            {
                tmpMp4FileName = Path.GetTempFileName();
                tmpMp4FileName = Path.ChangeExtension(tmpMp4FileName, "mp4");
                using (var fileStream = File.Create(tmpMp4FileName))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(fileStream);
                }

                tmpWavFileName = Path.GetTempFileName();
                tmpWavFileName = Path.ChangeExtension(tmpWavFileName, "wav");
                using (var reader = new MediaFoundationReader(tmpMp4FileName))
                {
                    WaveFileWriter.CreateWaveFile(tmpWavFileName, reader);
                }

                return await GetInsights(tmpWavFileName, log);
            }
            finally
            {
                if (tmpMp4FileName != null) File.Delete(tmpMp4FileName);
                if (tmpWavFileName != null) File.Delete(tmpWavFileName);
            }
        }

        static readonly Random r = new Random();
        public static async Task AddRecordToDb(string product, double sentiment)
        {
            CloudStorageAccount storageAccount =
            new CloudStorageAccount(new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials("recordingti", "JHUCnysRZdq3hWi/R7acdv+Q1iIFy6YArgcZ2kf6A3r5OCoUAOFhsvgNnwkM9vtIsgLJqRrWZWOYTQ0ajwgjmA=="),
                true);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            CloudTable _linkTable = tableClient.GetTableReference("products");

            // Create a new entity.
            LinkEntity link = new LinkEntity("p1", r.Next().ToString())
            {
                Product = product,
                Sentiment = sentiment
            };

            // Create the TableOperation that inserts the customer entity.
            TableOperation insertOperation = TableOperation.InsertOrMerge(link);

            await _linkTable.ExecuteAsync(insertOperation);
        }

    }
}
