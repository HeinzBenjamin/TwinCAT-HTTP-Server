$headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]"
$headers.Add("Content-Type", "application/json")

$body = "{`"request_type`": `"write`",`"names`":[`"GVL.bFanOn`", `"GVL.bHeatOn`", `"MAIN.dMotorSpeed`", `"MAIN.bLedsOn`"],`"types`":[`"bool`",`"bool`",`"lreal`",`"bool[6]`"], `"values`":[true, false, 3.141, [true, true, false, false, true, false]]}"

$response = Invoke-RestMethod 'http://localhost:8528/twincat' -Method 'POST' -Headers $headers -Body $body
$response | ConvertTo-Json