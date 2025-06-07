$uri = "http://localhost:5037/api/naturallanguage/triggers/query"
$body = '{"query": "Show me campaigns with high click rate percentage greater than 20", "includeDebugInfo": true}'

$response = Invoke-RestMethod -Uri $uri -Method POST -Body $body -ContentType "application/json"

Write-Output "Success: $($response.success)"
Write-Output "Total Count: $($response.totalCount)"
Write-Output "Explanation: $($response.explanation)"

if ($response.debugInfo.extractedFilters) {
    $filters = $response.debugInfo.extractedFilters
    Write-Output "isPercentageQuery: $($filters.isPercentageQuery)"
    Write-Output "appliedFilter: $($filters.appliedFilter)"
}
