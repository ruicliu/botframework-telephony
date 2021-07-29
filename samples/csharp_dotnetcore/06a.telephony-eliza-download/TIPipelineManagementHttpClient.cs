// <copyright file="TIPipelineManagementHttpClient.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace TIClient.TIPipelineManagementHttpClient
{
    using Newtonsoft.Json;
    //using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using TIClient.Constants;


    /// <summary>
    /// Test utils
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// try add slash to the url
        /// </summary>
        /// <param name="url">url</param>
        /// <returns>updated url</returns>
        public static string AddSlashIfDoesntExist(this string url)
        {
            return url.EndsWith('/') ? url : url + '/';
        }
    }


    /// <summary>
    /// TI Pipeline Management Http Client
    /// </summary>
    internal sealed class TIPipelineManagementHttpClient
    {
        /// <summary>
        /// trigger endpoint
        /// </summary>
        private readonly string triggerEndpoint;

        /// <summary>
        /// pipeline endpoint
        /// </summary>
        private readonly string pipelineEndpoint;

        /// <summary>
        /// http client
        /// </summary>
        private readonly HttpClient httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="TIPipelineManagementHttpClient"/> class.
        /// </summary>
        /// <param name="orchestrationEndpoint"> orchestration endpoint </param>
        /// <param name="subscriptionKey"> subscription key </param>
        public TIPipelineManagementHttpClient(string orchestrationEndpoint, string subscriptionKey)
        {
            if (string.IsNullOrWhiteSpace(orchestrationEndpoint))
            {
                throw new ArgumentNullException(nameof(orchestrationEndpoint));
            }

            if (string.IsNullOrWhiteSpace(subscriptionKey))
            {
                throw new ArgumentNullException(nameof(subscriptionKey));
            }

            this.triggerEndpoint = $"{orchestrationEndpoint.AddSlashIfDoesntExist()}projectFrankfurt/api/v1/triggers";
            this.pipelineEndpoint = $"{orchestrationEndpoint.AddSlashIfDoesntExist()}projectFrankfurt/api/v1/pipelines";

            this.httpClient = new HttpClient();
            this.httpClient.DefaultRequestHeaders.Add(HeaderConstants.SubscriptionKey, subscriptionKey);
        }

        /// <summary>
        /// create trigger and get pipeline id
        /// </summary>
        /// <param name="expr"> trigger expression </param>
        /// <param name="pipelineId"> pipeline id </param>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        public async Task<string> CreateTriggerAsync(string expr, string pipelineId)
        {
            var triggerJson = JObject.FromObject(new
            {
                Definition = expr,
                Name = "Trigger",
                PipelineId = pipelineId
            });

            var requestContent = new StringContent(triggerJson.ToString(), Encoding.UTF8, "application/json");
            var response = await this.httpClient.PostAsync(this.triggerEndpoint, requestContent).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var createTriggerResponse = JObject.Parse(json);

            //Console.WriteLine("Response to the create trigger request:");
            //Console.WriteLine(createTriggerResponse.ToString(Formatting.Indented));

            var triggerId = createTriggerResponse.Value<string>("Id");
            ///Assert.IsNotNull(string.IsNullOrWhiteSpace(triggerId), "Trigger id should not be null or whitespace.");

            return triggerId;
        }

        /// <summary>
        /// WARNING: Deletes ALL triggers associated with the current resource.
        /// </summary>
        public async Task DeleteAllTriggersAsync()
        {
            var tasks = (await GetTriggersAsync()).Select(x => DeleteTriggerAsync(x)).ToArray();
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Get current triggers
        /// </summary>
        /// <param name="expr"> trigger expression </param>
        /// <param name="pipelineId"> pipeline id </param>
        /// <returns>A list of the trigger Ids</returns>
        public async Task<List<string>> GetTriggersAsync()
        {
            var response = await this.httpClient.GetAsync(this.triggerEndpoint).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var triggersJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JArray.Parse(triggersJson).Select(x => x.Value<string>("Id")).ToList();
        }

        /// <summary>
        /// Create pipeline and get pipeline id
        /// </summary>
        /// <param name="pipelineRequestBodyFilePath"> path to pipeline file</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        public async Task<string> CreatePipelineAsync(string pipelineRequestBodyFilePath)
        {
            var requestContent = new StringContent(File.ReadAllText(pipelineRequestBodyFilePath), Encoding.UTF8, "application/json");
            var response = await this.httpClient.PostAsync(this.pipelineEndpoint, requestContent).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var createPipelineResponse = JObject.Parse(json);

            //Console.WriteLine("Response to the create pipeline request:");
            //Console.WriteLine(createPipelineResponse.ToString(Formatting.Indented));

            string pipelineId = createPipelineResponse.Value<string>("Id");
            //Assert.IsFalse(string.IsNullOrWhiteSpace(pipelineId), "Pipeline id should not be null or whitespace.");

            return pipelineId;
        }

        /// <summary>
        /// Delete pipeline
        /// </summary>
        /// <param name="pipelineId"> pipeline id</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        public async Task DeletePipelineAsync(string pipelineId)
        {
            if (string.IsNullOrWhiteSpace(pipelineId))
            {
                throw new Exception ("Pipeline ID is empty");
            }

            var deletePipelineEndpoint = $"{this.pipelineEndpoint}/{pipelineId}";
            await this.httpClient.DeleteAsync(deletePipelineEndpoint).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete trigger
        /// </summary>
        /// <param name="triggerId"> pipeline id</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        public async Task DeleteTriggerAsync(string triggerId)
        {
            if (string.IsNullOrWhiteSpace(triggerId))
            {
                throw new Exception("Trigger ID is empty");
            }

            var deleteTriggerEndpoint = $"{this.triggerEndpoint}/{triggerId}";
            await this.httpClient.DeleteAsync(deleteTriggerEndpoint).ConfigureAwait(false);
        }
    }
}
