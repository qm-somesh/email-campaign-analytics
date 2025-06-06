# Test the Natural Language Query API directly
$apiUrl = "http://localhost:5037/api/naturallanguage/triggers/query"
$headers = @{
    "Content-Type" = "application/json"
}
$body = @{
    query = "show me campaigns open rates high"
    includeDebugInfo = $true
} | ConvertTo-Json

Write-Host "Testing API endpoint: $apiUrl"
Write-Host "Request body: $body"
Write-Host ""

try {
    $response = Invoke-RestMethod -Uri $apiUrl -Method POST -Headers $headers -Body $body
    Write-Host "Response received successfully!"
    Write-Host ""
    Write-Host "Success: $($response.success)"
    Write-Host "TriggerReports count: $($response.triggerReports.Count)"
    Write-Host "TriggerReports exists: $($response.triggerReports -ne $null)"
    Write-Host ""
    Write-Host "Full response:"
    $response | ConvertTo-Json -Depth 10
} catch {
    Write-Host "Error occurred: $($_.Exception.Message)"
    Write-Host $_.Exception
}
