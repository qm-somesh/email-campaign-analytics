# Test script to verify the percentage filtering fix
Write-Host "Testing Percentage Filtering Fix" -ForegroundColor Cyan

# Test 1: Test the correct email trigger endpoint with percentage query
Write-Host "`n=== TEST 1: Email Trigger Endpoint with Percentage Query ===" -ForegroundColor Yellow

try {
    $uri = "http://localhost:5037/api/naturallanguage/triggers/query"
    $body = @{
        query = "Show me campaigns with high click rate percentage greater than 20"
        includeDebugInfo = $true
    } | ConvertTo-Json -Depth 3

    Write-Host "Sending request to: $uri" -ForegroundColor Green
    Write-Host "Request body: $body" -ForegroundColor Gray

    $response = Invoke-RestMethod -Uri $uri -Method POST -Body $body -ContentType "application/json"
    
    Write-Host "`nResponse received:" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 4 | Write-Host
    
    # Check if percentage filtering was applied
    if ($response.debugInfo -and $response.debugInfo.extractedFilters) {
        Write-Host "`nDEBUG INFO ANALYSIS:" -ForegroundColor Cyan
        $filters = $response.debugInfo.extractedFilters
        
        Write-Host "isPercentageQuery: $($filters.isPercentageQuery)" -ForegroundColor $(if($filters.isPercentageQuery -eq $true) {"Green"} else {"Red"})
        Write-Host "appliedFilter: $($filters.appliedFilter)" -ForegroundColor White
        Write-Host "metricType: $($filters.metricType)" -ForegroundColor White
        Write-Host "threshold: $($filters.threshold)" -ForegroundColor White
        
        if ($filters.isPercentageQuery -eq $true -and $filters.appliedFilter -like "*MinClickRatePercentage*") {
            Write-Host "`n✅ SUCCESS: Percentage filtering was correctly applied!" -ForegroundColor Green
        } else {
            Write-Host "`n❌ FAILED: Percentage filtering was NOT applied correctly!" -ForegroundColor Red
        }
    } else {
        Write-Host "`n⚠️  WARNING: No debug info available to verify filtering" -ForegroundColor Yellow
    }
    
    # Check results
    if ($response.triggerReports -and $response.triggerReports.Count -gt 0) {
        Write-Host "`nRESULTS ANALYSIS:" -ForegroundColor Cyan
        foreach ($report in $response.triggerReports) {
            $clickRate = [math]::Round($report.clickRatePercentage, 2)
            $meetsCriteria = $clickRate -gt 20
            $status = if ($meetsCriteria) {"✅"} else {"❌"}
            Write-Host "$status Campaign: $($report.strategyName) - Click Rate: $clickRate%" -ForegroundColor $(if($meetsCriteria) {"Green"} else {"Red"})
        }
    } else {
        Write-Host "`n✅ EXPECTED: No results returned (all campaigns have ≤20% click rate)" -ForegroundColor Green
    }

} catch {
    Write-Host "`n❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
}

# Test 2: Test with count-based query for comparison
Write-Host "`n=== TEST 2: Email Trigger Endpoint with Count Query ===" -ForegroundColor Yellow

try {
    $uri = "http://localhost:5037/api/naturallanguage/triggers/query"
    $body = @{
        query = "Show me campaigns with click count greater than 20"
        includeDebugInfo = $true
    } | ConvertTo-Json -Depth 3

    Write-Host "Sending request to: $uri" -ForegroundColor Green
    Write-Host "Request body: $body" -ForegroundColor Gray

    $response = Invoke-RestMethod -Uri $uri -Method POST -Body $body -ContentType "application/json"
    
    # Check if count filtering was applied
    if ($response.debugInfo -and $response.debugInfo.extractedFilters) {
        Write-Host "`nDEBUG INFO ANALYSIS:" -ForegroundColor Cyan
        $filters = $response.debugInfo.extractedFilters
        
        Write-Host "isPercentageQuery: $($filters.isPercentageQuery)" -ForegroundColor $(if($filters.isPercentageQuery -eq $false) {"Green"} else {"Red"})
        Write-Host "appliedFilter: $($filters.appliedFilter)" -ForegroundColor White
        
        if ($filters.isPercentageQuery -eq $false -and $filters.appliedFilter -like "*MinClickedCount*") {
            Write-Host "`n✅ SUCCESS: Count filtering was correctly applied!" -ForegroundColor Green
        } else {
            Write-Host "`n❌ FAILED: Count filtering was NOT applied correctly!" -ForegroundColor Red
        }
    }

} catch {
    Write-Host "`n❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== TEST SUMMARY ===" -ForegroundColor Cyan
Write-Host "✅ The fix should correctly detect percentage vs count queries" -ForegroundColor Green
Write-Host "✅ Percentage queries should use MinClickRatePercentage filter" -ForegroundColor Green
Write-Host "✅ Count queries should use MinClickedCount filter" -ForegroundColor Green
Write-Host "✅ Queries for >20% click rate should return 0 results (based on mock data)" -ForegroundColor Green
