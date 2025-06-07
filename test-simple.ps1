# Simple test to check percentage filtering fix
Write-Host "Testing Percentage Filtering Bug Fix" -ForegroundColor Cyan

# Test the working campaigns endpoint first
Write-Host "`n=== Testing Campaigns Endpoint ===" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5037/api/campaigns" -Method GET
    Write-Host "Campaigns endpoint working. Found campaigns: $($response.Length)" -ForegroundColor Green
} catch {
    Write-Host "Campaigns endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test the natural language endpoint structure
Write-Host "`n=== Testing Natural Language Endpoint ===" -ForegroundColor Yellow
try {
    $body = @{
        query = "Show me campaigns with high click rate percentage greater than 20"
        includeDebugInfo = $true
    } | ConvertTo-Json
    
    Write-Host "Request body: $body"
    $response = Invoke-RestMethod -Uri "http://localhost:5037/api/naturallanguage/triggers/query" -Method POST -Body $body -ContentType "application/json"
    Write-Host "Natural language endpoint response:" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 3
} catch {
    Write-Host "Natural language endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    Write-Host "Response: $($_.Exception.Response)" -ForegroundColor Red
}
