#!/usr/bin/env pwsh

# Test script to verify the numeric threshold fix
param(
    [string]$BaseUrl = "http://localhost:5037",
    [string]$Query = "Show me campaigns with high click rates more than 500"
)

Write-Host "Testing Numeric Threshold Fix" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan
Write-Host "Query: $Query" -ForegroundColor Yellow
Write-Host "Expected: Only strategies with click counts > 500" -ForegroundColor Yellow
Write-Host ""

# Prepare the request
$requestBody = @{
    query = $Query
} | ConvertTo-Json

$headers = @{
    'Content-Type' = 'application/json'
}

$endpoint = "$BaseUrl/api/naturallanguage/triggers/query"

try {
    Write-Host "Sending request to: $endpoint" -ForegroundColor Gray
    
    # Make the API call
    $response = Invoke-RestMethod -Uri $endpoint -Method Post -Body $requestBody -Headers $headers -TimeoutSec 30
    
    Write-Host "✅ Response received successfully" -ForegroundColor Green
    Write-Host ""
    Write-Host "Response Details:" -ForegroundColor White
    Write-Host "  Processing Type: $($response.processingType)" -ForegroundColor Blue
    Write-Host "  Intent: $($response.intent)" -ForegroundColor Blue
    Write-Host "  Total Records: $($response.data.Count)" -ForegroundColor Blue
    Write-Host ""
    
    if ($response.data.Count -eq 0) {
        Write-Host "⚠️  No data returned" -ForegroundColor Yellow
        return
    }
    
    Write-Host "Strategy Results:" -ForegroundColor White
    Write-Host "-----------------" -ForegroundColor White
    
    $validResults = 0
    $invalidResults = 0
    
    foreach ($strategy in $response.data) {
        $isValid = $strategy.TotalClickedCount -gt 500
        $color = if ($isValid) { 'Green' } else { 'Red' }
        $status = if ($isValid) { '✅' } else { '❌' }
        
        Write-Host "$status Strategy: $($strategy.StrategyName)" -ForegroundColor $color
        Write-Host "    Clicks: $($strategy.TotalClickedCount) | Opens: $($strategy.TotalOpenedCount)" -ForegroundColor $color
        
        if ($isValid) { $validResults++ } else { $invalidResults++ }
    }
    
    Write-Host ""
    Write-Host "Test Results:" -ForegroundColor White
    Write-Host "-------------" -ForegroundColor White
    Write-Host "✅ Valid results (clicks > 500): $validResults" -ForegroundColor Green
    Write-Host "❌ Invalid results (clicks ≤ 500): $invalidResults" -ForegroundColor Red
    
    if ($invalidResults -eq 0) {
        Write-Host ""
        Write-Host "🎉 SUCCESS: All strategies have click counts > 500!" -ForegroundColor Green
        Write-Host "The numeric threshold fix is working correctly." -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "❌ FAILURE: Found $invalidResults strategies with click counts ≤ 500" -ForegroundColor Red
        Write-Host "The numeric threshold fix needs further investigation." -ForegroundColor Red
    }
    
} catch {
    Write-Host "❌ Error occurred:" -ForegroundColor Red
    Write-Host "  Message: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.ErrorDetails) {
        Write-Host "  Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    
    if ($_.Exception.Response) {
        Write-Host "  Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}
