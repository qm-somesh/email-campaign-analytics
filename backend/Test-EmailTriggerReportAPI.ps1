# EmailTriggerReport API Test Script
# Tests all endpoints in the EmailTriggerReportController

param(
    [string]$BaseUrl = "http://localhost:5037",
    [switch]$Verbose
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Email Trigger Report API Test Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl" -ForegroundColor Yellow
Write-Host "Test Time: $(Get-Date)" -ForegroundColor Yellow
Write-Host ""

# Function to test an endpoint
function Test-Endpoint {
    param(
        [string]$Url,
        [string]$TestName,
        [string]$Method = "GET",
        [hashtable]$Headers = @{"Accept" = "application/json"}
    )
    
    Write-Host "🧪 Testing: $TestName" -ForegroundColor Green
    Write-Host "   URL: $Url" -ForegroundColor Gray
    
    try {
        $response = Invoke-WebRequest -Uri $Url -Method $Method -Headers $Headers -UseBasicParsing -TimeoutSec 30
        
        Write-Host "   ✅ Status: $($response.StatusCode)" -ForegroundColor Green
        
        if ($Verbose) {
            Write-Host "   📄 Content Length: $($response.Content.Length) characters" -ForegroundColor Blue
            if ($response.Content.Length -lt 2000) {
                Write-Host "   📋 Response Content:" -ForegroundColor Blue
                Write-Host "   $($response.Content)" -ForegroundColor White
            } else {
                Write-Host "   📋 Response Preview (first 500 chars):" -ForegroundColor Blue
                Write-Host "   $($response.Content.Substring(0, 500))..." -ForegroundColor White
            }
        }
        
        # Try to parse as JSON to validate structure
        try {
            $jsonResponse = $response.Content | ConvertFrom-Json
            Write-Host "   ✅ Valid JSON response" -ForegroundColor Green
            
            if ($jsonResponse -is [array]) {
                Write-Host "   📊 Array with $($jsonResponse.Count) items" -ForegroundColor Blue
            } elseif ($jsonResponse.items) {
                Write-Host "   📊 Paginated response with $($jsonResponse.items.Count) items" -ForegroundColor Blue
            }
        } catch {
            Write-Host "   ⚠️  Response is not valid JSON" -ForegroundColor Yellow
        }
        
    } catch {
        Write-Host "   ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
        
        if ($_.Exception.Response) {
            $statusCode = $_.Exception.Response.StatusCode
            Write-Host "   📊 HTTP Status: $statusCode" -ForegroundColor Red
            
            try {
                $errorStream = $_.Exception.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($errorStream)
                $errorContent = $reader.ReadToEnd()
                Write-Host "   📋 Error Content: $errorContent" -ForegroundColor Red
            } catch {
                Write-Host "   📋 Could not read error content" -ForegroundColor Red
            }
        }
    }
    
    Write-Host ""
}

# Function to check if API is running
function Test-ApiHealth {
    Write-Host "🏥 Checking API Health..." -ForegroundColor Magenta
    
    try {
        $response = Invoke-WebRequest -Uri "$BaseUrl/swagger" -UseBasicParsing -TimeoutSec 10
        Write-Host "   ✅ API is running (Swagger accessible)" -ForegroundColor Green
        return $true
    } catch {
        Write-Host "   ❌ API appears to be down or not accessible" -ForegroundColor Red
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Main test execution
Write-Host "Step 1: Health Check" -ForegroundColor Cyan
Write-Host "===================" -ForegroundColor Cyan
$apiRunning = Test-ApiHealth
Write-Host ""

if (-not $apiRunning) {
    Write-Host "❌ Cannot proceed with tests - API is not accessible" -ForegroundColor Red
    Write-Host "💡 Please ensure the backend is running with: dotnet run" -ForegroundColor Yellow
    exit 1
}

Write-Host "Step 2: EmailTriggerReport Endpoint Tests" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# Test 1: Get Strategy Names (simplest endpoint)
Test-Endpoint -Url "$BaseUrl/api/EmailTriggerReport/strategy-names" -TestName "Get Strategy Names"

# Test 2: Get Summary
Test-Endpoint -Url "$BaseUrl/api/EmailTriggerReport/summary" -TestName "Get Email Trigger Summary"

# Test 3: Get All Reports (default pagination)
Test-Endpoint -Url "$BaseUrl/api/EmailTriggerReport" -TestName "Get All Email Trigger Reports (Default)"

# Test 4: Get All Reports (custom pagination)
Test-Endpoint -Url "$BaseUrl/api/EmailTriggerReport?pageNumber=1&pageSize=5" -TestName "Get Reports (Page 1, Size 5)"

# Test 5: Get Report by Strategy Name (this will likely fail if no data exists)
Test-Endpoint -Url "$BaseUrl/api/EmailTriggerReport/Newsletter Campaign" -TestName "Get Report by Strategy Name"

# Test 6: Get Report by Non-existent Strategy
Test-Endpoint -Url "$BaseUrl/api/EmailTriggerReport/NonExistentStrategy" -TestName "Get Non-existent Strategy (Expected 404)"

# Test 7: Test error handling with invalid pagination
Test-Endpoint -Url "$BaseUrl/api/EmailTriggerReport?pageNumber=0&pageSize=-1" -TestName "Invalid Pagination Parameters"

Write-Host "Step 3: Other API Endpoints (for comparison)" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

# Test other working endpoints for comparison
Test-Endpoint -Url "$BaseUrl/api/dashboard/metrics" -TestName "Dashboard Metrics (using MockBigQueryService)"
Test-Endpoint -Url "$BaseUrl/api/campaigns" -TestName "Campaigns (using MockBigQueryService)"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✅ All tests completed" -ForegroundColor Green
Write-Host "💡 Check individual test results above" -ForegroundColor Yellow
Write-Host "💡 If EmailTriggerReport tests fail, check:" -ForegroundColor Yellow
Write-Host "   - SQL Server connection string" -ForegroundColor Yellow
Write-Host "   - Database and table existence" -ForegroundColor Yellow
Write-Host "   - Network connectivity to database" -ForegroundColor Yellow
Write-Host ""
Write-Host "🔗 For interactive testing, visit: $BaseUrl/swagger" -ForegroundColor Cyan
