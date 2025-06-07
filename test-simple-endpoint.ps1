# Simple test to verify the endpoint is working
$baseUrl = "http://localhost:5037"

Write-Host "Testing endpoint: $baseUrl/api/naturallanguage/triggers/query" -ForegroundColor Yellow

$body = @{
    Query = "show me campaigns"
    IncludeDebugInfo = $true
} | ConvertTo-Json

try {
    Write-Host "Sending request..." -ForegroundColor Cyan
    $response = Invoke-RestMethod -Uri "$baseUrl/api/naturallanguage/triggers/query" -Method POST -Body $body -ContentType "application/json" -TimeoutSec 30
    
    Write-Host "Response received!" -ForegroundColor Green
    Write-Host "Success: $($response.Success)" -ForegroundColor Green
    Write-Host "Original Query: $($response.OriginalQuery)" -ForegroundColor Cyan
    
    if ($response.DebugInfo) {
        Write-Host "Debug Info Available: Yes" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}

Write-Host "Test completed!" -ForegroundColor Green
