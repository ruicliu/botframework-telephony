// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.BotBuilderSamples.Bots;

namespace Microsoft.BotBuilderSamples
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // Create the Bot Framework Adapter
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            // Create the storage we'll be using for User and Conversation state.
            // (Memory is great for testing purposes - examples of implementing storage with
            // Azure Blob Storage or Cosmos DB are below).
            var storage = new MemoryStorage();

            // Create the Conversation state passing in the storage layer.
            var conversationState = new ConversationState(storage);
            services.AddSingleton(conversationState);

            // Register our voice font of choice. Here we are using en-US-AriaNeural.
            // Visit this page for a list of all our voice fonts - https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support#text-to-speech
            // Please note that neural voice fonts are only available in certain regions - https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/regions#text-to-speech
#if CONTOSO
            var voiceFactory = new VoiceFactory("Microsoft Server Speech Text to Speech Voice (en-US, GuyNeural)", "en-US", "customerservice");
//            var voiceFactory = new VoiceFactory("Microsoft Server Speech Text to Speech Voice (en-US, JessaRUS)", "en-US", "customerservice");
#else
            var voiceFactory = new VoiceFactory("Microsoft Server Speech Text to Speech Voice (en-IE, EmilyNeural)", "en-US", "customerservice");
#endif
            services.AddSingleton(voiceFactory);

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, EchoBotWithRecording>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseWebSockets();
            //app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
