#!/usr/bin/env pwsh

# Email Trigger Filter Extraction Test Script
# This script specifically tests the LLM-based filter extraction in EmailTriggerFilterService

$ErrorActionPreference = "Stop"

# Configuration
$baseUrl = "http://localhost:5037"
$endpoint = "$baseUrl/api/naturallanguageemailtrigger/query"

Write-Host "🧪 Testing Email Trigger Filter Extraction" -ForegroundColor Green
Write-Host "Base URL: $baseUrl" -ForegroundColor Cyan
Write-Host "Endpoint: $endpoint" -ForegroundColor Cyan
Write-Host ""

# Test a specific query with debug info
function Test-FilterExtraction {
    param (
        [string]$TestName,
        [string]$Query
    )
    
    Write-Host "Running test: $TestName" -ForegroundColor Cyan
    Write-Host "Query: $Query" -ForegroundColor Yellow
    
    $body = @{
        query = $Query
        includeDebugInfo = $true
        pageNumber = 1
        pageSize = 10
    } | ConvertTo-Json
    
    try {
        $response = Invoke-RestMethod -Uri $endpoint -Method POST -Body $body -ContentType "application/json"
        
        Write-Host "`nFilter Extraction Results:" -ForegroundColor Green
        Write-Host "Success: $($response.FilterExtractionSuccessful)" -ForegroundColor $(if($response.FilterExtractionSuccessful) {"Green"} else {"Red"})
        
        if ($response.FilterSummary) {
            Write-Host "Filter Summary: $($response.FilterSummary)" -ForegroundColor Magenta
        }
        
        if ($response.AppliedFilters) {
            Write-Host "`nApplied Filters:" -ForegroundColor Cyan
            $filters = $response.AppliedFilters
            $filterProps = $filters.PSObject.Properties | Where-Object { $null -ne $_.Value }
            
            if ($filterProps.Count -gt 0) {
                foreach ($prop in $filterProps) {
                    Write-Host "  $($prop.Name): $($prop.Value)" -ForegroundColor White
                }
            } else {
                Write-Host "  No filters extracted" -ForegroundColor Red
            }
        }
        
        # Debug information
        if ($response.DebugInfo) {
            Write-Host "`nDebug Information:" -ForegroundColor Yellow
            Write-Host "LLM Processing Time: $($response.DebugInfo.LlmProcessingTimeMs)ms" -ForegroundColor White
            Write-Host "JSON Parsing Successful: $($response.DebugInfo.JsonParsingSuccessful)" -ForegroundColor $(if($response.DebugInfo.JsonParsingSuccessful) {"Green"} else {"Red"})
            
            if ($response.DebugInfo.JsonParsingError) {
                Write-Host "JSON Parsing Error: $($response.DebugInfo.JsonParsingError)" -ForegroundColor Red
            }
            
            Write-Host "Confidence Score: $($response.DebugInfo.ConfidenceScore)" -ForegroundColor White
            
            if ($response.DebugInfo.ExtractedFilterFields) {
                Write-Host "Extracted Filter Fields: $($response.DebugInfo.ExtractedFilterFields -join ", ")" -ForegroundColor Cyan
            }
            
            if ($response.DebugInfo.RawLlmResponse) {
                Write-Host "`nRaw LLM Response:" -ForegroundColor Magenta
                Write-Host $response.DebugInfo.RawLlmResponse -ForegroundColor Gray
            }
        }
        
        return $response
    } 
    catch {
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            $responseBody = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($responseBody)
            $responseContent = $reader.ReadToEnd()
            Write-Host "Response Content: $responseContent" -ForegroundColor Red
        }
        return $null
    }
    
    Write-Host "---------------------------------------------------" -ForegroundColor DarkGray
}

# Run tests for various query types to diagnose filter extraction issues
$testCases = @(
    @{
        Name = "High Open Rate Query"
        Query = "Show campaigns with high open rates"
    },
    @{
        Name = "Click Rate Query"
        Query = "Find campaigns with click rate above 5%"
    },
    @{
        Name = "Campaign Name Query"
        Query = "Show me all Spring Sale campaigns"
    },
    @{
        Name = "Date Range Query"
        Query = "Find campaigns from last month"
    },
    @{
        Name = "Mixed Query"
        Query = "Show Spring Sale campaigns with open rates above 20% from last month" 
    }
)

# Run each test and analyze the results
foreach ($test in $testCases) {
    Write-Host "`n======================================================" -ForegroundColor DarkCyan
    $result = Test-FilterExtraction -TestName $test.Name -Query $test.Query
    
    # Analyze quality of filter extraction
    if ($result) {
        $extractedParams = $result.DebugInfo.ExtractedFilterFields
        if (-not $extractedParams -or $extractedParams.Count -eq 0) {
            Write-Host "`n⚠️ ISSUE DETECTED: No filter parameters extracted!" -ForegroundColor Red
        }
    }
    Write-Host "`n======================================================" -ForegroundColor DarkCyan
}

Write-Host "`n✅ All tests completed" -ForegroundColor Green
