# Listing bots and phone numbers

You can determine which phone numbers are bound to bots using the Powershell scripts below. 

  1. The following script will list all bots with Telephony configured. The script expects one parameter:
- subscription id

```Powershell
# List all bots with Telephony configured
#
param ($subscription)

$output = az resource list --subscription $subscription --query "[?type=='Microsoft.BotService/botServices'].{name:name,rg:resourceGroup}" | ConvertFrom-Json

foreach($item in $output){
    $resourceGroup = $item.rg
    $bot = $item.name

    $botdetail = az bot show -n $bot -g $resourceGroup --subscription $subscription | ConvertFrom-Json

    if($botdetail.properties.enabledChannels -contains "telephony"){
        $properties = az resource show --ids /subscriptions/$subscription/resourceGroups/$resourceGroup/providers/Microsoft.BotService/botServices/$bot/channels/TelephonyChannel | ConvertFrom-Json

        foreach($prop in $properties){
            $phoneNumbers = [string]::Join(",",($prop.properties.properties.phoneNumbers | Select-Object -ExpandProperty phoneNumber))
            write-output ("{0}: [{1}]" -f $bot, $phoneNumbers)
        }
    }
}
```

Example:

```
.\listBots.ps1 839d0dee-dc2c-4022-b9fd-9b949aced033
```

  2. The following script will list phone numbers associated with a given bot. The script expects three parameters:
- subscription id
- resource group
- bot id

```Powershell
# List phone numbers associated with a given bot
#
param ($subscription, $resourceGroup, $bot)

$botdetail = az bot show -n $bot -g $resourceGroup --subscription $subscription | ConvertFrom-Json

if($botdetail.properties.enabledChannels -contains "telephony"){
    $properties = az resource show --ids /subscriptions/$subscription/resourceGroups/$resourceGroup/providers/Microsoft.BotService/botServices/$bot/channels/TelephonyChannel | ConvertFrom-Json

    foreach($item in $properties){
        $phoneNumbers = [string]::Join(",",($item.properties.properties.phoneNumbers | Select-Object -ExpandProperty phoneNumber))
        write-output ("[{0}]" -f $phoneNumbers)
    }
}
```
Example:
```
.\listPhoneNumbers.ps1 839d0dee-dc2c-4022-b9fd-9b949aced033 Telephony-bots my-testbot
```
