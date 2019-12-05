# Create a Bot in Azure Bot Framework

In this step, we will create a new bot in Azure that we could then link to the phone number we created in the previous steps.

Log into Azure with your Azure admin credentials - or create a new free trial account of Azure. 
Open https://aka.ms/ivr6 to go directly into the bot flow creation if you are an expert user.  Otherwise, after you log into Azure, click **Create a resource**:

![](images/create-a-bot/c001-create-a-resource.png)

Type in "Bot" and press **Enter**:

![](images/create-a-bot/c002-type-bot.png)

Among the options, click on **Web App Bot**.  This is the main Microsoft Bot Framework offering:

![](images/create-a-bot/c003-click-web-app-bot.png)

Click **Create**:

![](images/create-a-bot/c004-click-create.png)

Fill out the settings for your bot.  For reduced latency for telephony, for **Location** we recommend one of these regions:  West US, West US 2, East US, East US 2, West Europe, North Europe, Southeast Asia.  You may start with **F0** free plan, but for production loads, you should use **S0** plan that does not have monthly limits:

![](images/create-a-bot/c005-fill-out-settings.png)

At the bottom of the screen, click **Create**.  

Azure is now going to start the deployment of your bot resources.  This usually takes a minute or two.  

After your bot is created, navigate to that bot:

![](images/create-a-bot/c011-go-to-speech-services.png)

On the left side, click on **All App service settings**:

![](images/create-a-bot/c018-click-on-all-app-service-settings.png)

At the top, click **Get publishing profile**.  You can use the publishing settings provided in the downloaded file to deploy your bot's code from Visual Studio easily with just two clicks: 

![](images/create-a-bot/c019-click-on-get-publish-profile.png)

**Next step**:  [Enable your bot to speech and understand voice](EnableTelephony.md)
