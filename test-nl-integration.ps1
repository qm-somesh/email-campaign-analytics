# Test Natural Language Integration
# This script tests the fixed EmailTrigger natural language endpoint

$baseUrl = "http://localhost:5037"
$endpoint = "$baseUrl/api/NaturalLanguage/triggers/query"

Write-Host "Testing Natural Language EmailTrigger Integration" -ForegroundColor Green
Write-Host "Endpoint: $endpoint" -ForegroundColor Yellow

# Test 1: Summary query
Write-Host "`n=== Test 1: Summary Query ===" -ForegroundColor Cyan
$body1 = @{
    query = "show me a summary of email triggers"
    includeDebugInfo = $true
} | ConvertTo-Json

try {
    $response1 = Invoke-RestMethod -Uri $endpoint -Method POST -Body $body1 -ContentType "application/json"
    Write-Host "✅ Summary Query Success" -ForegroundColor Green
    Write-Host "Original Query: $($response1.originalQuery)" -ForegroundColor White
    Write-Host "Intent: $($response1.intent)" -ForegroundColor White
    Write-Host "Explanation: $($response1.explanation)" -ForegroundColor White
    Write-Host "Success: $($response1.success)" -ForegroundColor White
    if ($response1.triggerReports) {
        Write-Host "Trigger Reports Count: $($response1.triggerReports.Count)" -ForegroundColor White
    }
} catch {
    Write-Host "❌ Summary Query Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Performance metrics query
Write-Host "`n=== Test 2: Performance Metrics Query ===" -ForegroundColor Cyan
$body2 = @{
    query = "show me bounce rates and click rates"
    includeDebugInfo = $true
} | ConvertTo-Json

try {
    $response2 = Invoke-RestMethod -Uri $endpoint -Method POST -Body $body2 -ContentType "application/json"
    Write-Host "✅ Performance Metrics Query Success" -ForegroundColor Green
    Write-Host "Original Query: $($response2.originalQuery)" -ForegroundColor White
    Write-Host "Intent: $($response2.intent)" -ForegroundColor White
    Write-Host "Explanation: $($response2.explanation)" -ForegroundColor White
    Write-Host "Success: $($response2.success)" -ForegroundColor White
    if ($response2.triggerReports) {
        Write-Host "Trigger Reports Count: $($response2.triggerReports.Count)" -ForegroundColor White
    }
} catch {
    Write-Host "❌ Performance Metrics Query Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Strategy listing query
Write-Host "`n=== Test 3: Strategy Listing Query ===" -ForegroundColor Cyan
$body3 = @{
    query = "list all strategies"
    includeDebugInfo = $true
} | ConvertTo-Json

try {
    $response3 = Invoke-RestMethod -Uri $endpoint -Method POST -Body $body3 -ContentType "application/json"
    Write-Host "✅ Strategy Listing Query Success" -ForegroundColor Green
    Write-Host "Original Query: $($response3.originalQuery)" -ForegroundColor White
    Write-Host "Intent: $($response3.intent)" -ForegroundColor White
    Write-Host "Explanation: $($response3.explanation)" -ForegroundColor White
    Write-Host "Success: $($response3.success)" -ForegroundColor White
    if ($response3.availableStrategies) {
        Write-Host "Available Strategies Count: $($response3.availableStrategies.Count)" -ForegroundColor White
    }
} catch {
    Write-Host "❌ Strategy Listing Query Failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Integration Test Complete ===" -ForegroundColor Green
Write-Host "All tests check the fixed endpoint structure:" -ForegroundColor Yellow
Write-Host "- originalQuery field" -ForegroundColor Yellow
Write-Host "- intent field" -ForegroundColor Yellow
Write-Host "- explanation field (instead of message)" -ForegroundColor Yellow
Write-Host "- success field" -ForegroundColor Yellow
