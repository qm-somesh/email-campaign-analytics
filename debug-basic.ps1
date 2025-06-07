# Simple debug to identify the hanging point
$baseUrl = "http://localhost:5037"

Write-Host "Starting debug..." -ForegroundColor Yellow

# Test 1: Very basic request
Write-Host "Test 1: Basic connectivity test" -ForegroundColor Cyan
try {
    $startTime = Get-Date
    Write-Host "Sending basic request at $startTime" -ForegroundColor Gray
    
    $response = Invoke-WebRequest -Uri "$baseUrl/api/naturallanguage/triggers/query" -Method POST -Body '{"Query":"test"}' -ContentType "application/json" -TimeoutSec 5
    
    $endTime = Get-Date
    $duration = ($endTime - $startTime).TotalSeconds
    Write-Host "✓ Basic request completed in $duration seconds" -ForegroundColor Green
    Write-Host "Status Code: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Content Length: $($response.Content.Length)" -ForegroundColor Green
    
} catch {
    Write-Host "✗ Basic request failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception -is [System.Net.WebException]) {
        Write-Host "WebException details: $($_.Exception.Status)" -ForegroundColor Red
    }
}

Write-Host "`nTest completed." -ForegroundColor Yellow
