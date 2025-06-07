# PowerShell script to test percentage filtering with detailed debugging
$baseUrl = "http://localhost:5037"

# Test 1: Percentage query with "percentage" keyword
Write-Host "Test 1: Query with 'percentage' keyword" -ForegroundColor Yellow
$body = @{
    Query = "show me campaigns with click rate percentage greater than 20"
    Context = "debug_test"
    IncludeDebugInfo = $true
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/naturallanguage/triggers/query" -Method POST -Body $body -ContentType "application/json"
    
    Write-Host "Success: $($response.Success)" -ForegroundColor Green
    Write-Host "Original Query: $($response.OriginalQuery)" -ForegroundColor Cyan
    Write-Host "Explanation: $($response.Explanation)" -ForegroundColor Magenta
    
    if ($response.DebugInfo) {
        Write-Host "Debug Info:" -ForegroundColor Yellow
        Write-Host "  Processing Method: $($response.DebugInfo.ProcessingMethod)" -ForegroundColor White
        Write-Host "  Additional Info:" -ForegroundColor White
        foreach ($key in $response.DebugInfo.AdditionalInfo.Keys) {
            Write-Host "    $key`: $($response.DebugInfo.AdditionalInfo[$key])" -ForegroundColor Gray
        }
    }
    
    if ($response.Results -and $response.Results.Count -gt 0) {
        Write-Host "First Result:" -ForegroundColor Green
        $firstResult = $response.Results[0]
        foreach ($prop in $firstResult.PSObject.Properties) {
            Write-Host "  $($prop.Name): $($prop.Value)" -ForegroundColor Gray
        }
    }
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response Body: $responseBody" -ForegroundColor Red
    }
}

Write-Host "`n" + "="*80 + "`n" -ForegroundColor Blue

# Test 2: Percentage query with "rate" keyword
Write-Host "Test 2: Query with 'rate' keyword" -ForegroundColor Yellow
$body2 = @{
    Query = "find strategies with high click rate greater than 25"
    Context = "debug_test"
    IncludeDebugInfo = $true
} | ConvertTo-Json

try {
    $response2 = Invoke-RestMethod -Uri "$baseUrl/api/naturallanguage/triggers/query" -Method POST -Body $body2 -ContentType "application/json"
    
    Write-Host "Success: $($response2.Success)" -ForegroundColor Green
    Write-Host "Original Query: $($response2.OriginalQuery)" -ForegroundColor Cyan
    Write-Host "Explanation: $($response2.Explanation)" -ForegroundColor Magenta
    
    if ($response2.DebugInfo) {
        Write-Host "Debug Info:" -ForegroundColor Yellow
        Write-Host "  Processing Method: $($response2.DebugInfo.ProcessingMethod)" -ForegroundColor White
        Write-Host "  Additional Info:" -ForegroundColor White
        foreach ($key in $response2.DebugInfo.AdditionalInfo.Keys) {
            Write-Host "    $key`: $($response2.DebugInfo.AdditionalInfo[$key])" -ForegroundColor Gray
        }
    }
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n" + "="*80 + "`n" -ForegroundColor Blue

# Test 3: Count-based query (should NOT use percentage)
Write-Host "Test 3: Count-based query (should NOT use percentage)" -ForegroundColor Yellow
$body3 = @{
    Query = "show me campaigns with click count greater than 20"
    Context = "debug_test"
    IncludeDebugInfo = $true
} | ConvertTo-Json

try {
    $response3 = Invoke-RestMethod -Uri "$baseUrl/api/naturallanguage/triggers/query" -Method POST -Body $body3 -ContentType "application/json"
    
    Write-Host "Success: $($response3.Success)" -ForegroundColor Green
    Write-Host "Original Query: $($response3.OriginalQuery)" -ForegroundColor Cyan
    Write-Host "Explanation: $($response3.Explanation)" -ForegroundColor Magenta
    
    if ($response3.DebugInfo) {
        Write-Host "Debug Info:" -ForegroundColor Yellow
        Write-Host "  Processing Method: $($response3.DebugInfo.ProcessingMethod)" -ForegroundColor White
        Write-Host "  Additional Info:" -ForegroundColor White
        foreach ($key in $response3.DebugInfo.AdditionalInfo.Keys) {
            Write-Host "    $key`: $($response3.DebugInfo.AdditionalInfo[$key])" -ForegroundColor Gray
        }
    }
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nTest completed!" -ForegroundColor Green
