# Listing bots and phone numbers

You can determine which phone numbers are bound to bots using the Powershell scripts below. 

---

**Prerequisites**: To use the Powershell scripts below, you will need to additionally install the `armclient` tool using one of these methods:
* Chocolatey:

`choco install armclient --source=https://chocolatey.org/api/v2/`

* Scoop:

`scoop install armclient`

---

1. Log in to Azure using `armclient` as follows:

```
armclient login
```

2. Copy the ARM token to clipboard. You will use it as one of the parameters to the Powershell scripts below:

```
armclient token
```

3. The following script will list all bots with Telephony configured. The script expects two parameters:
- subscription id
- token (from `armclient token` call above)

```Powershell
# List all bots with Telephony configured
#
param ($subscription, $token)

$headers = @{
    Authorization="Bearer $token"
}

$output = az resource list --subscription $subscription --query "[?type=='Microsoft.BotService/botServices'].{name:name,rg:resourceGroup}" | ConvertFrom-Json

foreach($item in $output){
    $resourceGroup = $item.rg
    $bot = $item.name

    $botdetail = az bot show -n $bot -g $resourceGroup --subscription $subscription | ConvertFrom-Json

    if($botdetail.properties.enabledChannels -contains "telephony"){

        $url = "https://management.azure.com/subscriptions/"+ $subscription + "/resourceGroups/" + $resourceGroup + "/providers/Microsoft.BotService/botServices/" + $bot +"/channels/TelephonyChannel?api-version=2020-06-02"

        $properties = Invoke-RestMethod -uri $url -Method Get -ContentType "application/json" -Headers $headers -ErrorAction SilentlyContinue

        foreach($prop in $properties){
            $phoneNumbers = [string]::Join(",",($prop.properties.properties.phoneNumbers | Select-Object -ExpandProperty phoneNumber))
            write-output ("{0}: [{1}]" -f $bot, $phoneNumbers)
        }
    }
  }
  ```

Example:

```
.\listBots.ps1 839d0dee-dc2c-4022-b9fd-9b949aced033 ey..XYZ
```

  4. The following script will list phone numbers associated with a given bot. The script expects two parameters:
- subscription id
- resource group
- bot id
- token (from `armclient token` call above)

```Powershell
# List phone numbers associated with a given bot
#
param ($subscription, $resourceGroup, $bot, $token)

$botdetail = az bot show -n $bot -g $resourceGroup --subscription $subscription | ConvertFrom-Json

$url = "https://management.azure.com/subscriptions/"+ $subscription + "/resourceGroups/" + $resourceGroup + "/providers/Microsoft.BotService/botServices/" + $bot +"/channels/TelephonyChannel?api-version=2020-06-02"

$headers = @{
    Authorization="Bearer $token"
}

if($botdetail.properties.enabledChannels -contains "telephony"){
    $response = Invoke-RestMethod -uri $url -Method Get -ContentType "application/json" -Headers $headers
    foreach($item in $response){
        $phoneNumbers = [string]::Join(",",($item.properties.properties.phoneNumbers | Select-Object -ExpandProperty phoneNumber))
        write-output ("[{0}]" -f $phoneNumbers)
    }
}
```
Example:
```
.\listPhoneNumbers.ps1 839d0dee-dc2c-4022-b9fd-9b949aced033 Telephony-bots my-testbot ey..XYZ
```
