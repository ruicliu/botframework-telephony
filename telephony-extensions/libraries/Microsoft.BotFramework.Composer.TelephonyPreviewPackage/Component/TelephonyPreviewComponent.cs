using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Components.Telephony.Preview;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Components.Telephony.Preview.Component
{
    public class TelephonyPreviewComponent : BotComponent
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<DeclarativeType>(new DeclarativeType<SendRecordingStart>(SendRecordingStart.Kind));
            services.AddSingleton<DeclarativeType>(new DeclarativeType<OnRecordingStartResult>(OnRecordingStartResult.Kind));
        }
    }
}
