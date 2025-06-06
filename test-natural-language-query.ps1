# Test the EmailTrigger natural language query API
$baseUrl = "http://localhost:5037"

# Test the query that the user mentioned: "show me campaigns open rates high"
$testQuery = @{
    query = "show me campaigns open rates high"
    includeDebugInfo = $true
} | ConvertTo-Json

Write-Host "Testing EmailTrigger Natural Language Query API..." -ForegroundColor Cyan
Write-Host "Query: $($testQuery)" -ForegroundColor Yellow

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/naturallanguage/triggers/query" -Method POST -Body $testQuery -ContentType "application/json"
    
    Write-Host "`nAPI Response:" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 10 | Write-Host
    
    # Check what data is available
    Write-Host "`nAnalyzing Response Data:" -ForegroundColor Magenta
    Write-Host "Success: $($response.success)" -ForegroundColor $(if($response.success) {"Green"} else {"Red"})
    Write-Host "Intent: $($response.intent)" -ForegroundColor Blue
    Write-Host "Explanation: $($response.explanation)" -ForegroundColor Blue
    Write-Host "Processing Time: $($response.processingTimeMs)ms" -ForegroundColor Blue
    
    if ($response.triggerReports) {
        Write-Host "Trigger Reports Count: $($response.triggerReports.Count)" -ForegroundColor Green
        if ($response.triggerReports.Count -gt 0) {
            Write-Host "First Report Strategy: $($response.triggerReports[0].strategyName)" -ForegroundColor Green
        }
    } else {
        Write-Host "No triggerReports in response" -ForegroundColor Red
    }
    
    if ($response.summary) {
        Write-Host "Summary Available: Yes" -ForegroundColor Green
        Write-Host "Summary Total Emails: $($response.summary.totalEmails)" -ForegroundColor Green
    } else {
        Write-Host "Summary Available: No" -ForegroundColor Red
    }
    
    if ($response.availableStrategies) {
        Write-Host "Available Strategies Count: $($response.availableStrategies.Count)" -ForegroundColor Green
    } else {
        Write-Host "No availableStrategies in response" -ForegroundColor Red
    }
    
} catch {
    Write-Host "Error testing API: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode
        Write-Host "HTTP Status: $statusCode" -ForegroundColor Red
    }
}
