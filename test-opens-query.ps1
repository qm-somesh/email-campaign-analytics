# Test the opens query issue
$baseUrl = "http://localhost:5037"
$query = "What campaigns had more than 1000 opens?"

Write-Host "Testing Query: $query" -ForegroundColor Yellow

$body = @{
    query = $query
    includeDebugInfo = $true
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/naturallanguage/triggers/query" -Method POST -Body $body -ContentType "application/json" -TimeoutSec 30
    
    Write-Host "Success: $($response.success)" -ForegroundColor Green
    Write-Host "Total Count: $($response.totalCount)" -ForegroundColor Cyan
    Write-Host "Explanation: $($response.explanation)" -ForegroundColor Cyan
    
    if ($response.debugInfo -and $response.debugInfo.extractedFilters) {
        Write-Host "`nDebug Info:" -ForegroundColor Yellow
        $filters = $response.debugInfo.extractedFilters
        foreach ($key in $filters.Keys) {
            Write-Host "  $key = $($filters[$key])" -ForegroundColor Gray
        }
    }
    
    if ($response.triggerReports) {
        Write-Host "`nResults:" -ForegroundColor Green
        foreach ($report in $response.triggerReports) {
            $status = if ($report.openedCount -gt 1000) { "✅" } else { "❌" }
            Write-Host "$status $($report.strategyName): Opens = $($report.openedCount)" -ForegroundColor $(if ($report.openedCount -gt 1000) { "Green" } else { "Red" })
        }
    }
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}
