$baseUrl = "http://localhost:5037"
$body = @{
    Query = "show me campaigns with click rate percentage greater than 20"
    Context = "debug_test"
    IncludeDebugInfo = $true
} | ConvertTo-Json

Write-Host "Testing percentage filtering..." -ForegroundColor Yellow
Write-Host "Query: show me campaigns with click rate percentage greater than 20" -ForegroundColor Cyan

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/natural-language/emailtrigger" -Method POST -Body $body -ContentType "application/json"
    
    Write-Host "SUCCESS!" -ForegroundColor Green
    Write-Host "Original Query: $($response.OriginalQuery)" -ForegroundColor White
    Write-Host "Explanation: $($response.Explanation)" -ForegroundColor White
    
    if ($response.DebugInfo -and $response.DebugInfo.AdditionalInfo) {
        Write-Host "Debug Info:" -ForegroundColor Yellow
        foreach ($key in $response.DebugInfo.AdditionalInfo.Keys) {
            Write-Host "  $key`: $($response.DebugInfo.AdditionalInfo[$key])" -ForegroundColor Gray
        }
    }
    
    Write-Host "Results Count: $($response.Results.Count)" -ForegroundColor Cyan
    
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
}
