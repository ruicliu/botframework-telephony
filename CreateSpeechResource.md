# Create a Speech Services resource

Follow these instructions to create a Speech resource:

1. Go to the [Azure portal](https://portal.azure.com) and select **Create a resource** from the left navigation.
2. In the search bar, type **Speech**.
3. Select **Speech**, then click **Create**.
4. You'll be prompted to provide some information:
   * Give your resource a **Name** (Say **TelephonyChannelSpeech**)
   * For **Subscription**, choose the appropriate subscription
   * For **Location**, choose the appropriate region.Ideally, this should be same as Bot's Azure region for best latencies. Please note that currently Telephony Channel is only supported in following Azure regions:
        * West US
        * West US 2
        * East US
        * East US 2
        * West Europe
        * North Europe
        * Southeast Asia
   * For **Pricing tier**, select **F0** (Free Tier) to start with. Note that usage in Free tier is subjected to [Free tier Limits](https://azure.microsoft.com/en-us/pricing/details/cognitive-services/speech-services/)
   * For **Resource group**, select an existing resource group or create a new resource group.
5. After you've entered all required information, click **Create**. It may take a few minutes to create your resource.
6. Once the resource is created, note down **Cognitive Service Subscription Key** for this resource
    * You can access these keys at any time from your resource's **Overview** (Manage keys) or **Keys**.
