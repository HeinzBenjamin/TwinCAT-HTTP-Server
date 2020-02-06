$headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]"
$headers.Add("Content-Type", "application/json")

$body = "{`"request_type`": `"read`",`"names`":[`"GVL.bFanOn`", `"GVL.bHeatOn`", `"MAIN.dMotorSpeed`", `"MAIN.bLedsOn`"],`"types`":[`"bool`",`"bool`",`"lreal`",`"bool[6]`"]}"

$response = Invoke-RestMethod 'http://localhost:8528/twincat' -Method 'POST' -Headers $headers -Body $body
$response | ConvertTo-Json