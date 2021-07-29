using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.DependencyInjection;
using NAudio.Wave;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using tiClient.TIServiceDependencies;
using TIClient.TIPipelineManagementHttpClient;

namespace CallAnalyzer
{
    class ProductSentiment
    {
        public string Product;
        public double Sentiment;
    }

    class Program
    {
        static async Task<ProductSentiment> GetInsights(string wavFile)
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
            string pathPipeline = Path.Combine(binPath, @"AllPipeline.json");

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
                        Console.WriteLine($"Subscribed to conversation: {msg.UserId}");
                    });

                    await signalrConnection.StartAsync().ConfigureAwait(false);
                    await signalrConnection.InvokeAsync(SignalRRouteConstants.SubscribeToCall, new SubscriptionMessage(topicName)).ConfigureAwait(false);

                    recognizer.SessionStarted += (s, e) =>
                    {
                        // This ID can be used to investigate service logs for any issues
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.Write($"Session ID: {e.SessionId}");
                        Console.ResetColor();
                        Console.WriteLine();
                    };

                    var completedTcs = new TaskCompletionSource<int>();
                    recognizer.SessionStopped += async (s, e) =>
                    {
                        // Wait a little while for all insights to flow in.
                        await Task.Delay(5000);

                        // Save the raw insights
                        var outDir = Path.Combine(binPath, "output");
                        Directory.CreateDirectory(outDir);

                        var insightsFilePath = Path.Combine(
                            outDir,
                            $"{Path.GetFileNameWithoutExtension(pathAudioInput)}-{Path.GetFileNameWithoutExtension(pathPipeline)}-insights.json");

                        //File.WriteAllText(insightsFilePath, JsonConvert.SerializeObject(allInsights, Formatting.Indented));

                        // Print the transcript with key insights shown inline
                        // Utilitites.DisplayTranscriptWithInsights(allInsights, tiPayload.callerChannel, tiPayload.calleeChannel);

                        //Clean Up Project Frankfurt Management API
                        await pipelineClient.DeletePipelineAsync(pipelineId); //Delete Pipeline to Project Frankfurt resource
                        await pipelineClient.DeleteTriggerAsync(triggerId);   //Delete Trigger to Project Frankfurt resource

                        completedTcs.SetResult(0);
                    };

                    await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
                    await completedTcs.Task;

                    result.Sentiment = sentimentSum / countSentiments;

                    return result;
                }
            }
            catch (Exception ex)
            {
                // If you get a 404 error, try again. Known issue.
                Console.WriteLine(ex.ToString());
                throw;
            }
            finally
            {
                await signalrConnection.StopAsync();
            }
        }

        static async Task<ProductSentiment> GetProductSentimentFromStreamAsync(Stream stream)
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

                return await GetInsights(tmpWavFileName);
            }
            finally
            {
                if (tmpMp4FileName != null) File.Delete(tmpMp4FileName);
                if (tmpWavFileName != null) File.Delete(tmpWavFileName);
            }
        }

        static void Main(string[] args)
        {
            string fileName = @"C:\g\project-frankfurt\sampleData\f7a1b24c-a942-41d0-bcef-b32b33e7fd9a.mp4";
            var stream = File.OpenRead(fileName);

            var v = GetProductSentimentFromStreamAsync(stream).Result;

            Console.WriteLine("Hello World!");
        }
    }
}
