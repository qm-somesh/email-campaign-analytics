#!/usr/bin/env pwsh

# Natural Language Email Trigger API Test Script
# This script tests the new Natural Language Email Trigger Controller

$ErrorActionPreference = "Stop"

# Configuration
$baseUrl = "http://localhost:5037"
$endpoint = "$baseUrl/api/naturallanguageemailtrigger/query"

Write-Host "🧪 Testing Natural Language Email Trigger API" -ForegroundColor Green
Write-Host "Base URL: $baseUrl" -ForegroundColor Cyan
Write-Host "Endpoint: $endpoint" -ForegroundColor Cyan
Write-Host ""

# Test queries to validate
$testQueries = @(
    @{
        Name = "High Open Count Query"
        Query = "Show me campaigns with more than 1000 opens"
        ExpectedFilters = @("MinOpenedCount")
    },
    @{
        Name = "Click Rate Query"  
        Query = "What campaigns had click rates above 5%?"
        ExpectedFilters = @("MinClickedPercentage")
    },
    @{
        Name = "Strategy Name Query"
        Query = "Find campaigns with 'Black Friday' in the name"
        ExpectedFilters = @("StrategyName")
    },
    @{
        Name = "Date Range Query"
        Query = "Show me campaigns from last month with high performance"
        ExpectedFilters = @("DateRangeStart", "DateRangeEnd")
    },
    @{
        Name = "Sorting Query"
        Query = "List all campaigns sorted by open rate descending"
        ExpectedFilters = @("SortBy", "SortDescending")
    },
    @{
        Name = "Complex Multi-Filter Query"
        Query = "Find campaigns with more than 500 opens, click rate above 3%, and containing 'promotion' in the strategy name"
        ExpectedFilters = @("MinOpenedCount", "MinClickedPercentage", "StrategyName")
    },
    @{
        Name = "Range Query"
        Query = "Show campaigns with open rates between 10% and 50%"
        ExpectedFilters = @("MinOpenedPercentage", "MaxOpenedPercentage")
    }
)

function Test-ApiEndpoint {
    param(
        [string]$Query,
        [string]$TestName,
        [string[]]$ExpectedFilters,
        [bool]$IncludeDebug = $true
    )
    
    Write-Host "📋 Testing: $TestName" -ForegroundColor Yellow
    Write-Host "   Query: '$Query'" -ForegroundColor Gray
    
    $requestBody = @{
        query = $Query
        page = 1
        pageSize = 10
        includeDebugInfo = $IncludeDebug
    } | ConvertTo-Json -Depth 10
    
    try {
        $response = Invoke-RestMethod -Uri $endpoint -Method Post -Body $requestBody -ContentType "application/json" -TimeoutSec 30
        
        Write-Host "   ✅ Status: Success" -ForegroundColor Green
        Write-Host "   📊 Results: $($response.results.totalCount) total records" -ForegroundColor Cyan
        Write-Host "   ⏱️  Processing Time: $($response.processingTimeMs)ms" -ForegroundColor Cyan
        Write-Host "   🎯 Filter Extraction: $($response.filterExtractionSuccessful)" -ForegroundColor Cyan
        Write-Host "   📝 Applied Filters: $($response.filterSummary)" -ForegroundColor Cyan
        
        if ($response.hasWarnings) {
            Write-Host "   ⚠️  Warnings:" -ForegroundColor Yellow
            foreach ($warning in $response.warnings) {
                Write-Host "      - $warning" -ForegroundColor Yellow
            }
        }
        
        # Check if expected filters were extracted
        if ($IncludeDebug -and $response.debugInfo) {
            Write-Host "   🔍 Debug Info:" -ForegroundColor Magenta
            Write-Host "      - LLM Processing: $($response.debugInfo.llmProcessingTimeMs)ms" -ForegroundColor Gray
            Write-Host "      - DB Query: $($response.debugInfo.databaseQueryTimeMs)ms" -ForegroundColor Gray
            Write-Host "      - JSON Parsing: $($response.debugInfo.jsonParsingSuccessful)" -ForegroundColor Gray
            Write-Host "      - Extracted Fields: $($response.debugInfo.extractedFilterFields -join ', ')" -ForegroundColor Gray
            
            if ($response.debugInfo.processingErrors.Count -gt 0) {
                Write-Host "      - Errors:" -ForegroundColor Red
                foreach ($error in $response.debugInfo.processingErrors) {
                    Write-Host "        $error" -ForegroundColor Red
                }
            }
        }
        
        # Validate expected filters
        $extractedFields = if ($response.debugInfo) { $response.debugInfo.extractedFilterFields } else { @() }
        $missingFilters = $ExpectedFilters | Where-Object { $_ -notin $extractedFields }
        
        if ($missingFilters.Count -gt 0) {
            Write-Host "   ⚠️  Missing Expected Filters: $($missingFilters -join ', ')" -ForegroundColor Yellow
        } else {
            Write-Host "   ✅ All expected filters extracted successfully" -ForegroundColor Green
        }
        
        Write-Host ""
        return $true
    }
    catch {
        Write-Host "   ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            $statusCode = $_.Exception.Response.StatusCode
            Write-Host "   📊 Status Code: $statusCode" -ForegroundColor Red
        }
        Write-Host ""
        return $false
    }
}

function Test-ApiHealth {
    Write-Host "🏥 Checking API Health..." -ForegroundColor Blue
    
    try {
        $healthResponse = Invoke-RestMethod -Uri "$baseUrl/health" -Method Get -TimeoutSec 10
        Write-Host "   ✅ API is healthy" -ForegroundColor Green
        return $true
    }
    catch {
        try {
            # Try the swagger endpoint as fallback
            $swaggerResponse = Invoke-RestMethod -Uri "$baseUrl/swagger/v1/swagger.json" -Method Get -TimeoutSec 10
            Write-Host "   ✅ API is responding (via Swagger)" -ForegroundColor Green
            return $true
        }
        catch {
            Write-Host "   ❌ API is not responding: $($_.Exception.Message)" -ForegroundColor Red
            return $false
        }
    }
}

# Main execution
Write-Host "Starting Natural Language Email Trigger API Tests..." -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
Write-Host ""

# Check API health first
if (-not (Test-ApiHealth)) {
    Write-Host "❌ Cannot proceed - API is not responding. Please ensure the backend is running on $baseUrl" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Run all test queries
$successCount = 0
$totalTests = $testQueries.Count

foreach ($test in $testQueries) {
    $success = Test-ApiEndpoint -Query $test.Query -TestName $test.Name -ExpectedFilters $test.ExpectedFilters
    if ($success) {
        $successCount++
    }
    Start-Sleep -Milliseconds 500  # Brief pause between tests
}

# Summary
Write-Host "=============================================" -ForegroundColor Green
Write-Host "🏁 Test Summary" -ForegroundColor Green
Write-Host "   Total Tests: $totalTests" -ForegroundColor Cyan
Write-Host "   Successful: $successCount" -ForegroundColor Green
Write-Host "   Failed: $($totalTests - $successCount)" -ForegroundColor Red

if ($successCount -eq $totalTests) {
    Write-Host "   🎉 All tests passed!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "   ⚠️  Some tests failed. Check the output above for details." -ForegroundColor Yellow
    exit 1
}
