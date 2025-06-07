# Test percentage filtering fix
Write-Host "Testing Percentage Filtering Fix" -ForegroundColor Cyan

$body = @{
    query = "Show me campaigns with high click rate percentage greater than 20"
    includeDebugInfo = $true
} | ConvertTo-Json

Write-Host "Request Body:" -ForegroundColor Yellow
Write-Host $body

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5037/api/naturallanguage/triggers/query" -Method POST -Body $body -ContentType "application/json"
    
    Write-Host "`nResponse:" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 5 | Write-Host
    
    # Check the debug info for percentage filtering
    if ($response.debugInfo -and $response.debugInfo.extractedFilters) {
        Write-Host "`nDebug Analysis:" -ForegroundColor Magenta
        $filters = $response.debugInfo.extractedFilters
        
        Write-Host "isPercentageQuery: $($filters.isPercentageQuery)" -ForegroundColor Yellow
        Write-Host "appliedFilter: $($filters.appliedFilter)" -ForegroundColor Yellow
        Write-Host "threshold: $($filters.threshold)" -ForegroundColor Yellow
        Write-Host "isGreater: $($filters.isGreater)" -ForegroundColor Yellow
        
        # Check if percentage filtering was applied correctly
        if ($filters.appliedFilter -like "*ClickRatePercentage*") {
            Write-Host "✅ SUCCESS: Percentage filtering was applied correctly!" -ForegroundColor Green
        } else {
            Write-Host "❌ ISSUE: Count filtering was applied instead of percentage filtering" -ForegroundColor Red
        }
    }
    
    # Check results
    if ($response.totalCount -eq 0) {
        Write-Host "✅ CORRECT: Zero results returned for >20% click rate (expected since all mock data has ≤20% click rates)" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Unexpected: Found $($response.totalCount) results for >20% click rate" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $errorDetails = $reader.ReadToEnd()
        Write-Host "Error Details: $errorDetails" -ForegroundColor Red
    }
}
