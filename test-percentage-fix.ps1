# Test the percentage filtering fix
$uri = "http://localhost:5037/api/NaturalLanguage/process-emailtrigger-query"
$headers = @{ "Content-Type" = "application/json" }
$body = @{
    query = "Show me campaigns with high click rate percentage greater than 20"
    includeDebugInfo = $true
} | ConvertTo-Json

Write-Host "Testing query: 'Show me campaigns with high click rate percentage greater than 20'" -ForegroundColor Green
Write-Host "Request Body: $body" -ForegroundColor Yellow

try {
    $response = Invoke-RestMethod -Uri $uri -Method POST -Headers $headers -Body $body
    
    Write-Host "`nResponse received:" -ForegroundColor Green
    Write-Host "Intent: $($response.intent)" -ForegroundColor Cyan
    Write-Host "Explanation: $($response.explanation)" -ForegroundColor Cyan
    Write-Host "Total Count: $($response.totalCount)" -ForegroundColor Cyan
    
    if ($response.debugInfo) {
        Write-Host "`nDebug Info:" -ForegroundColor Yellow
        Write-Host "  Service Method: $($response.debugInfo.serviceMethodCalled)" -ForegroundColor White
        if ($response.debugInfo.extractedFilters) {
            Write-Host "  Extracted Filters:" -ForegroundColor White
            $response.debugInfo.extractedFilters.PSObject.Properties | ForEach-Object {
                Write-Host "    $($_.Name): $($_.Value)" -ForegroundColor Gray
            }
        }
    }
    
    if ($response.triggerReports -and $response.triggerReports.Count -gt 0) {
        Write-Host "`nReturned Reports:" -ForegroundColor Green
        $response.triggerReports | ForEach-Object {
            Write-Host "  - $($_.strategyName): Click Rate = $($_.clickRate)%" -ForegroundColor White
        }
        
        # Verify all returned reports have click rate > 20%
        $invalidReports = $response.triggerReports | Where-Object { $_.clickRate -le 20 }
        if ($invalidReports.Count -gt 0) {
            Write-Host "`n❌ BUG STILL EXISTS: Found reports with click rate <= 20%:" -ForegroundColor Red
            $invalidReports | ForEach-Object {
                Write-Host "  - $($_.strategyName): Click Rate = $($_.clickRate)%" -ForegroundColor Red
            }
        } else {
            Write-Host "`n✅ SUCCESS: All returned reports have click rate > 20%" -ForegroundColor Green
        }
    } else {
        Write-Host "`nNo reports returned" -ForegroundColor Yellow
    }
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Response: $($_.Exception.Response)" -ForegroundColor Red
}
