using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace BotFramework.Telephony.Samples
{
    public static class RecordingHandler
    {
        private static HttpResponseMessage ErrorResult(string text, HttpStatusCode errorCode)
        {
            HttpResponseMessage message = new HttpResponseMessage();
            message.Content = new StringContent($"<html><body><div>{text}</div></body></html>");
            message.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            message.StatusCode = errorCode;
            return message;
        }

        [FunctionName("Get")]
        //public static async Task<IActionResult> Run(
        public static async Task<HttpResponseMessage> Run(
             //[HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
             [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]
            HttpRequest req, ILogger log)
        {
            try
            {
                log.LogInformation("Get function processed a request.");

                string id = req.Query["id"];

                string requestBody = String.Empty;
                using (StreamReader streamReader = new StreamReader(req.Body))
                {
                    requestBody = await streamReader.ReadToEndAsync();
                }
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                id = id ?? data?.id;

                log.LogInformation($"id={id}");

                string cacheConnection = Environment.GetEnvironmentVariable("CUSTOMCONNSTR_cacheConnection", EnvironmentVariableTarget.Process);
                if (string.IsNullOrEmpty(cacheConnection))
                {
                    log.LogError($"Missing Redis connection string '{cacheConnection}'");
                    return ErrorResult("Recording downloader is not properly configured", HttpStatusCode.InternalServerError);
                }
                else
                {
                    ConnectionMultiplexer cm = ConnectionMultiplexer.Connect(cacheConnection);
                    IDatabase cache = cm.GetDatabase();

                    string recordingFileName = "";

                    try
                    {
                        recordingFileName = cache.StringGet(id);
                    }
                    catch { }

                    if (string.IsNullOrEmpty(recordingFileName))
                    {
                        return ErrorResult($"Sorry, cannot find recording {id}", HttpStatusCode.NotFound);
                    }

                    log.LogInformation($"{id}={recordingFileName}");

                    string connStr = Environment.GetEnvironmentVariable("CUSTOMCONNSTR_RecordingStorageBlob", EnvironmentVariableTarget.Process);
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connStr);

                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                    CloudBlobContainer container = blobClient.GetContainerReference("recordings");

                    CloudBlockBlob downloadBlob = container.GetBlockBlobReference(recordingFileName);

                    Stream stream = await downloadBlob.OpenReadAsync();

                    var httpResponseMessage = new HttpResponseMessage()
                    {
                        Content = new StreamContent(stream)
                    };

                    httpResponseMessage.Content.Headers.ContentLength = stream.Length;
                    httpResponseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
                    httpResponseMessage.Headers.AcceptRanges.Add("bytes");
                    httpResponseMessage.StatusCode = HttpStatusCode.OK;

                    return httpResponseMessage;
                }
            }
            catch(Exception ex)
            {
                log.LogError("Failed with {0}", ex.Message);
                log.LogError("Failed with {0}", ex.InnerException.Message);
                return ErrorResult("Recording downloader has run into an error", HttpStatusCode.InternalServerError);
            }
        }
    }
}
