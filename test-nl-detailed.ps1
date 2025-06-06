param(
    [string]$Query = "show me campaigns open rates high"
)

$headers = @{
    "Content-Type" = "application/json"
}

$body = @{
    query = $Query
    includeDebugInfo = $true
} | ConvertTo-Json

Write-Host "Testing Natural Language Query API..."
Write-Host "URL: http://localhost:5037/api/naturallanguage/triggers/query"
Write-Host "Query: $Query"
Write-Host ""

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5037/api/naturallanguage/triggers/query" -Method POST -Headers $headers -Body $body
    
    Write-Host "=== RESPONSE SUCCESS ===" -ForegroundColor Green
    Write-Host "Success: $($response.success)" -ForegroundColor Yellow
    Write-Host "Intent: $($response.intent)" -ForegroundColor Yellow
    Write-Host "Explanation: $($response.explanation)" -ForegroundColor Yellow
    Write-Host "ProcessingTimeMs: $($response.processingTimeMs)" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "=== TRIGGER REPORTS ===" -ForegroundColor Cyan
    if ($response.triggerReports) {
        Write-Host "TriggerReports Count: $($response.triggerReports.Count)" -ForegroundColor Yellow
        Write-Host "Property exists: $($response.triggerReports -ne $null)" -ForegroundColor Yellow
        
        if ($response.triggerReports.Count -gt 0) {
            Write-Host "First report strategy: $($response.triggerReports[0].strategyName)" -ForegroundColor Yellow
            Write-Host "First report total emails: $($response.triggerReports[0].totalEmails)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "TriggerReports: NULL or missing" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "=== SUMMARY ===" -ForegroundColor Cyan
    if ($response.summary) {
        Write-Host "Summary exists: $($response.summary -ne $null)" -ForegroundColor Yellow
        Write-Host "Summary total emails: $($response.summary.totalEmails)" -ForegroundColor Yellow
    } else {
        Write-Host "Summary: NULL or missing" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "=== FULL RESPONSE ===" -ForegroundColor Magenta
    $response | ConvertTo-Json -Depth 5
    
} catch {
    Write-Host "=== ERROR ===" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}
