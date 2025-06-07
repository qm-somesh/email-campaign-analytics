# Step-by-step debugging script for percentage filtering
$baseUrl = "http://localhost:5037"

Write-Host "=== STEP-BY-STEP DEBUGGING FOR PERCENTAGE FILTERING ===" -ForegroundColor Magenta

# Step 1: Test basic endpoint connectivity
Write-Host "`nSTEP 1: Testing basic endpoint connectivity" -ForegroundColor Yellow
try {
    $testResponse = Invoke-RestMethod -Uri "$baseUrl/api/naturallanguage/triggers/query" -Method POST -Body (@{Query = "test"} | ConvertTo-Json) -ContentType "application/json" -TimeoutSec 10
    Write-Host "✓ Endpoint is accessible" -ForegroundColor Green
    Write-Host "  Success: $($testResponse.Success)" -ForegroundColor Cyan
} catch {
    Write-Host "✗ Endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Test percentage query with detailed debug info
Write-Host "`nSTEP 2: Testing percentage query with debug info" -ForegroundColor Yellow
$percentageQuery = "show me campaigns with click rate percentage greater than 20"
$body = @{
    Query = $percentageQuery
    IncludeDebugInfo = $true
} | ConvertTo-Json

Write-Host "Query: $percentageQuery" -ForegroundColor Cyan
Write-Host "Request Body:" -ForegroundColor Gray
Write-Host $body -ForegroundColor Gray

try {
    Write-Host "Sending request..." -ForegroundColor White
    $response = Invoke-RestMethod -Uri "$baseUrl/api/naturallanguage/triggers/query" -Method POST -Body $body -ContentType "application/json" -TimeoutSec 30
    
    Write-Host "✓ Request completed successfully" -ForegroundColor Green
    
    # Step 3: Analyze response structure
    Write-Host "`nSTEP 3: Analyzing response structure" -ForegroundColor Yellow
    Write-Host "Response Type: $($response.GetType().Name)" -ForegroundColor Cyan
    Write-Host "Success: $($response.Success)" -ForegroundColor Cyan
    Write-Host "Original Query: '$($response.OriginalQuery)'" -ForegroundColor Cyan
    
    if ($response.Explanation) {
        Write-Host "Explanation: $($response.Explanation)" -ForegroundColor Cyan
    }
    
    # Step 4: Examine debug information
    Write-Host "`nSTEP 4: Examining debug information" -ForegroundColor Yellow
    if ($response.DebugInfo) {
        Write-Host "✓ Debug info is present" -ForegroundColor Green
        Write-Host "Processing Method: $($response.DebugInfo.ProcessingMethod)" -ForegroundColor Cyan
        
        if ($response.DebugInfo.AdditionalInfo) {
            Write-Host "Additional Info:" -ForegroundColor White
            foreach ($key in $response.DebugInfo.AdditionalInfo.Keys) {
                $value = $response.DebugInfo.AdditionalInfo[$key]
                Write-Host "  $key = $value" -ForegroundColor Gray
                
                # Check for percentage-related debugging info
                if ($key -like "*percentage*" -or $key -like "*rate*" -or $key -like "*filter*") {
                    Write-Host "    >>> PERCENTAGE-RELATED: $key = $value" -ForegroundColor Yellow
                }
            }
        }
        
        if ($response.DebugInfo.ExtractedFilters) {
            Write-Host "Extracted Filters:" -ForegroundColor White
            foreach ($key in $response.DebugInfo.ExtractedFilters.Keys) {
                $value = $response.DebugInfo.ExtractedFilters[$key]
                Write-Host "  $key = $value" -ForegroundColor Gray
            }
        }
        
        if ($response.DebugInfo.Warnings -and $response.DebugInfo.Warnings.Count -gt 0) {
            Write-Host "Warnings:" -ForegroundColor Red
            foreach ($warning in $response.DebugInfo.Warnings) {
                Write-Host "  - $warning" -ForegroundColor Red
            }
        }
    } else {
        Write-Host "✗ No debug info available" -ForegroundColor Red
    }
    
    # Step 5: Check results
    Write-Host "`nSTEP 5: Checking results" -ForegroundColor Yellow
    if ($response.Results) {
        Write-Host "✓ Results found: $($response.Results.Count) items" -ForegroundColor Green
        if ($response.Results.Count -gt 0) {
            Write-Host "First result sample:" -ForegroundColor White
            $firstResult = $response.Results[0]
            $properties = $firstResult | Get-Member -MemberType Properties | Select-Object -First 5
            foreach ($prop in $properties) {
                $value = $firstResult.($prop.Name)
                Write-Host "  $($prop.Name): $value" -ForegroundColor Gray
            }
        }
    } else {
        Write-Host "✗ No results returned" -ForegroundColor Red
    }
    
} catch {
    Write-Host "✗ Request failed at step 2" -ForegroundColor Red
    Write-Host "Error Type: $($_.Exception.GetType().Name)" -ForegroundColor Red
    Write-Host "Error Message: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        Write-Host "HTTP Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
        try {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $responseBody = $reader.ReadToEnd()
            Write-Host "Response Body: $responseBody" -ForegroundColor Red
        } catch {
            Write-Host "Could not read response body" -ForegroundColor Red
        }
    }
}

Write-Host "`n=== DEBUGGING COMPLETED ===" -ForegroundColor Magenta
