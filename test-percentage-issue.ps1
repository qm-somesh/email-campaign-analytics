# Test the percentage filtering issue that was reported
# Query: "Show me campaigns with high click rate percentage greater than 20"
# Expected: Only campaigns with click rates > 20%
# Problem: Returns campaigns with rates like 9.5% and 28.9%

$baseUrl = "http://localhost:5037"
$endpoint = "$baseUrl/api/naturallanguage/triggers/query"

# Test the problematic query
$testQuery = @{
    Query = "Show me campaigns with high click rate percentage greater than 20"
    IncludeDebugInfo = $true
    Context = "email_triggers"
} | ConvertTo-Json

Write-Host "Testing percentage filtering issue..." -ForegroundColor Yellow
Write-Host "Query: 'Show me campaigns with high click rate percentage greater than 20'" -ForegroundColor Cyan
Write-Host ""

try {
    $response = Invoke-RestMethod -Uri $endpoint -Method POST -Body $testQuery -ContentType "application/json"
    
    Write-Host "✅ API Response received successfully" -ForegroundColor Green
    Write-Host "Intent: $($response.Intent)" -ForegroundColor Magenta
    Write-Host "Explanation: $($response.Explanation)" -ForegroundColor Magenta
    Write-Host ""
    
    if ($response.TriggerReports) {
        Write-Host "📊 Returned Campaigns:" -ForegroundColor Blue
        foreach ($report in $response.TriggerReports) {
            $clickRate = [math]::Round($report.ClickRate, 1)
            $status = if ($clickRate -gt 20) { "✅ CORRECT" } else { "❌ WRONG" }
            Write-Host "  - $($report.StrategyName): $clickRate% $status" -ForegroundColor $(if ($clickRate -gt 20) { "Green" } else { "Red" })
        }
        
        # Check if any results have click rate <= 20%
        $incorrectResults = $response.TriggerReports | Where-Object { $_.ClickRate -le 20 }
        if ($incorrectResults) {
            Write-Host ""
            Write-Host "🚨 ISSUE CONFIRMED: Found $($incorrectResults.Count) campaigns with click rate ≤ 20%" -ForegroundColor Red
            Write-Host "These should NOT be included when filtering for > 20%:" -ForegroundColor Red
            foreach ($incorrect in $incorrectResults) {
                Write-Host "  - $($incorrect.StrategyName): $([math]::Round($incorrect.ClickRate, 1))%" -ForegroundColor Red
            }
        } else {
            Write-Host ""
            Write-Host "✅ FILTERING WORKS CORRECTLY: All results have click rate > 20%" -ForegroundColor Green
        }
    } else {
        Write-Host "❌ No trigger reports returned" -ForegroundColor Red
    }
    
    # Show debug information if available
    if ($response.DebugInfo) {
        Write-Host ""
        Write-Host "🔍 Debug Information:" -ForegroundColor Yellow
        Write-Host "Processing Method: $($response.DebugInfo.ProcessingMethod)" -ForegroundColor Gray
        Write-Host "Service Method Called: $($response.DebugInfo.ServiceMethodCalled)" -ForegroundColor Gray
        
        if ($response.DebugInfo.ExtractedFilters) {
            Write-Host "Extracted Filters:" -ForegroundColor Gray
            $response.DebugInfo.ExtractedFilters | ConvertTo-Json | Write-Host -ForegroundColor Gray
        }
        
        if ($response.DebugInfo.Warnings) {
            Write-Host "Warnings:" -ForegroundColor Yellow
            foreach ($warning in $response.DebugInfo.Warnings) {
                Write-Host "  - $warning" -ForegroundColor Yellow
            }
        }
    }
    
} catch {
    Write-Host "❌ API call failed:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode
        Write-Host "Status Code: $statusCode" -ForegroundColor Red
        
        try {
            $errorResponse = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($errorResponse)
            $errorContent = $reader.ReadToEnd()
            Write-Host "Error Content: $errorContent" -ForegroundColor Red
        } catch {
            Write-Host "Could not read error response" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "Test completed." -ForegroundColor Cyan
